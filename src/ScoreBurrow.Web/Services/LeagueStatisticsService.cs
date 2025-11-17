using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using ScoreBurrow.Data;
using ScoreBurrow.Data.Enums;
using ScoreBurrow.Web.Models;

namespace ScoreBurrow.Web.Services;

public class LeagueStatisticsService : ILeagueStatisticsService
{
    private readonly ScoreBurrowDbContext _dbContext;
    private readonly IMemoryCache _memoryCache;
    private readonly ILogger<LeagueStatisticsService> _logger;

    public LeagueStatisticsService(
        ScoreBurrowDbContext dbContext,
        IMemoryCache memoryCache,
        ILogger<LeagueStatisticsService> logger)
    {
        _dbContext = dbContext;
        _memoryCache = memoryCache;
        _logger = logger;
    }

    public async Task<List<TownStatisticsDto>> GetLeagueTownStatisticsAsync(Guid leagueId, int daysWindow = 365)
    {
        var cacheKey = $"league_town_stats_{leagueId}_{daysWindow}";
        
        if (_memoryCache.TryGetValue<List<TownStatisticsDto>>(cacheKey, out var cachedStats))
        {
            return cachedStats!;
        }

        _logger.LogInformation("Calculating town statistics for league {LeagueId} with {Days} days window", leagueId, daysWindow);

        var cutoffDate = DateTime.UtcNow.AddDays(-daysWindow);

        // Get all completed game participants within time window
        var participants = await _dbContext.GameParticipants
            .AsNoTracking()
            .Include(gp => gp.Game)
            .Include(gp => gp.Town)
            .Include(gp => gp.Hero)
            .Where(gp => gp.Game.LeagueId == leagueId 
                && gp.Game.Status == GameStatus.Completed
                && gp.Game.StartTime >= cutoffDate)
            .ToListAsync();

        if (participants.Count == 0)
        {
            _logger.LogInformation("No completed games found for league {LeagueId} in time window", leagueId);
            return new List<TownStatisticsDto>();
        }

        // Calculate league-wide median gold trade for value categorization
        var allGoldTrades = participants.Select(p => p.GoldTrade).ToList();
        var leagueMedianGold = CalculateMedian(allGoldTrades);

        // Group by town
        var townGroups = participants.GroupBy(p => p.TownId);
        var townStats = new List<TownStatisticsDto>();

        foreach (var townGroup in townGroups)
        {
            var townId = townGroup.Key;
            var townName = townGroup.First().Town.Name;
            var townParticipants = townGroup.ToList();

            var gamesPlayed = townParticipants.Count;
            var wins = townParticipants.Count(p => p.IsWinner);
            var winRate = gamesPlayed > 0 ? (decimal)wins * 100 / gamesPlayed : 0;

            // Calculate gold trade statistics
            var goldTrades = townParticipants.Select(p => p.GoldTrade).ToList();
            var avgGoldTrade = (int)Math.Round(goldTrades.Average() / 100.0) * 100; // Round to /100
            var medianGoldTrade = (int)Math.Round(CalculateMedian(goldTrades) / 500.0) * 500; // Round to /500

            // Determine value category (only if >= 10 games)
            TownValueCategory? valueCategory = null;
            if (gamesPlayed >= 10)
            {
                if (medianGoldTrade > leagueMedianGold + 500 && winRate < 40)
                {
                    valueCategory = TownValueCategory.Overhyped;
                }
                else if (medianGoldTrade < leagueMedianGold - 500 && winRate > 60)
                {
                    valueCategory = TownValueCategory.Undervalued;
                }
            }

            // Calculate hero statistics within this town
            var heroStats = CalculateHeroStatistics(townParticipants, gamesPlayed);

            townStats.Add(new TownStatisticsDto
            {
                TownId = townId,
                TownName = townName,
                GamesPlayed = gamesPlayed,
                Wins = wins,
                WinRate = winRate,
                AvgGoldTrade = avgGoldTrade,
                MedianGoldTrade = medianGoldTrade,
                ValueCategory = valueCategory,
                TopWinningHeroes = heroStats.TopWinning,
                TopLosingHeroes = heroStats.TopLosing
            });
        }

        // Order by games played descending
        var result = townStats.OrderByDescending(t => t.GamesPlayed).ToList();

        // Cache for 1 hour
        var cacheOptions = new MemoryCacheEntryOptions()
            .SetSlidingExpiration(TimeSpan.FromHours(1));
        _memoryCache.Set(cacheKey, result, cacheOptions);

        _logger.LogInformation("Calculated statistics for {Count} towns in league {LeagueId}", result.Count, leagueId);

        return result;
    }

    public async Task<PlayerPerformanceDto> GetPlayerPerformanceAsync(
        Guid leagueMembershipId,
        int minGames = 3,
        int daysWindow = 365)
    {
        var cacheKey = $"player_performance_{leagueMembershipId}_{minGames}_{daysWindow}";
        
        if (_memoryCache.TryGetValue<PlayerPerformanceDto>(cacheKey, out var cachedPerformance))
        {
            return cachedPerformance!;
        }

        _logger.LogInformation("Calculating player performance for membership {MembershipId}", leagueMembershipId);

        var cutoffDate = DateTime.UtcNow.AddDays(-daysWindow);

        // Get all completed game participants for this player within time window
        var participants = await _dbContext.GameParticipants
            .AsNoTracking()
            .Include(gp => gp.Game)
            .Include(gp => gp.Town)
            .Include(gp => gp.Hero)
            .Where(gp => gp.LeagueMembershipId == leagueMembershipId
                && gp.Game.Status == GameStatus.Completed
                && gp.Game.StartTime >= cutoffDate)
            .ToListAsync();

        var result = new PlayerPerformanceDto
        {
            LeagueMembershipId = leagueMembershipId
        };

        if (participants.Count == 0)
        {
            return result;
        }

        // Calculate town performance
        var townPerformances = participants
            .GroupBy(p => p.TownId)
            .Select(g => new TownPerformanceDto
            {
                TownId = g.Key,
                TownName = g.First().Town.Name,
                GamesPlayed = g.Count(),
                Wins = g.Count(p => p.IsWinner),
                WinRate = g.Count() > 0 ? (decimal)g.Count(p => p.IsWinner) * 100 / g.Count() : 0
            })
            .Where(t => t.GamesPlayed >= minGames)
            .ToList();

        result.BestTowns = townPerformances
            .OrderByDescending(t => t.WinRate)
            .ThenByDescending(t => t.GamesPlayed)
            .Take(3)
            .ToList();

        result.WorstTowns = townPerformances
            .OrderBy(t => t.WinRate)
            .ThenByDescending(t => t.GamesPlayed)
            .Take(3)
            .ToList();

        // Calculate hero performance (only where hero data exists)
        var heroParticipants = participants.Where(p => p.HeroId.HasValue && p.Hero != null).ToList();
        
        if (heroParticipants.Any())
        {
            var heroPerformances = heroParticipants
                .GroupBy(p => p.HeroId!.Value)
                .Select(g => new HeroPerformanceDto
                {
                    HeroId = g.Key,
                    HeroName = g.First().Hero!.Name,
                    GamesPlayed = g.Count(),
                    Wins = g.Count(p => p.IsWinner),
                    WinRate = g.Count() > 0 ? (decimal)g.Count(p => p.IsWinner) * 100 / g.Count() : 0
                })
                .Where(h => h.GamesPlayed >= minGames)
                .ToList();

            result.BestHeroes = heroPerformances
                .OrderByDescending(h => h.WinRate)
                .ThenByDescending(h => h.GamesPlayed)
                .Take(3)
                .ToList();

            result.WorstHeroes = heroPerformances
                .OrderBy(h => h.WinRate)
                .ThenByDescending(h => h.GamesPlayed)
                .Take(3)
                .ToList();
        }

        // Cache for 1 hour
        var cacheOptions = new MemoryCacheEntryOptions()
            .SetSlidingExpiration(TimeSpan.FromHours(1));
        _memoryCache.Set(cacheKey, result, cacheOptions);

        return result;
    }

    public async Task<TownPerformanceDto?> GetPlayerBestTownAsync(
        Guid leagueMembershipId,
        int minGames = 3,
        int daysWindow = 365)
    {
        var performance = await GetPlayerPerformanceAsync(leagueMembershipId, minGames, daysWindow);
        return performance.BestTowns.FirstOrDefault();
    }

    /// <summary>
    /// Calculate hero statistics within a town
    /// </summary>
    private (List<HeroStatisticsDto> TopWinning, List<HeroStatisticsDto> TopLosing) CalculateHeroStatistics(
        List<Data.Entities.GameParticipant> townParticipants,
        int totalTownGames)
    {
        // Filter out participants without hero data
        var heroParticipants = townParticipants
            .Where(p => p.HeroId.HasValue && p.Hero != null)
            .ToList();

        if (!heroParticipants.Any())
        {
            return (new List<HeroStatisticsDto>(), new List<HeroStatisticsDto>());
        }

        var heroStats = heroParticipants
            .GroupBy(p => p.HeroId!.Value)
            .Select(g =>
            {
                var gamesPlayed = g.Count();
                var wins = g.Count(p => p.IsWinner);
                var winRate = gamesPlayed > 0 ? (decimal)wins * 100 / gamesPlayed : 0;
                var pickRate = totalTownGames > 0 ? (decimal)gamesPlayed * 100 / totalTownGames : 0;

                return new HeroStatisticsDto
                {
                    HeroId = g.Key,
                    HeroName = g.First().Hero!.Name,
                    GamesPlayed = gamesPlayed,
                    Wins = wins,
                    WinRate = winRate,
                    PickRate = pickRate
                };
            })
            .ToList();

        var topWinning = heroStats
            .OrderByDescending(h => h.WinRate)
            .ThenByDescending(h => h.GamesPlayed)
            .Take(5)
            .ToList();

        var topLosing = heroStats
            .OrderBy(h => h.WinRate)
            .ThenByDescending(h => h.GamesPlayed)
            .Take(5)
            .ToList();

        return (topWinning, topLosing);
    }

    /// <summary>
    /// Calculate median value from a list of integers
    /// </summary>
    private double CalculateMedian(List<int> values)
    {
        if (values.Count == 0)
            return 0;

        var sorted = values.OrderBy(v => v).ToList();
        int mid = sorted.Count / 2;

        if (sorted.Count % 2 == 0)
        {
            // Even number of elements - average of two middle values
            return (sorted[mid - 1] + sorted[mid]) / 2.0;
        }
        else
        {
            // Odd number of elements - middle value
            return sorted[mid];
        }
    }
}
