using ScoreBurrow.Data.Entities;

namespace ScoreBurrow.Data.Statistics;

public readonly record struct PlayerParticipation(
    int TownId,
    int? HeroId,
    int Position,
    bool IsWinner,
    bool IsTechnicalLoss);

public sealed class PlayerStatsProjection
{
    public int GamesPlayed { get; init; }
    public int GamesWon { get; init; }
    public int TechnicalLosses { get; init; }
    public decimal WinRate { get; init; }
    public decimal AveragePosition { get; init; }
    public int FavoriteTownId { get; init; }
    public int? FavoriteHeroId { get; init; }
}

public static class PlayerStatisticsCalculator
{
    public static PlayerStatsProjection FromParticipations(IReadOnlyList<PlayerParticipation> participations)
    {
        ArgumentNullException.ThrowIfNull(participations);
        if (participations.Count == 0)
        {
            throw new ArgumentException("Cannot project statistics from an empty participation list.", nameof(participations));
        }

        var gamesPlayed = participations.Count;
        var gamesWon = participations.Count(p => p.IsWinner);
        var technicalLosses = participations.Count(p => p.IsTechnicalLoss);

        var favoriteTownId = participations
            .GroupBy(p => p.TownId)
            .OrderByDescending(g => g.Count())
            .Select(g => g.Key)
            .First();

        var favoriteHeroId = participations
            .Where(p => p.HeroId.HasValue)
            .GroupBy(p => p.HeroId)
            .OrderByDescending(g => g.Count())
            .Select(g => g.Key)
            .FirstOrDefault();

        return new PlayerStatsProjection
        {
            GamesPlayed = gamesPlayed,
            GamesWon = gamesWon,
            TechnicalLosses = technicalLosses,
            WinRate = (decimal)gamesWon * 100 / gamesPlayed,
            AveragePosition = (decimal)participations.Average(p => p.Position),
            FavoriteTownId = favoriteTownId,
            FavoriteHeroId = favoriteHeroId
        };
    }

    public static void Apply(PlayerStatistics statistics, PlayerStatsProjection projection, DateTime lastUpdated)
    {
        statistics.GamesPlayed = projection.GamesPlayed;
        statistics.GamesWon = projection.GamesWon;
        statistics.TechnicalLosses = projection.TechnicalLosses;
        statistics.WinRate = projection.WinRate;
        statistics.AveragePosition = projection.AveragePosition;
        statistics.FavoriteTownId = projection.FavoriteTownId;
        statistics.FavoriteHeroId = projection.FavoriteHeroId;
        statistics.LastUpdated = lastUpdated;
    }
}
