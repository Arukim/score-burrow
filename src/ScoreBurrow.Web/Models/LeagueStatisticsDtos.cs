namespace ScoreBurrow.Web.Models;

/// <summary>
/// Town statistics for a league over a specified time window
/// </summary>
public class TownStatisticsDto
{
    public int TownId { get; set; }
    public string TownName { get; set; } = string.Empty;
    public int GamesPlayed { get; set; }
    public int Wins { get; set; }
    public decimal WinRate { get; set; }
    public int AvgGoldTrade { get; set; }      // Rounded to /100
    public int MedianGoldTrade { get; set; }   // Rounded to /500
    public TownValueCategory? ValueCategory { get; set; } // null if < 10 games
    public List<HeroStatisticsDto> TopWinningHeroes { get; set; } = new();
    public List<HeroStatisticsDto> TopLosingHeroes { get; set; } = new();
    public List<PlayerTownStatsDto> TopPlayers { get; set; } = new();       // Top players by pick count
    public List<PlayerTownStatsDto> BestPlayers { get; set; } = new();      // Top players by performance
}

/// <summary>
/// Hero statistics within a town
/// </summary>
public class HeroStatisticsDto
{
    public int HeroId { get; set; }
    public string HeroName { get; set; } = string.Empty;
    public int GamesPlayed { get; set; }
    public int Wins { get; set; }
    public decimal WinRate { get; set; }
    public decimal PickRate { get; set; }  // % of times picked when town is played
}

/// <summary>
/// Value category for towns based on cost vs performance
/// </summary>
public enum TownValueCategory
{
    Overhyped,     // High cost, low win rate
    Undervalued    // Low cost, high win rate
}

/// <summary>
/// Player-specific performance statistics
/// </summary>
public class PlayerPerformanceDto
{
    public Guid LeagueMembershipId { get; set; }
    public List<TownPerformanceDto> BestTowns { get; set; } = new();
    public List<TownPerformanceDto> WorstTowns { get; set; } = new();
    public List<HeroPerformanceDto> BestHeroes { get; set; } = new();
    public List<HeroPerformanceDto> WorstHeroes { get; set; } = new();
    public List<TownPerformanceDto> FavoriteTowns { get; set; } = new();    // Top towns by pick count
    public List<TownPerformanceDto> BestWeightedTowns { get; set; } = new(); // Top towns by weighted win rate
}

/// <summary>
/// Town performance for a specific player
/// </summary>
public class TownPerformanceDto
{
    public int TownId { get; set; }
    public string TownName { get; set; } = string.Empty;
    public int GamesPlayed { get; set; }
    public int Wins { get; set; }
    public decimal WinRate { get; set; }
}

/// <summary>
/// Hero performance for a specific player
/// </summary>
public class HeroPerformanceDto
{
    public int HeroId { get; set; }
    public string HeroName { get; set; } = string.Empty;
    public int GamesPlayed { get; set; }
    public int Wins { get; set; }
    public decimal WinRate { get; set; }
}

/// <summary>
/// Player statistics for a specific town (used in town expand details)
/// </summary>
public class PlayerTownStatsDto
{
    public Guid LeagueMembershipId { get; set; }
    public string PlayerNickname { get; set; } = string.Empty;
    public string? PlayerDisplayName { get; set; }
    public int GamesPlayed { get; set; }
    public int Wins { get; set; }
    public decimal WinRate { get; set; }
    public bool IsUnregistered { get; set; }
}
