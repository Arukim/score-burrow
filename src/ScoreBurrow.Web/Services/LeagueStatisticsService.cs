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
            .Include(gp => gp.LeagueMembership)
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

        // Group by town - first pass to calculate stats
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

            // Calculate hero statistics within this town
            var heroStats = CalculateHeroStatistics(townParticipants, gamesPlayed);

            // Calculate player statistics within this town
            var playerStats = CalculatePlayerStatistics(townParticipants);

            townStats.Add(new TownStatisticsDto
            {
                TownId = townId,
                TownName = townName,
                GamesPlayed = gamesPlayed,
                Wins = wins,
                WinRate = winRate,
                AvgGoldTrade = avgGoldTrade,
                MedianGoldTrade = medianGoldTrade,
                ValueCategory = null, // Will be set in second pass
                TopWinningHeroes = heroStats.TopWinning,
                TopLosingHeroes = heroStats.TopLosing,
                TopPlayers = playerStats.TopPlayers,
                BestPlayers = playerStats.BestPlayers
            });
        }

        // Second pass: Calculate dynamic thresholds and assign value categories
        // Only consider towns with >= 10 games for threshold calculation
        var significantTowns = townStats.Where(t => t.GamesPlayed >= 10).ToList();
        
        if (significantTowns.Any())
        {
            // Calculate average win rate across all significant towns
            var avgWinRate = significantTowns.Average(t => t.WinRate);
            
            // Define high/low thresholds as 5 percentage points above/below average
            var highWinRateThreshold = avgWinRate + 5;
            var lowWinRateThreshold = avgWinRate - 5;
            
            _logger.LogInformation(
                "Win rate thresholds for league {LeagueId}: Low={Low:F1}%, Avg={Avg:F1}%, High={High:F1}%",
                leagueId, lowWinRateThreshold, avgWinRate, highWinRateThreshold);

            // Assign value categories based on dynamic thresholds
            foreach (var town in significantTowns)
            {
                // Note: Negative gold = expensive (you paid), Positive gold = cheap (you received)
                // Overhyped: Expensive (negative gold) but low win rate
                if (town.MedianGoldTrade < leagueMedianGold - 500 && town.WinRate < lowWinRateThreshold)
                {
                    town.ValueCategory = TownValueCategory.Overhyped;
                }
                // Undervalued: Cheap (positive gold) but high win rate
                else if (town.MedianGoldTrade > leagueMedianGold + 500 && town.WinRate > highWinRateThreshold)
                {
                    town.ValueCategory = TownValueCategory.Undervalued;
                }
            }
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

        // Calculate favorite towns (by pick count)
        result.FavoriteTowns = townPerformances
            .OrderByDescending(t => t.GamesPlayed)
            .ThenByDescending(t => t.WinRate)
            .Take(3)
            .ToList();

        // Calculate best weighted towns using Bayesian average (Wins + 2) / (Games + 4)
        result.BestWeightedTowns = townPerformances
            .Select(t => new TownPerformanceDto
            {
                TownId = t.TownId,
                TownName = t.TownName,
                GamesPlayed = t.GamesPlayed,
                Wins = t.Wins,
                WinRate = (decimal)((t.Wins + 2.0) / (t.GamesPlayed + 4.0) * 100)  // Weighted score as win rate representation
            })
            .OrderByDescending(t => (t.Wins + 2.0) / (t.GamesPlayed + 4.0))
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
    /// Calculate player statistics within a town (for expandable details)
    /// </summary>
    private (List<PlayerTownStatsDto> TopPlayers, List<PlayerTownStatsDto> BestPlayers) CalculatePlayerStatistics(
        List<Data.Entities.GameParticipant> townParticipants)
    {
        var playerStats = townParticipants
            .GroupBy(p => p.LeagueMembershipId)
            .Select(g =>
            {
                var gamesPlayed = g.Count();
                var wins = g.Count(p => p.IsWinner);
                var winRate = gamesPlayed > 0 ? (decimal)wins * 100 / gamesPlayed : 0;

                var membership = g.First().LeagueMembership;
                return new PlayerTownStatsDto
                {
                    LeagueMembershipId = g.Key,
                    PlayerNickname = membership?.PlayerNickname ?? "Unknown",
                    PlayerDisplayName = membership?.PlayerDisplayName,
                    GamesPlayed = gamesPlayed,
                    Wins = wins,
                    WinRate = winRate,
                    IsUnregistered = membership?.UserId == null
                };
            })
            .ToList();

        var topPlayers = playerStats
            .OrderByDescending(p => p.GamesPlayed)
            .ThenByDescending(p => p.WinRate)
            .Take(3)
            .ToList();

        var bestPlayers = playerStats
            .OrderByDescending(p => p.WinRate)
            .ThenByDescending(p => p.GamesPlayed)
            .Take(3)
            .ToList();

        return (topPlayers, bestPlayers);
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

    public async Task<List<ColorStatisticsDto>> GetLeagueColorStatisticsAsync(
        Guid leagueId,
        int daysWindow = 365,
        int minGamesPerSize = 10)
    {
        var cacheKey = $"league_color_stats_{leagueId}_{daysWindow}_{minGamesPerSize}";

        if (_memoryCache.TryGetValue<List<ColorStatisticsDto>>(cacheKey, out var cachedStats))
        {
            return cachedStats!;
        }

        _logger.LogInformation(
            "Calculating colour statistics for league {LeagueId} with {Days} days window and min {MinGames} games per size",
            leagueId, daysWindow, minGamesPerSize);

        var cutoffDate = DateTime.UtcNow.AddDays(-daysWindow);

        // Get completed games with participants within time window for this league
        var gamesWithParticipants = await _dbContext.Games
            .AsNoTracking()
            .Include(g => g.Participants)
            .Where(g => g.LeagueId == leagueId
                && g.Status == GameStatus.Completed
                && g.StartTime >= cutoffDate)
            .ToListAsync();

        if (!gamesWithParticipants.Any())
        {
            _logger.LogInformation("No completed games found for league {LeagueId} in time window", leagueId);
            return new List<ColorStatisticsDto>();
        }

        // Group games by participant count (game size)
        var gamesBySize = gamesWithParticipants
            .GroupBy(g => g.Participants.Count)
            .Where(g => g.Sum(game => game.Participants.Count) >= minGamesPerSize) // Minimum total games across all colors for this size
            .OrderBy(g => g.Key)
            .ToList();

        var colorStats = new List<ColorStatisticsDto>();

        foreach (var sizeGroup in gamesBySize)
        {
            var gameSize = sizeGroup.Key;
            var games = sizeGroup.ToList();

            // Count total games for this size (each game contributes to the total)
            var totalGameInstances = games.Count;

            // Since colors are deterministic (Red=0, Blue=1, etc.) and we want to analyze per color,
            // we need to group participants by their position/color assignment for this game size
            var colorPerformances = new List<ColorPerformanceDto>();

            // For each possible color (0-7, corresponding to enum values)
            for (int colorIndex = 0; colorIndex < 8; colorIndex++)
            {
                var color = (PlayerColor)colorIndex;
                var colorName = color.ToString();

                // Find all participants who played this color in this game size
                var colorParticipants = new List<Data.Entities.GameParticipant>();
                foreach (var game in games)
                {
                    var participantWithColor = game.Participants.FirstOrDefault(p => (int)p.PlayerColor == colorIndex);
                    if (participantWithColor != null)
                    {
                        colorParticipants.Add(participantWithColor);
                    }
                }

                if (!colorParticipants.Any())
                {
                    continue; // This color wasn't used in any games of this size
                }

                var totalGames = colorParticipants.Count;
                var wins = colorParticipants.Count(p => p.IsWinner);
                var winRate = totalGames > 0 ? (decimal)wins * 100 / totalGames : 0;

                colorPerformances.Add(new ColorPerformanceDto
                {
                    Color = color,
                    ColorName = colorName,
                    Wins = wins,
                    WinRate = winRate,
                    TotalGames = totalGames
                });
            }

            // Sort colors by enum order (Red=0, Blue=1, etc.)
            colorPerformances = colorPerformances
                .OrderBy(c => (int)c.Color)
                .ToList();

            colorStats.Add(new ColorStatisticsDto
            {
                GameSize = gameSize,
                ColorPerformances = colorPerformances
            });
        }

        // Sort by game size ascending
        var result = colorStats.OrderBy(c => c.GameSize).ToList();

        // Cache for 1 hour
        var cacheOptions = new MemoryCacheEntryOptions()
            .SetSlidingExpiration(TimeSpan.FromHours(1));
        _memoryCache.Set(cacheKey, result, cacheOptions);

        _logger.LogInformation(
            "Calculated colour statistics for {Count} game sizes in league {LeagueId}",
            result.Count, leagueId);

        return result;
    }

    /// <summary>
    /// Get display name for a player color
    /// </summary>
    private string GetColorDisplayName(PlayerColor color)
    {
        return color switch
        {
            PlayerColor.Red => "Red",
            PlayerColor.Blue => "Blue",
            PlayerColor.Tan => "Tan",
            PlayerColor.Green => "Green",
            PlayerColor.Orange => "Orange",
            PlayerColor.Purple => "Purple",
            PlayerColor.Teal => "Teal",
            PlayerColor.Pink => "Pink",
            _ => color.ToString()
        };
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
