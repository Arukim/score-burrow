using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Primitives;
using ScoreBurrow.Data;
using ScoreBurrow.Data.Entities;
using ScoreBurrow.Data.Enums;
using ScoreBurrow.Data.Statistics;
using ScoreBurrow.Rating.Models;
using ScoreBurrow.Rating.Services;
using ScoreBurrow.Web.Models;
using ScoreBurrow.Web.Services;

namespace ScoreBurrow.Web.Tests;

public class GameServiceDeleteGameTests
{
    [Fact]
    public async Task DeleteCompletedGame_ReplaysRemainingGamesAndRefreshesStatistics()
    {
        var leagueId = Guid.NewGuid();
        var playerA = Guid.NewGuid();
        var playerB = Guid.NewGuid();
        var earlier = new DateTime(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);
        var later = earlier.AddDays(7);

        await using var context = CreateContext();
        SeedLeague(context, leagueId, playerA, playerB);
        var deletedGameId = SeedCompletedGame(context, leagueId, winnerId: playerA, loserId: playerB, earlier);
        var keptGameId = SeedCompletedGame(context, leagueId, winnerId: playerB, loserId: playerA, later);
        SeedStaleStatistics(context, playerA, playerB);
        await context.SaveChangesAsync();
        SpoilRatings(context, playerA, playerB);

        var leagues = new StubLeagueService();
        var service = CreateService(context, leagues);

        var deleted = await service.DeleteGameAsync(deletedGameId, "admin");

        deleted.Should().BeTrue();
        (await context.Games.AnyAsync(g => g.Id == deletedGameId)).Should().BeFalse();
        (await context.GameParticipants.AnyAsync(p => p.GameId == deletedGameId)).Should().BeFalse();
        (await context.RatingHistory.AnyAsync(h => h.GameId == deletedGameId)).Should().BeFalse();
        (await context.Games.AnyAsync(g => g.Id == keptGameId)).Should().BeTrue();
        (await context.GameParticipants.CountAsync(p => p.GameId == keptGameId)).Should().Be(2);

        var historyGameIds = await context.RatingHistory.Select(h => h.GameId).Distinct().ToListAsync();
        historyGameIds.Should().Equal(keptGameId);

        var expected = new RatingService().CalculateMultiPlayerGameRatings(
            new Dictionary<Guid, RatingSnapshot>
            {
                [playerA] = RatingSnapshot.CreateDefault(),
                [playerB] = RatingSnapshot.CreateDefault()
            },
            playerB);

        var membershipA = await context.LeagueMemberships.SingleAsync(m => m.Id == playerA);
        var membershipB = await context.LeagueMemberships.SingleAsync(m => m.Id == playerB);
        AssertRating(membershipA, expected[playerA].NewRating);
        AssertRating(membershipB, expected[playerB].NewRating);

        var statsA = await context.PlayerStatistics.SingleAsync(s => s.LeagueMembershipId == playerA);
        var statsB = await context.PlayerStatistics.SingleAsync(s => s.LeagueMembershipId == playerB);
        statsA.GamesPlayed.Should().Be(1);
        statsA.GamesWon.Should().Be(0);
        statsB.GamesPlayed.Should().Be(1);
        statsB.GamesWon.Should().Be(1);

        leagues.InvalidateCount.Should().Be(1);
        leagues.LastLeagueId.Should().Be(leagueId);
        leagues.LastGameId.Should().Be(deletedGameId);
    }

    [Fact]
    public async Task DeleteOnlyCompletedGame_ResetsRatingsAndClearsStatistics()
    {
        var leagueId = Guid.NewGuid();
        var playerA = Guid.NewGuid();
        var playerB = Guid.NewGuid();

        await using var context = CreateContext();
        SeedLeague(context, leagueId, playerA, playerB);
        var gameId = SeedCompletedGame(
            context,
            leagueId,
            winnerId: playerA,
            loserId: playerB,
            new DateTime(2026, 2, 1, 12, 0, 0, DateTimeKind.Utc));
        SeedStaleStatistics(context, playerA, playerB);
        await context.SaveChangesAsync();
        SpoilRatings(context, playerA, playerB);

        var service = CreateService(context, new StubLeagueService());
        (await service.DeleteGameAsync(gameId, "admin")).Should().BeTrue();

        (await context.Games.CountAsync()).Should().Be(0);
        (await context.GameParticipants.CountAsync()).Should().Be(0);
        (await context.RatingHistory.CountAsync()).Should().Be(0);
        (await context.PlayerStatistics.CountAsync()).Should().Be(0);

        var defaults = RatingSnapshot.CreateDefault();
        var memberships = await context.LeagueMemberships.Where(m => m.LeagueId == leagueId).ToListAsync();
        memberships.Should().HaveCount(2);
        foreach (var membership in memberships)
        {
            AssertRating(membership, defaults);
            membership.LastRatingUpdate.Should().BeNull();
        }
    }

    [Fact]
    public async Task DeleteInProgressGame_LeavesCompletedRatingsIntact()
    {
        var leagueId = Guid.NewGuid();
        var playerA = Guid.NewGuid();
        var playerB = Guid.NewGuid();

        await using var context = CreateContext();
        SeedLeague(context, leagueId, playerA, playerB);
        var completedId = SeedCompletedGame(
            context,
            leagueId,
            winnerId: playerA,
            loserId: playerB,
            new DateTime(2026, 3, 1, 12, 0, 0, DateTimeKind.Utc));
        await context.SaveChangesAsync();
        SpoilRatings(context, playerA, playerB);

        var leagues = new StubLeagueService();
        var service = CreateService(context, leagues);
        var inProgressId = await service.CreateGameAsync(leagueId, "admin", TwoPlayerRequest(playerA, playerB));

        (await service.DeleteGameAsync(inProgressId, "admin")).Should().BeTrue();

        (await context.Games.AnyAsync(g => g.Id == inProgressId)).Should().BeFalse();
        (await context.Games.AnyAsync(g => g.Id == completedId)).Should().BeTrue();

        var expected = new RatingService().CalculateMultiPlayerGameRatings(
            new Dictionary<Guid, RatingSnapshot>
            {
                [playerA] = RatingSnapshot.CreateDefault(),
                [playerB] = RatingSnapshot.CreateDefault()
            },
            playerA);

        AssertRating(await context.LeagueMemberships.SingleAsync(m => m.Id == playerA), expected[playerA].NewRating);
        AssertRating(await context.LeagueMemberships.SingleAsync(m => m.Id == playerB), expected[playerB].NewRating);
    }

    [Fact]
    public async Task DeleteCancelledGame_RemovesIt()
    {
        var leagueId = Guid.NewGuid();
        var playerA = Guid.NewGuid();
        var playerB = Guid.NewGuid();

        await using var context = CreateContext();
        SeedLeague(context, leagueId, playerA, playerB);
        await context.SaveChangesAsync();

        var service = CreateService(context, new StubLeagueService());
        var gameId = await service.CreateGameAsync(leagueId, "admin", TwoPlayerRequest(playerA, playerB));
        (await service.CancelGameAsync(gameId, "admin")).Should().BeTrue();

        (await service.DeleteGameAsync(gameId, "admin")).Should().BeTrue();

        (await context.Games.AnyAsync(g => g.Id == gameId)).Should().BeFalse();
        (await context.GameParticipants.AnyAsync(p => p.GameId == gameId)).Should().BeFalse();
    }

    [Fact]
    public async Task DeleteGame_RejectsNonAdmin()
    {
        var leagueId = Guid.NewGuid();
        var playerA = Guid.NewGuid();
        var playerB = Guid.NewGuid();

        await using var context = CreateContext();
        SeedLeague(context, leagueId, playerA, playerB);
        var gameId = SeedCompletedGame(
            context,
            leagueId,
            winnerId: playerA,
            loserId: playerB,
            new DateTime(2026, 4, 1, 12, 0, 0, DateTimeKind.Utc));
        await context.SaveChangesAsync();

        var leagues = new StubLeagueService { IsAdmin = false };
        var service = CreateService(context, leagues);

        var act = async () => await service.DeleteGameAsync(gameId, "member");

        await act.Should().ThrowAsync<UnauthorizedAccessException>();
        (await context.Games.AnyAsync(g => g.Id == gameId)).Should().BeTrue();
        leagues.InvalidateCount.Should().Be(0);
    }

    [Fact]
    public async Task DeleteGame_ReturnsFalseWhenMissing()
    {
        await using var context = CreateContext();
        var leagues = new StubLeagueService();
        var service = CreateService(context, leagues);

        (await service.DeleteGameAsync(Guid.NewGuid(), "admin")).Should().BeFalse();
        leagues.InvalidateCount.Should().Be(0);
    }

    private static void AssertRating(LeagueMembership membership, RatingSnapshot expected)
    {
        membership.Glicko2Rating.Should().BeApproximately(expected.Rating, 0.001);
        membership.Glicko2RatingDeviation.Should().BeApproximately(expected.RatingDeviation, 0.001);
        membership.Glicko2Volatility.Should().BeApproximately(expected.Volatility, 0.000001);
    }

    private static void SpoilRatings(ScoreBurrowDbContext context, params Guid[] membershipIds)
    {
        foreach (var membership in context.LeagueMemberships.Where(m => membershipIds.Contains(m.Id)))
        {
            membership.Glicko2Rating = 2000;
            membership.Glicko2RatingDeviation = 10;
            membership.Glicko2Volatility = 0.09;
        }
    }

    private static Guid SeedCompletedGame(
        ScoreBurrowDbContext context,
        Guid leagueId,
        Guid winnerId,
        Guid loserId,
        DateTime start)
    {
        var gameId = Guid.NewGuid();
        context.Games.Add(new Game
        {
            Id = gameId,
            LeagueId = leagueId,
            MapName = "Jebus",
            StartTime = start,
            EndTime = start.AddHours(2),
            Status = GameStatus.Completed,
            WinnerId = winnerId,
            CreatedBy = "admin",
            CreatedOn = start
        });

        context.GameParticipants.Add(Participant(gameId, winnerId, PlayerColor.Red, position: 1, isWinner: true, start));
        context.GameParticipants.Add(Participant(gameId, loserId, PlayerColor.Blue, position: 2, isWinner: false, start));
        context.RatingHistory.Add(History(gameId, winnerId, start));
        context.RatingHistory.Add(History(gameId, loserId, start));
        return gameId;
    }

    private static GameParticipant Participant(
        Guid gameId,
        Guid membershipId,
        PlayerColor color,
        int position,
        bool isWinner,
        DateTime createdOn) =>
        new()
        {
            Id = Guid.NewGuid(),
            GameId = gameId,
            LeagueMembershipId = membershipId,
            TownId = color == PlayerColor.Red ? 1 : 2,
            PlayerColor = color,
            Position = position,
            IsWinner = isWinner,
            CreatedBy = "admin",
            CreatedOn = createdOn
        };

    private static RatingHistory History(Guid gameId, Guid membershipId, DateTime calculatedAt) =>
        new()
        {
            Id = Guid.NewGuid(),
            GameId = gameId,
            LeagueMembershipId = membershipId,
            CalculatedAt = calculatedAt,
            PreviousRating = 1500,
            PreviousRatingDeviation = 350,
            PreviousVolatility = 0.06,
            NewRating = 1600,
            NewRatingDeviation = 300,
            NewVolatility = 0.06,
            CreatedBy = "admin",
            CreatedOn = calculatedAt
        };

    private static void SeedStaleStatistics(ScoreBurrowDbContext context, params Guid[] membershipIds)
    {
        foreach (var membershipId in membershipIds)
        {
            context.PlayerStatistics.Add(new PlayerStatistics
            {
                Id = Guid.NewGuid(),
                LeagueMembershipId = membershipId,
                GamesPlayed = 50,
                GamesWon = 50,
                WinRate = 1,
                AveragePosition = 1,
                LastUpdated = DateTime.UtcNow
            });
        }
    }

    private static GameService CreateService(ScoreBurrowDbContext context, StubLeagueService leagues) =>
        new(
            context,
            leagues,
            new RatingService(),
            new PlayerStatisticsProjector(context),
            new RatingReplayService(context, new RatingService(), NullLogger<RatingReplayService>.Instance));

    private static ScoreBurrowDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ScoreBurrowDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;
        var context = new ScoreBurrowDbContext(options);
        context.Database.EnsureCreated();
        return context;
    }

    private static void SeedLeague(ScoreBurrowDbContext context, Guid leagueId, params Guid[] membershipIds)
    {
        context.Leagues.Add(new League
        {
            Id = leagueId,
            Name = "Test League",
            OwnerId = "admin",
            CreatedDate = DateTime.UtcNow,
            CreatedBy = "admin",
            CreatedOn = DateTime.UtcNow
        });

        foreach (var membershipId in membershipIds)
        {
            context.LeagueMemberships.Add(new LeagueMembership
            {
                Id = membershipId,
                LeagueId = leagueId,
                PlayerNickname = membershipId.ToString("N")[..8],
                Role = LeagueRole.Member,
                JoinedDate = DateTime.UtcNow,
                CreatedBy = "admin",
                CreatedOn = DateTime.UtcNow
            });
        }
    }

    private static CreateGameRequest TwoPlayerRequest(Guid playerA, Guid playerB) =>
        new()
        {
            MapName = "XL",
            Participants =
            [
                new ParticipantRequest
                {
                    LeagueMembershipId = playerA,
                    PlayerColor = PlayerColor.Red,
                    Position = 1,
                    TownId = 1
                },
                new ParticipantRequest
                {
                    LeagueMembershipId = playerB,
                    PlayerColor = PlayerColor.Blue,
                    Position = 2,
                    TownId = 2
                }
            ]
        };

    private sealed class StubLeagueService : ILeagueService
    {
        public bool IsAdmin { get; set; } = true;
        public int InvalidateCount { get; set; }
        public Guid? LastLeagueId { get; private set; }
        public Guid? LastGameId { get; private set; }

        public Task<bool> IsAdminOrOwnerAsync(string userId, Guid leagueId) => Task.FromResult(IsAdmin);

        public void InvalidateLeagueCache(Guid leagueId, string? userId = null, Guid? gameId = null)
        {
            InvalidateCount++;
            LastLeagueId = leagueId;
            LastGameId = gameId;
        }

        public Task<Guid> CreateLeagueAsync(string userId, string name, string? description) => throw new NotImplementedException();
        public Task<bool> UpdateLeagueAsync(Guid leagueId, string userId, string name, string? description) => throw new NotImplementedException();
        public Task<bool> ArchiveLeagueAsync(Guid leagueId, string userId) => throw new NotImplementedException();
        public Task<bool> UnarchiveLeagueAsync(Guid leagueId, string userId) => throw new NotImplementedException();
        public Task<bool> DeleteLeagueAsync(Guid leagueId, string userId) => throw new NotImplementedException();
        public Task<bool> AddMemberAsync(Guid leagueId, string userId, string memberEmail, string? displayName = null) => throw new NotImplementedException();
        public Task<bool> AddUnregisteredMemberAsync(Guid leagueId, string userId, string playerNickname, string? playerDisplayName = null) => throw new NotImplementedException();
        public Task<bool> LinkMemberAsync(Guid leagueId, string userId, Guid membershipId, string memberEmail) => throw new NotImplementedException();
        public Task<bool> UpdateMemberUserAsync(Guid leagueId, string userId, Guid membershipId, string newEmail) => throw new NotImplementedException();
        public Task<bool> UpdateMemberRoleAsync(Guid leagueId, string userId, Guid membershipId, LeagueRole newRole) => throw new NotImplementedException();
        public Task<bool> RemoveMemberAsync(Guid leagueId, string userId, Guid membershipId) => throw new NotImplementedException();
        public Task<bool> IsOwnerAsync(string userId, Guid leagueId) => throw new NotImplementedException();
        public Task<bool> IsMemberAsync(string userId, Guid leagueId) => throw new NotImplementedException();
        public Task<bool> RecalculateStatisticsAsync(Guid leagueId, string userId) => throw new NotImplementedException();
        public Task<bool> RecalculateRatingsAsync(Guid leagueId, string userId) => throw new NotImplementedException();
        public IChangeToken GetLeagueCacheExpirationToken(Guid leagueId) => throw new NotImplementedException();
    }
}
