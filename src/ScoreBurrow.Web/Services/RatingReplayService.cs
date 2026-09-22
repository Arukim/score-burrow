using Microsoft.EntityFrameworkCore;
using ScoreBurrow.Data;
using ScoreBurrow.Data.Entities;
using ScoreBurrow.Data.Enums;
using ScoreBurrow.Rating.Models;
using ScoreBurrow.Rating.Services;

namespace ScoreBurrow.Web.Services;

/// <summary>
/// Replays a league's completed games chronologically to rebuild Glicko-2 ratings,
/// participant snapshots, and rating history under the current formula.
/// </summary>
public class RatingReplayService
{
    private readonly ScoreBurrowDbContext _dbContext;
    private readonly IRatingService _ratingService;
    private readonly ILogger<RatingReplayService> _logger;

    public RatingReplayService(
        ScoreBurrowDbContext dbContext,
        IRatingService ratingService,
        ILogger<RatingReplayService> logger)
    {
        _dbContext = dbContext;
        _ratingService = ratingService;
        _logger = logger;
    }

    /// <summary>
    /// Resets and recalculates all Glicko-2 ratings for a league from completed games.
    /// Returns the number of games that produced rating updates.
    /// </summary>
    public async Task<int> RecalculateLeagueRatingsAsync(
        Guid leagueId,
        string userId,
        CancellationToken cancellationToken = default)
    {
        var memberships = await _dbContext.LeagueMemberships
            .Where(m => m.LeagueId == leagueId)
            .ToListAsync(cancellationToken);

        var membershipIds = memberships.Select(m => m.Id).ToList();
        if (membershipIds.Count == 0)
        {
            return 0;
        }

        var completedGames = await _dbContext.Games
            .Include(g => g.Participants)
            .Where(g => g.LeagueId == leagueId && g.Status == GameStatus.Completed)
            .OrderBy(g => g.StartTime)
            .ThenBy(g => g.CreatedOn)
            .ToListAsync(cancellationToken);

        var existingHistory = await _dbContext.RatingHistory
            .Where(h => membershipIds.Contains(h.LeagueMembershipId))
            .ToListAsync(cancellationToken);

        _dbContext.RatingHistory.RemoveRange(existingHistory);

        var currentRatings = memberships.ToDictionary(
            m => m.Id,
            _ => RatingSnapshot.CreateDefault());

        foreach (var membership in memberships)
        {
            var defaults = RatingSnapshot.CreateDefault();
            membership.Glicko2Rating = defaults.Rating;
            membership.Glicko2RatingDeviation = defaults.RatingDeviation;
            membership.Glicko2Volatility = defaults.Volatility;
            membership.LastRatingUpdate = null;
            membership.ModifiedBy = userId;
            membership.ModifiedOn = DateTime.UtcNow;
        }

        var gamesUpdated = 0;
        var now = DateTime.UtcNow;

        foreach (var game in completedGames)
        {
            var technicalLossCulprits = game.Participants
                .Where(p => p.IsTechnicalLoss)
                .ToList();

            if (technicalLossCulprits.Count > 0)
            {
                foreach (var culprit in technicalLossCulprits)
                {
                    var current = currentRatings[culprit.LeagueMembershipId];
                    culprit.RatingAtGameTime = current.Rating;
                    culprit.RatingDeviationAtGameTime = current.RatingDeviation;
                    culprit.VolatilityAtGameTime = current.Volatility;

                    var update = _ratingService.ApplyTechnicalLossPenalty(current);
                    ApplyUpdate(currentRatings, memberships, culprit.LeagueMembershipId, update, game, userId, now);
                }

                // Non-culprit participants keep their current rating as the at-game-time snapshot
                foreach (var participant in game.Participants.Where(p => !p.IsTechnicalLoss))
                {
                    var current = currentRatings[participant.LeagueMembershipId];
                    participant.RatingAtGameTime = current.Rating;
                    participant.RatingDeviationAtGameTime = current.RatingDeviation;
                    participant.VolatilityAtGameTime = current.Volatility;
                }

                gamesUpdated++;
                continue;
            }

            if (game.WinnerId is null)
            {
                _logger.LogWarning(
                    "Skipping completed game {GameId} in league {LeagueId}: no winner and no technical loss",
                    game.Id,
                    leagueId);
                continue;
            }

            var winnerId = game.WinnerId.Value;
            if (game.Participants.All(p => p.LeagueMembershipId != winnerId))
            {
                _logger.LogWarning(
                    "Skipping completed game {GameId} in league {LeagueId}: winner {WinnerId} is not a participant",
                    game.Id,
                    leagueId,
                    winnerId);
                continue;
            }

            var participantRatings = new Dictionary<Guid, RatingSnapshot>();
            foreach (var participant in game.Participants)
            {
                var current = currentRatings[participant.LeagueMembershipId];
                participant.RatingAtGameTime = current.Rating;
                participant.RatingDeviationAtGameTime = current.RatingDeviation;
                participant.VolatilityAtGameTime = current.Volatility;
                participantRatings[participant.LeagueMembershipId] = current;
            }

            var updates = _ratingService.CalculateMultiPlayerGameRatings(participantRatings, winnerId);
            foreach (var (membershipId, update) in updates)
            {
                ApplyUpdate(currentRatings, memberships, membershipId, update, game, userId, now);
            }

            gamesUpdated++;
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Replayed ratings for league {LeagueId}: {GameCount} games, {MemberCount} members, by user {UserId}",
            leagueId,
            gamesUpdated,
            memberships.Count,
            userId);

        return gamesUpdated;
    }

    private void ApplyUpdate(
        Dictionary<Guid, RatingSnapshot> currentRatings,
        List<LeagueMembership> memberships,
        Guid membershipId,
        RatingUpdate update,
        Game game,
        string userId,
        DateTime now)
    {
        currentRatings[membershipId] = update.NewRating;

        var membership = memberships.First(m => m.Id == membershipId);
        membership.Glicko2Rating = update.NewRating.Rating;
        membership.Glicko2RatingDeviation = update.NewRating.RatingDeviation;
        membership.Glicko2Volatility = update.NewRating.Volatility;
        membership.LastRatingUpdate = game.EndTime ?? game.StartTime;
        membership.ModifiedBy = userId;
        membership.ModifiedOn = now;

        _dbContext.RatingHistory.Add(new RatingHistory
        {
            Id = Guid.NewGuid(),
            LeagueMembershipId = membershipId,
            GameId = game.Id,
            CalculatedAt = game.EndTime ?? game.StartTime,
            PreviousRating = update.PreviousRating.Rating,
            PreviousRatingDeviation = update.PreviousRating.RatingDeviation,
            PreviousVolatility = update.PreviousRating.Volatility,
            NewRating = update.NewRating.Rating,
            NewRatingDeviation = update.NewRating.RatingDeviation,
            NewVolatility = update.NewRating.Volatility,
            CreatedBy = userId,
            CreatedOn = now
        });
    }
}
