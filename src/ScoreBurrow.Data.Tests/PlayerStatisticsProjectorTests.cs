using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using ScoreBurrow.Data.Entities;
using ScoreBurrow.Data.Enums;
using ScoreBurrow.Data.Statistics;

namespace ScoreBurrow.Data.Tests;

public class PlayerStatisticsProjectorTests
{
    [Fact]
    public async Task RecalculateMemberships_UsesCompletedGamesOnlyAndFillsProfileFields()
    {
        var leagueId = Guid.NewGuid();
        var membershipId = Guid.NewGuid();

        await using var context = CreateContext();
        SeedLeagueAndMember(context, leagueId, membershipId);

        AddGame(context, leagueId, membershipId, GameStatus.Completed, townId: 1, heroId: 1, position: 1, isWinner: true, isTechnicalLoss: false);
        AddGame(context, leagueId, membershipId, GameStatus.Completed, townId: 1, heroId: 2, position: 3, isWinner: false, isTechnicalLoss: true);
        AddGame(context, leagueId, membershipId, GameStatus.InProgress, townId: 2, heroId: 3, position: 1, isWinner: false, isTechnicalLoss: false);
        AddGame(context, leagueId, membershipId, GameStatus.Cancelled, townId: 2, heroId: 3, position: 1, isWinner: false, isTechnicalLoss: false);
        await context.SaveChangesAsync();

        var projector = new PlayerStatisticsProjector(context);
        var updated = await projector.RecalculateMembershipsAsync(leagueId, new[] { membershipId });
        await context.SaveChangesAsync();

        updated.Should().Be(1);
        var stats = await context.PlayerStatistics.SingleAsync(s => s.LeagueMembershipId == membershipId);
        stats.GamesPlayed.Should().Be(2);
        stats.GamesWon.Should().Be(1);
        stats.TechnicalLosses.Should().Be(1);
        stats.WinRate.Should().Be(50m);
        stats.AveragePosition.Should().Be(2m);
        stats.FavoriteTownId.Should().Be(1);
        stats.FavoriteHeroId.Should().Be(1);
        stats.LastUpdated.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task RecalculateLeague_RemovesStatsWhenPlayerHasNoCompletedGames()
    {
        var leagueId = Guid.NewGuid();
        var membershipId = Guid.NewGuid();

        await using var context = CreateContext();
        SeedLeagueAndMember(context, leagueId, membershipId);
        context.PlayerStatistics.Add(new PlayerStatistics
        {
            Id = Guid.NewGuid(),
            LeagueMembershipId = membershipId,
            GamesPlayed = 3,
            GamesWon = 1,
            WinRate = 33.33m,
            LastUpdated = DateTime.UtcNow.AddDays(-1)
        });
        AddGame(context, leagueId, membershipId, GameStatus.Cancelled, townId: 1, heroId: 1, position: 1, isWinner: false, isTechnicalLoss: false);
        await context.SaveChangesAsync();

        var projector = new PlayerStatisticsProjector(context);
        await projector.RecalculateLeagueAsync(leagueId);
        await context.SaveChangesAsync();

        (await context.PlayerStatistics.CountAsync(s => s.LeagueMembershipId == membershipId)).Should().Be(0);
    }

    private static ScoreBurrowDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ScoreBurrowDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var context = new ScoreBurrowDbContext(options);
        context.Database.EnsureCreated();
        return context;
    }

    private static void SeedLeagueAndMember(ScoreBurrowDbContext context, Guid leagueId, Guid membershipId)
    {
        context.Leagues.Add(new League
        {
            Id = leagueId,
            Name = "Test League",
            OwnerId = "owner",
            CreatedDate = DateTime.UtcNow,
            CreatedBy = "owner",
            CreatedOn = DateTime.UtcNow
        });
        context.LeagueMemberships.Add(new LeagueMembership
        {
            Id = membershipId,
            LeagueId = leagueId,
            PlayerNickname = "tester",
            Role = LeagueRole.Member,
            JoinedDate = DateTime.UtcNow,
            CreatedBy = "owner",
            CreatedOn = DateTime.UtcNow
        });
    }

    private static void AddGame(
        ScoreBurrowDbContext context,
        Guid leagueId,
        Guid membershipId,
        GameStatus status,
        int townId,
        int heroId,
        int position,
        bool isWinner,
        bool isTechnicalLoss)
    {
        var gameId = Guid.NewGuid();
        context.Games.Add(new Game
        {
            Id = gameId,
            LeagueId = leagueId,
            MapName = "XL",
            StartTime = DateTime.UtcNow.AddHours(-1),
            EndTime = status == GameStatus.InProgress ? null : DateTime.UtcNow,
            Status = status,
            CreatedBy = "owner",
            CreatedOn = DateTime.UtcNow
        });
        context.GameParticipants.Add(new GameParticipant
        {
            Id = Guid.NewGuid(),
            GameId = gameId,
            LeagueMembershipId = membershipId,
            TownId = townId,
            HeroId = heroId,
            PlayerColor = PlayerColor.Red,
            Position = position,
            IsWinner = isWinner,
            IsTechnicalLoss = isTechnicalLoss,
            CreatedBy = "owner",
            CreatedOn = DateTime.UtcNow
        });
    }
}
