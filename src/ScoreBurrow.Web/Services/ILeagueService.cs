using ScoreBurrow.Data.Enums;

namespace ScoreBurrow.Web.Services;

public interface ILeagueService
{
    /// <summary>
    /// Creates a new league with the specified user as owner
    /// </summary>
    Task<Guid> CreateLeagueAsync(string userId, string name, string? description);

    /// <summary>
    /// Updates league name and description. Requires owner role.
    /// </summary>
    Task<bool> UpdateLeagueAsync(Guid leagueId, string userId, string name, string? description);

    /// <summary>
    /// Archives a league (sets IsActive = false). Requires owner role.
    /// </summary>
    Task<bool> ArchiveLeagueAsync(Guid leagueId, string userId);

    /// <summary>
    /// Unarchives a league (sets IsActive = true). Requires owner role.
    /// </summary>
    Task<bool> UnarchiveLeagueAsync(Guid leagueId, string userId);

    /// <summary>
    /// Permanently deletes a league and all related data. Requires owner role.
    /// </summary>
    Task<bool> DeleteLeagueAsync(Guid leagueId, string userId);

    /// <summary>
    /// Adds a registered user as a member of the league. Requires owner or admin role.
    /// </summary>
    Task<bool> AddMemberAsync(Guid leagueId, string userId, string memberEmail, string? displayName = null);

    /// <summary>
    /// Links an existing unregistered membership to a registered user. Requires owner or admin role.
    /// </summary>
    Task<bool> LinkMemberAsync(Guid leagueId, string userId, Guid membershipId, string memberEmail);

    /// <summary>
    /// Updates the user binding for an existing membership. Requires owner or admin role.
    /// </summary>
    Task<bool> UpdateMemberUserAsync(Guid leagueId, string userId, Guid membershipId, string newEmail);

    /// <summary>
    /// Updates a member's role. Requires owner or admin role.
    /// </summary>
    Task<bool> UpdateMemberRoleAsync(Guid leagueId, string userId, Guid membershipId, LeagueRole newRole);

    /// <summary>
    /// Removes a member from the league. Requires owner or admin role. Cannot remove owner.
    /// </summary>
    Task<bool> RemoveMemberAsync(Guid leagueId, string userId, Guid membershipId);

    /// <summary>
    /// Checks if user is the owner of the league
    /// </summary>
    Task<bool> IsOwnerAsync(string userId, Guid leagueId);

    /// <summary>
    /// Checks if user is an admin or owner of the league
    /// </summary>
    Task<bool> IsAdminOrOwnerAsync(string userId, Guid leagueId);

    /// <summary>
    /// Checks if user is a member (any role) of the league
    /// </summary>
    Task<bool> IsMemberAsync(string userId, Guid leagueId);
}
