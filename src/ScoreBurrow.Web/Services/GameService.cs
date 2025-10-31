using Microsoft.EntityFrameworkCore;
using ScoreBurrow.Data;
using ScoreBurrow.Data.Entities;
using ScoreBurrow.Data.Enums;
using ScoreBurrow.Rating.Models;
using ScoreBurrow.Rating.Services;
using ScoreBurrow.Web.Models;

namespace ScoreBurrow.Web.Services;

public class GameService : IGameService
{
    private readonly ScoreBurrowDbContext _context;
    private readonly ILeagueService _leagueService;
    private readonly IRatingService _ratingService;

    public GameService(
        ScoreBurrowDbContext context,
        ILeagueService leagueService,
        IRatingService ratingService)
    {
        _context = context;
        _leagueService = leagueService;
        _ratingService = ratingService;
    }

    public async Task<Guid> CreateGameAsync(Guid leagueId, string userId, CreateGameRequest request)
    {
        // Verify permissions
        if (!await _leagueService.IsAdminOrOwnerAsync(userId, leagueId))
        {
            throw new UnauthorizedAccessException("User does not have permission to create games in this league.");
        }

        // Create game
        var game = new Game
        {
            Id = Guid.NewGuid(),
            LeagueId = leagueId,
            MapName = request.MapName,
            StartTime = DateTime.UtcNow,
            Status = GameStatus.InProgress,
            CreatedBy = userId,
            CreatedOn = DateTime.UtcNow
        };

        _context.Games.Add(game);

        // Create participants with rating snapshots
        foreach (var participantRequest in request.Participants)
        {
            // Get current rating from LeagueMembership
            var membership = await _context.LeagueMemberships
                .FirstOrDefaultAsync(m => m.Id == participantRequest.LeagueMembershipId);

            if (membership == null)
            {
                throw new ArgumentException($"League membership {participantRequest.LeagueMembershipId} not found.");
            }

            var participant = new GameParticipant
            {
                Id = Guid.NewGuid(),
                GameId = game.Id,
                LeagueMembershipId = participantRequest.LeagueMembershipId,
                TownId = participantRequest.TownId,
                HeroId = participantRequest.HeroId,
                PlayerColor = participantRequest.PlayerColor,
                Position = participantRequest.Position,
                GoldTrade = participantRequest.GoldTrade,
                IsWinner = false,
                IsTechnicalLoss = false,
                // Snapshot current ratings
                RatingAtGameTime = membership.Glicko2Rating,
                RatingDeviationAtGameTime = membership.Glicko2RatingDeviation,
                VolatilityAtGameTime = membership.Glicko2Volatility,
                CreatedBy = userId,
                CreatedOn = DateTime.UtcNow
            };

            _context.GameParticipants.Add(participant);
        }

        await _context.SaveChangesAsync();

        return game.Id;
    }

    public async Task<bool> CompleteGameAsync(Guid gameId, string userId, Guid winnerId)
    {
        var game = await _context.Games
            .Include(g => g.Participants)
            .ThenInclude(p => p.LeagueMembership)
            .FirstOrDefaultAsync(g => g.Id == gameId);

        if (game == null)
        {
            return false;
        }

        // Verify permissions
        if (!await _leagueService.IsAdminOrOwnerAsync(userId, game.LeagueId))
        {
            throw new UnauthorizedAccessException("User does not have permission to manage games in this league.");
        }

        // Verify game is in progress
        if (game.Status != GameStatus.InProgress)
        {
            throw new InvalidOperationException("Game is not in progress.");
        }

        // Verify winner is a participant
        var winnerParticipant = game.Participants.FirstOrDefault(p => p.LeagueMembershipId == winnerId);
        if (winnerParticipant == null)
        {
            throw new ArgumentException("Winner must be a participant in the game.");
        }

        // Build rating snapshots for calculation
        var participantRatings = new Dictionary<Guid, RatingSnapshot>();
        foreach (var participant in game.Participants)
        {
            participantRatings[participant.LeagueMembershipId] = new RatingSnapshot(
                participant.RatingAtGameTime,
                participant.RatingDeviationAtGameTime,
                participant.VolatilityAtGameTime
            );
        }

        // Calculate new ratings
        var ratingUpdates = _ratingService.CalculateMultiPlayerGameRatings(participantRatings, winnerId);

        // Update game
        game.Status = GameStatus.Completed;
        game.EndTime = DateTime.UtcNow;
        game.WinnerId = winnerId;
        game.ModifiedBy = userId;
        game.ModifiedOn = DateTime.UtcNow;

        // Mark winner
        winnerParticipant.IsWinner = true;
        winnerParticipant.ModifiedBy = userId;
        winnerParticipant.ModifiedOn = DateTime.UtcNow;

        // Update ratings and create history
        foreach (var participant in game.Participants)
        {
            var update = ratingUpdates[participant.LeagueMembershipId];
            var membership = participant.LeagueMembership;

            // Update membership ratings
            membership.Glicko2Rating = update.NewRating.Rating;
            membership.Glicko2RatingDeviation = update.NewRating.RatingDeviation;
            membership.Glicko2Volatility = update.NewRating.Volatility;

            // Create rating history
            var history = new RatingHistory
            {
                Id = Guid.NewGuid(),
                LeagueMembershipId = participant.LeagueMembershipId,
                GameId = gameId,
                CalculatedAt = DateTime.UtcNow,
                PreviousRating = update.PreviousRating.Rating,
                PreviousRatingDeviation = update.PreviousRating.RatingDeviation,
                PreviousVolatility = update.PreviousRating.Volatility,
                NewRating = update.NewRating.Rating,
                NewRatingDeviation = update.NewRating.RatingDeviation,
                NewVolatility = update.NewRating.Volatility,
                CreatedBy = userId,
                CreatedOn = DateTime.UtcNow
            };

            _context.RatingHistory.Add(history);

            // Update statistics
            var stats = await _context.PlayerStatistics
                .FirstOrDefaultAsync(s => s.LeagueMembershipId == participant.LeagueMembershipId);

            if (stats == null)
            {
                stats = new PlayerStatistics
                {
                    Id = Guid.NewGuid(),
                    LeagueMembershipId = participant.LeagueMembershipId
                };
                _context.PlayerStatistics.Add(stats);
            }

            stats.GamesPlayed++;
            if (participant.LeagueMembershipId == winnerId)
            {
                stats.GamesWon++;
            }

            // Update favorite town (town with most games played)
            var townStats = await _context.GameParticipants
                .Where(gp => gp.LeagueMembershipId == participant.LeagueMembershipId)
                .GroupBy(gp => gp.TownId)
                .Select(g => new { TownId = g.Key, Count = g.Count() })
                .OrderByDescending(x => x.Count)
                .FirstOrDefaultAsync();

            if (townStats != null)
            {
                stats.FavoriteTownId = townStats.TownId;
            }
        }

        await _context.SaveChangesAsync();

        return true;
    }

    public async Task<Guid?> ApplyTechnicalLossAsync(Guid gameId, string userId, Guid culpritMembershipId)
    {
        var game = await _context.Games
            .Include(g => g.Participants)
            .ThenInclude(p => p.LeagueMembership)
            .Include(g => g.Participants)
            .ThenInclude(p => p.Town)
            .Include(g => g.Participants)
            .ThenInclude(p => p.Hero)
            .FirstOrDefaultAsync(g => g.Id == gameId);

        if (game == null)
        {
            return null;
        }

        // Verify permissions
        if (!await _leagueService.IsAdminOrOwnerAsync(userId, game.LeagueId))
        {
            throw new UnauthorizedAccessException("User does not have permission to manage games in this league.");
        }

        // Verify game is in progress
        if (game.Status != GameStatus.InProgress)
        {
            throw new InvalidOperationException("Game is not in progress.");
        }

        // Get culprit participant
        var culpritParticipant = game.Participants.FirstOrDefault(p => p.LeagueMembershipId == culpritMembershipId);
        if (culpritParticipant == null)
        {
            throw new ArgumentException("Culprit must be a participant in the game.");
        }

        // Apply technical loss penalty
        var culpritRating = new RatingSnapshot(
            culpritParticipant.RatingAtGameTime,
            culpritParticipant.RatingDeviationAtGameTime,
            culpritParticipant.VolatilityAtGameTime
        );

        var penaltyUpdate = _ratingService.ApplyTechnicalLossPenalty(culpritRating);

        // Mark participant as technical loss
        culpritParticipant.IsTechnicalLoss = true;
        culpritParticipant.ModifiedBy = userId;
        culpritParticipant.ModifiedOn = DateTime.UtcNow;

        // Update culprit's rating
        var culpritMembership = culpritParticipant.LeagueMembership;
        culpritMembership.Glicko2Rating = penaltyUpdate.NewRating.Rating;
        culpritMembership.Glicko2RatingDeviation = penaltyUpdate.NewRating.RatingDeviation;
        culpritMembership.Glicko2Volatility = penaltyUpdate.NewRating.Volatility;

        // Create rating history for culprit
        var history = new RatingHistory
        {
            Id = Guid.NewGuid(),
            LeagueMembershipId = culpritMembershipId,
            GameId = gameId,
            CalculatedAt = DateTime.UtcNow,
            PreviousRating = penaltyUpdate.PreviousRating.Rating,
            PreviousRatingDeviation = penaltyUpdate.PreviousRating.RatingDeviation,
            PreviousVolatility = penaltyUpdate.PreviousRating.Volatility,
            NewRating = penaltyUpdate.NewRating.Rating,
            NewRatingDeviation = penaltyUpdate.NewRating.RatingDeviation,
            NewVolatility = penaltyUpdate.NewRating.Volatility,
            CreatedBy = userId,
            CreatedOn = DateTime.UtcNow
        };

        _context.RatingHistory.Add(history);

        // Cancel current game
        game.Status = GameStatus.Cancelled;
        game.EndTime = DateTime.UtcNow;
        game.Notes = $"Cancelled due to technical loss by {culpritMembership.PlayerDisplayName ?? culpritMembership.PlayerNickname}";
        game.ModifiedBy = userId;
        game.ModifiedOn = DateTime.UtcNow;

        // Create new game with same settings
        var newGame = new Game
        {
            Id = Guid.NewGuid(),
            LeagueId = game.LeagueId,
            MapName = game.MapName,
            StartTime = DateTime.UtcNow,
            Status = GameStatus.InProgress,
            Notes = $"Restarted after technical loss in game {gameId}",
            CreatedBy = userId,
            CreatedOn = DateTime.UtcNow
        };

        _context.Games.Add(newGame);

        // Create new participants with updated gold for culprit
        foreach (var oldParticipant in game.Participants.OrderBy(p => p.Position))
        {
            // Get updated rating snapshot
            var membership = await _context.LeagueMemberships
                .FirstOrDefaultAsync(m => m.Id == oldParticipant.LeagueMembershipId);

            if (membership == null) continue;

            var newParticipant = new GameParticipant
            {
                Id = Guid.NewGuid(),
                GameId = newGame.Id,
                LeagueMembershipId = oldParticipant.LeagueMembershipId,
                TownId = oldParticipant.TownId,
                HeroId = oldParticipant.HeroId,
                PlayerColor = oldParticipant.PlayerColor,
                Position = oldParticipant.Position,
                // Apply -1000 gold penalty to culprit
                GoldTrade = oldParticipant.LeagueMembershipId == culpritMembershipId 
                    ? oldParticipant.GoldTrade - 1000 
                    : oldParticipant.GoldTrade,
                IsWinner = false,
                IsTechnicalLoss = false,
                // Snapshot current ratings (updated for culprit)
                RatingAtGameTime = membership.Glicko2Rating,
                RatingDeviationAtGameTime = membership.Glicko2RatingDeviation,
                VolatilityAtGameTime = membership.Glicko2Volatility,
                CreatedBy = userId,
                CreatedOn = DateTime.UtcNow
            };

            _context.GameParticipants.Add(newParticipant);
        }

        await _context.SaveChangesAsync();

        return newGame.Id;
    }

    public async Task<bool> CancelGameAsync(Guid gameId, string userId)
    {
        var game = await _context.Games
            .FirstOrDefaultAsync(g => g.Id == gameId);

        if (game == null)
        {
            return false;
        }

        // Verify permissions
        if (!await _leagueService.IsAdminOrOwnerAsync(userId, game.LeagueId))
        {
            throw new UnauthorizedAccessException("User does not have permission to manage games in this league.");
        }

        // Verify game is in progress
        if (game.Status != GameStatus.InProgress)
        {
            throw new InvalidOperationException("Game is not in progress.");
        }

        // Cancel game without rating changes
        game.Status = GameStatus.Cancelled;
        game.EndTime = DateTime.UtcNow;
        game.ModifiedBy = userId;
        game.ModifiedOn = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return true;
    }

    public async Task<GameDetailsDto?> GetGameForManagementAsync(Guid gameId, string userId)
    {
        var game = await _context.Games
            .Include(g => g.Participants)
            .ThenInclude(p => p.LeagueMembership)
            .Include(g => g.Participants)
            .ThenInclude(p => p.Town)
            .Include(g => g.Participants)
            .ThenInclude(p => p.Hero)
            .FirstOrDefaultAsync(g => g.Id == gameId);

        if (game == null)
        {
            return null;
        }

        // Verify permissions
        if (!await _leagueService.IsAdminOrOwnerAsync(userId, game.LeagueId))
        {
            return null;
        }

        var dto = new GameDetailsDto
        {
            Id = game.Id,
            LeagueId = game.LeagueId,
            MapName = game.MapName,
            StartTime = game.StartTime,
            Status = game.Status,
            Participants = game.Participants
                .OrderBy(p => p.Position)
                .Select(p => new ParticipantDto
                {
                    Id = p.Id,
                    LeagueMembershipId = p.LeagueMembershipId,
                    PlayerName = p.LeagueMembership.PlayerDisplayName ?? p.LeagueMembership.PlayerNickname,
                    PlayerColor = p.PlayerColor,
                    Position = p.Position,
                    TownName = p.Town.Name,
                    HeroName = p.Hero?.Name,
                    GoldTrade = p.GoldTrade,
                    IsWinner = p.IsWinner,
                    IsTechnicalLoss = p.IsTechnicalLoss
                })
                .ToList()
        };

        return dto;
    }
}
