using FluentAssertions;
using ScoreBurrow.Data.Entities;
using ScoreBurrow.Data.Statistics;

namespace ScoreBurrow.Data.Tests;

public class PlayerStatisticsCalculatorTests
{
    [Fact]
    public void FromParticipations_Empty_Throws()
    {
        var act = () => PlayerStatisticsCalculator.FromParticipations(Array.Empty<PlayerParticipation>());

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void FromParticipations_ComputesWinRateAveragePositionAndFavorites()
    {
        var participations = new[]
        {
            new PlayerParticipation(TownId: 1, HeroId: 10, Position: 1, IsWinner: true, IsTechnicalLoss: false),
            new PlayerParticipation(TownId: 1, HeroId: 11, Position: 2, IsWinner: false, IsTechnicalLoss: false),
            new PlayerParticipation(TownId: 2, HeroId: 10, Position: 3, IsWinner: false, IsTechnicalLoss: true),
        };

        var projection = PlayerStatisticsCalculator.FromParticipations(participations);

        projection.GamesPlayed.Should().Be(3);
        projection.GamesWon.Should().Be(1);
        projection.TechnicalLosses.Should().Be(1);
        projection.WinRate.Should().Be(100m / 3);
        projection.AveragePosition.Should().Be(2m);
        projection.FavoriteTownId.Should().Be(1);
        projection.FavoriteHeroId.Should().Be(10);
    }

    [Fact]
    public void FromParticipations_NoHeroes_LeavesFavoriteHeroNull()
    {
        var participations = new[]
        {
            new PlayerParticipation(TownId: 4, HeroId: null, Position: 2, IsWinner: false, IsTechnicalLoss: false),
        };

        var projection = PlayerStatisticsCalculator.FromParticipations(participations);

        projection.FavoriteTownId.Should().Be(4);
        projection.FavoriteHeroId.Should().BeNull();
        projection.WinRate.Should().Be(0m);
    }

    [Fact]
    public void Apply_WritesEveryProfileField()
    {
        var stats = new PlayerStatistics { Id = Guid.NewGuid(), LeagueMembershipId = Guid.NewGuid() };
        var projection = new PlayerStatsProjection
        {
            GamesPlayed = 4,
            GamesWon = 1,
            TechnicalLosses = 1,
            WinRate = 25m,
            AveragePosition = 2.5m,
            FavoriteTownId = 7,
            FavoriteHeroId = 12
        };
        var lastUpdated = new DateTime(2026, 9, 21, 12, 0, 0, DateTimeKind.Utc);

        PlayerStatisticsCalculator.Apply(stats, projection, lastUpdated);

        stats.GamesPlayed.Should().Be(4);
        stats.GamesWon.Should().Be(1);
        stats.TechnicalLosses.Should().Be(1);
        stats.WinRate.Should().Be(25m);
        stats.AveragePosition.Should().Be(2.5m);
        stats.FavoriteTownId.Should().Be(7);
        stats.FavoriteHeroId.Should().Be(12);
        stats.LastUpdated.Should().Be(lastUpdated);
    }
}
