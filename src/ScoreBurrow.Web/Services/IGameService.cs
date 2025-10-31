using ScoreBurrow.Web.Models;

namespace ScoreBurrow.Web.Services;

public interface IGameService
{
    /// <summary>
    /// Creates a new game with participants. Requires admin or owner role.
    /// </summary>
    Task<Guid> CreateGameAsync(Guid leagueId, string userId, CreateGameRequest request);

    /// <summary>
    /// Completes a game by selecting a winner and calculating ratings. Requires admin or owner role.
    /// </summary>
    Task<bool> CompleteGameAsync(Guid gameId, string userId, Guid winnerId);

    /// <summary>
    /// Applies technical loss penalty to culprit and creates new game with same settings. Requires admin or owner role.
    /// </summary>
    Task<Guid?> ApplyTechnicalLossAsync(Guid gameId, string userId, Guid culpritMembershipId);

    /// <summary>
    /// Cancels a game without affecting ratings. Requires admin or owner role.
    /// </summary>
    Task<bool> CancelGameAsync(Guid gameId, string userId);

    /// <summary>
    /// Gets game details for management. Returns null if user lacks permission or game not found.
    /// </summary>
    Task<GameDetailsDto?> GetGameForManagementAsync(Guid gameId, string userId);
}
