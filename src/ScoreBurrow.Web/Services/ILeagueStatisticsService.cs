using ScoreBurrow.Web.Models;

namespace ScoreBurrow.Web.Services;

/// <summary>
/// Service for calculating league-wide and player-specific town/hero statistics
/// </summary>
public interface ILeagueStatisticsService
{
    /// <summary>
    /// Get league-wide town statistics over a time window
    /// </summary>
    /// <param name="leagueId">League ID</param>
    /// <param name="daysWindow">Number of days to look back (default 365)</param>
    /// <returns>List of town statistics ordered by games played descending</returns>
    Task<List<TownStatisticsDto>> GetLeagueTownStatisticsAsync(
        Guid leagueId, 
        int daysWindow = 365);

    /// <summary>
    /// Get player-specific performance statistics
    /// </summary>
    /// <param name="leagueMembershipId">League membership ID</param>
    /// <param name="minGames">Minimum games required to be included (default 3)</param>
    /// <param name="daysWindow">Number of days to look back (default 365)</param>
    /// <returns>Player performance data with best/worst towns and heroes</returns>
    Task<PlayerPerformanceDto> GetPlayerPerformanceAsync(
        Guid leagueMembershipId,
        int minGames = 3,
        int daysWindow = 365);

    /// <summary>
    /// Get player's best performing town for display on league page
    /// </summary>
    /// <param name="leagueMembershipId">League membership ID</param>
    /// <param name="minGames">Minimum games required (default 3)</param>
    /// <param name="daysWindow">Number of days to look back (default 365)</param>
    /// <returns>Best town or null if no qualifying data</returns>
    Task<TownPerformanceDto?> GetPlayerBestTownAsync(
        Guid leagueMembershipId,
        int minGames = 3,
        int daysWindow = 365);
}
