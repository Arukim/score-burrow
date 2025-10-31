using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using ScoreBurrow.Data;
using ScoreBurrow.Data.Entities;
using ScoreBurrow.Data.Enums;

namespace ScoreBurrow.Web.Services;

public class LeagueService : ILeagueService
{
    private readonly ScoreBurrowDbContext _dbContext;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IMemoryCache _memoryCache;
    private readonly ILogger<LeagueService> _logger;

    public LeagueService(
        ScoreBurrowDbContext dbContext,
        UserManager<ApplicationUser> userManager,
        IMemoryCache memoryCache,
        ILogger<LeagueService> logger)
    {
        _dbContext = dbContext;
        _userManager = userManager;
        _memoryCache = memoryCache;
        _logger = logger;
    }

    public async Task<Guid> CreateLeagueAsync(string userId, string name, string? description)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
        {
            throw new InvalidOperationException("User not found");
        }

        var league = new League
        {
            Id = Guid.NewGuid(),
            Name = name,
            Description = description,
            OwnerId = userId,
            IsActive = true,
            CreatedDate = DateTime.UtcNow,
            CreatedBy = user.UserName ?? userId,
            CreatedOn = DateTime.UtcNow
        };

        _dbContext.Leagues.Add(league);

        // Create owner membership
        var membership = new LeagueMembership
        {
            Id = Guid.NewGuid(),
            LeagueId = league.Id,
            UserId = userId,
            PlayerNickname = user.UserName ?? user.Email ?? "Unknown",
            Role = LeagueRole.Owner,
            JoinedDate = DateTime.UtcNow,
            CreatedBy = user.UserName ?? userId,
            CreatedOn = DateTime.UtcNow
        };

        _dbContext.LeagueMemberships.Add(membership);

        await _dbContext.SaveChangesAsync();

        InvalidateLeagueCache(league.Id);

        _logger.LogInformation("League {LeagueId} created by user {UserId}", league.Id, userId);

        return league.Id;
    }

    public async Task<bool> UpdateLeagueAsync(Guid leagueId, string userId, string name, string? description)
    {
        if (!await IsOwnerAsync(userId, leagueId))
        {
            _logger.LogWarning("User {UserId} attempted to update league {LeagueId} without owner permission", userId, leagueId);
            return false;
        }

        var league = await _dbContext.Leagues.FindAsync(leagueId);
        if (league == null)
        {
            return false;
        }

        var user = await _userManager.FindByIdAsync(userId);
        league.Name = name;
        league.Description = description;
        league.ModifiedBy = user?.UserName ?? userId;
        league.ModifiedOn = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync();

        InvalidateLeagueCache(leagueId);

        _logger.LogInformation("League {LeagueId} updated by user {UserId}", leagueId, userId);

        return true;
    }

    public async Task<bool> ArchiveLeagueAsync(Guid leagueId, string userId)
    {
        if (!await IsOwnerAsync(userId, leagueId))
        {
            _logger.LogWarning("User {UserId} attempted to archive league {LeagueId} without owner permission", userId, leagueId);
            return false;
        }

        var league = await _dbContext.Leagues.FindAsync(leagueId);
        if (league == null)
        {
            return false;
        }

        var user = await _userManager.FindByIdAsync(userId);
        league.IsActive = false;
        league.ModifiedBy = user?.UserName ?? userId;
        league.ModifiedOn = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync();

        InvalidateLeagueCache(leagueId);

        _logger.LogInformation("League {LeagueId} archived by user {UserId}", leagueId, userId);

        return true;
    }

    public async Task<bool> UnarchiveLeagueAsync(Guid leagueId, string userId)
    {
        if (!await IsOwnerAsync(userId, leagueId))
        {
            _logger.LogWarning("User {UserId} attempted to unarchive league {LeagueId} without owner permission", userId, leagueId);
            return false;
        }

        var league = await _dbContext.Leagues.FindAsync(leagueId);
        if (league == null)
        {
            return false;
        }

        var user = await _userManager.FindByIdAsync(userId);
        league.IsActive = true;
        league.ModifiedBy = user?.UserName ?? userId;
        league.ModifiedOn = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync();

        InvalidateLeagueCache(leagueId);

        _logger.LogInformation("League {LeagueId} unarchived by user {UserId}", leagueId, userId);

        return true;
    }

    public async Task<bool> DeleteLeagueAsync(Guid leagueId, string userId)
    {
        if (!await IsOwnerAsync(userId, leagueId))
        {
            _logger.LogWarning("User {UserId} attempted to delete league {LeagueId} without owner permission", userId, leagueId);
            return false;
        }

        var league = await _dbContext.Leagues.FindAsync(leagueId);
        if (league == null)
        {
            return false;
        }

        // Get all games in this league
        var games = await _dbContext.Games
            .Where(g => g.LeagueId == leagueId)
            .ToListAsync();

        var gameIds = games.Select(g => g.Id).ToList();

        // Delete GameParticipants for all games
        var gameParticipants = await _dbContext.GameParticipants
            .Where(gp => gameIds.Contains(gp.GameId))
            .ToListAsync();
        _dbContext.GameParticipants.RemoveRange(gameParticipants);

        // Delete Games
        _dbContext.Games.RemoveRange(games);

        // Get all memberships
        var memberships = await _dbContext.LeagueMemberships
            .Where(m => m.LeagueId == leagueId)
            .ToListAsync();

        var membershipIds = memberships.Select(m => m.Id).ToList();

        // Delete PlayerStatistics
        var statistics = await _dbContext.PlayerStatistics
            .Where(s => membershipIds.Contains(s.LeagueMembershipId))
            .ToListAsync();
        _dbContext.PlayerStatistics.RemoveRange(statistics);

        // Delete RatingHistory
        var ratingHistory = await _dbContext.RatingHistory
            .Where(r => membershipIds.Contains(r.LeagueMembershipId))
            .ToListAsync();
        _dbContext.RatingHistory.RemoveRange(ratingHistory);

        // Delete LeagueMemberships
        _dbContext.LeagueMemberships.RemoveRange(memberships);

        // Delete League
        _dbContext.Leagues.Remove(league);

        await _dbContext.SaveChangesAsync();

        InvalidateLeagueCache(leagueId);

        _logger.LogInformation("League {LeagueId} permanently deleted by user {UserId}", leagueId, userId);

        return true;
    }

    public async Task<bool> AddMemberAsync(Guid leagueId, string userId, string memberEmail, string? displayName = null)
    {
        if (!await IsAdminOrOwnerAsync(userId, leagueId))
        {
            _logger.LogWarning("User {UserId} attempted to add member to league {LeagueId} without admin permission", userId, leagueId);
            return false;
        }

        var league = await _dbContext.Leagues.FindAsync(leagueId);
        if (league == null)
        {
            return false;
        }

        var memberUser = await _userManager.FindByEmailAsync(memberEmail);
        if (memberUser == null)
        {
            _logger.LogWarning("User with email {Email} not found", memberEmail);
            return false;
        }

        // Check if already a member
        var existingMembership = await _dbContext.LeagueMemberships
            .FirstOrDefaultAsync(m => m.LeagueId == leagueId && m.UserId == memberUser.Id);

        if (existingMembership != null)
        {
            _logger.LogWarning("User {UserId} is already a member of league {LeagueId}", memberUser.Id, leagueId);
            return false;
        }

        var user = await _userManager.FindByIdAsync(userId);
        var membership = new LeagueMembership
        {
            Id = Guid.NewGuid(),
            LeagueId = leagueId,
            UserId = memberUser.Id,
            PlayerNickname = memberUser.UserName ?? memberUser.Email ?? "Unknown",
            PlayerDisplayName = displayName,
            Role = LeagueRole.Member,
            JoinedDate = DateTime.UtcNow,
            CreatedBy = user?.UserName ?? userId,
            CreatedOn = DateTime.UtcNow
        };

        _dbContext.LeagueMemberships.Add(membership);
        await _dbContext.SaveChangesAsync();

        InvalidateLeagueCache(leagueId);

        _logger.LogInformation("User {MemberId} added to league {LeagueId} by {UserId}", memberUser.Id, leagueId, userId);

        return true;
    }

    public async Task<bool> LinkMemberAsync(Guid leagueId, string userId, Guid membershipId, string memberEmail)
    {
        if (!await IsAdminOrOwnerAsync(userId, leagueId))
        {
            _logger.LogWarning("User {UserId} attempted to link member in league {LeagueId} without admin permission", userId, leagueId);
            return false;
        }

        var membership = await _dbContext.LeagueMemberships
            .FirstOrDefaultAsync(m => m.Id == membershipId && m.LeagueId == leagueId);

        if (membership == null)
        {
            _logger.LogWarning("Membership {MembershipId} not found in league {LeagueId}", membershipId, leagueId);
            return false;
        }

        // Can only link unregistered members
        if (!string.IsNullOrEmpty(membership.UserId))
        {
            _logger.LogWarning("Membership {MembershipId} is already linked to a user", membershipId);
            return false;
        }

        var memberUser = await _userManager.FindByEmailAsync(memberEmail);
        if (memberUser == null)
        {
            _logger.LogWarning("User with email {Email} not found", memberEmail);
            return false;
        }

        // Check if this user is already a member
        var existingMembership = await _dbContext.LeagueMemberships
            .FirstOrDefaultAsync(m => m.LeagueId == leagueId && m.UserId == memberUser.Id);

        if (existingMembership != null)
        {
            _logger.LogWarning("User {UserId} is already a member of league {LeagueId}", memberUser.Id, leagueId);
            return false;
        }

        var user = await _userManager.FindByIdAsync(userId);
        membership.UserId = memberUser.Id;
        membership.ModifiedBy = user?.UserName ?? userId;
        membership.ModifiedOn = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync();

        InvalidateLeagueCache(leagueId);

        _logger.LogInformation("Membership {MembershipId} linked to user {UserId} in league {LeagueId}", membershipId, memberUser.Id, leagueId);

        return true;
    }

    public async Task<bool> UpdateMemberUserAsync(Guid leagueId, string userId, Guid membershipId, string newEmail)
    {
        if (!await IsAdminOrOwnerAsync(userId, leagueId))
        {
            _logger.LogWarning("User {UserId} attempted to update member user binding in league {LeagueId} without admin permission", userId, leagueId);
            return false;
        }

        var membership = await _dbContext.LeagueMemberships
            .FirstOrDefaultAsync(m => m.Id == membershipId && m.LeagueId == leagueId);

        if (membership == null)
        {
            _logger.LogWarning("Membership {MembershipId} not found in league {LeagueId}", membershipId, leagueId);
            return false;
        }

        // Cannot change owner binding
        if (membership.Role == LeagueRole.Owner)
        {
            _logger.LogWarning("User {UserId} attempted to change owner user binding in league {LeagueId}", userId, leagueId);
            return false;
        }

        var newUser = await _userManager.FindByEmailAsync(newEmail);
        if (newUser == null)
        {
            _logger.LogWarning("User with email {Email} not found", newEmail);
            return false;
        }

        // Check if the new user is already a member (different membership)
        var existingMembership = await _dbContext.LeagueMemberships
            .FirstOrDefaultAsync(m => m.LeagueId == leagueId && m.UserId == newUser.Id && m.Id != membershipId);

        if (existingMembership != null)
        {
            _logger.LogWarning("User {UserId} is already a member of league {LeagueId}", newUser.Id, leagueId);
            return false;
        }

        var user = await _userManager.FindByIdAsync(userId);
        membership.UserId = newUser.Id;
        membership.ModifiedBy = user?.UserName ?? userId;
        membership.ModifiedOn = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync();

        InvalidateLeagueCache(leagueId);

        _logger.LogInformation("Membership {MembershipId} user binding updated to {UserId} in league {LeagueId}", membershipId, newUser.Id, leagueId);

        return true;
    }

    public async Task<bool> UpdateMemberRoleAsync(Guid leagueId, string userId, Guid membershipId, LeagueRole newRole)
    {
        if (!await IsAdminOrOwnerAsync(userId, leagueId))
        {
            _logger.LogWarning("User {UserId} attempted to update member role in league {LeagueId} without admin permission", userId, leagueId);
            return false;
        }

        var membership = await _dbContext.LeagueMemberships
            .FirstOrDefaultAsync(m => m.Id == membershipId && m.LeagueId == leagueId);

        if (membership == null)
        {
            return false;
        }

        // Cannot change owner role
        if (membership.Role == LeagueRole.Owner || newRole == LeagueRole.Owner)
        {
            _logger.LogWarning("User {UserId} attempted to change owner role in league {LeagueId}", userId, leagueId);
            return false;
        }

        var user = await _userManager.FindByIdAsync(userId);
        membership.Role = newRole;
        membership.ModifiedBy = user?.UserName ?? userId;
        membership.ModifiedOn = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync();

        InvalidateLeagueCache(leagueId);

        _logger.LogInformation("Membership {MembershipId} role updated to {Role} in league {LeagueId} by user {UserId}", membershipId, newRole, leagueId, userId);

        return true;
    }

    public async Task<bool> RemoveMemberAsync(Guid leagueId, string userId, Guid membershipId)
    {
        if (!await IsAdminOrOwnerAsync(userId, leagueId))
        {
            _logger.LogWarning("User {UserId} attempted to remove member from league {LeagueId} without admin permission", userId, leagueId);
            return false;
        }

        var membership = await _dbContext.LeagueMemberships
            .FirstOrDefaultAsync(m => m.Id == membershipId && m.LeagueId == leagueId);

        if (membership == null)
        {
            return false;
        }

        // Cannot remove owner
        if (membership.Role == LeagueRole.Owner)
        {
            _logger.LogWarning("User {UserId} attempted to remove owner from league {LeagueId}", userId, leagueId);
            return false;
        }

        _dbContext.LeagueMemberships.Remove(membership);
        await _dbContext.SaveChangesAsync();

        InvalidateLeagueCache(leagueId);

        _logger.LogInformation("Membership {MembershipId} removed from league {LeagueId} by user {UserId}", membershipId, leagueId, userId);

        return true;
    }

    public async Task<bool> IsOwnerAsync(string userId, Guid leagueId)
    {
        // Check both League.OwnerId and LeagueMembership role
        var league = await _dbContext.Leagues.FindAsync(leagueId);
        if (league?.OwnerId == userId)
        {
            return true;
        }

        return await _dbContext.LeagueMemberships
            .AnyAsync(m => m.LeagueId == leagueId && m.UserId == userId && m.Role == LeagueRole.Owner);
    }

    public async Task<bool> IsAdminOrOwnerAsync(string userId, Guid leagueId)
    {
        // Check League.OwnerId first
        var league = await _dbContext.Leagues.FindAsync(leagueId);
        if (league?.OwnerId == userId)
        {
            return true;
        }

        // Then check LeagueMembership role
        return await _dbContext.LeagueMemberships
            .AnyAsync(m => m.LeagueId == leagueId && m.UserId == userId && 
                (m.Role == LeagueRole.Owner || m.Role == LeagueRole.Admin));
    }

    public async Task<bool> IsMemberAsync(string userId, Guid leagueId)
    {
        return await _dbContext.LeagueMemberships
            .AnyAsync(m => m.LeagueId == leagueId && m.UserId == userId);
    }

    private void InvalidateLeagueCache(Guid leagueId)
    {
        // Clear league cache for all users by clearing all matching keys
        // Since we can't enumerate MemoryCache keys, we rely on cache expiration
        // For now, we just clear the most common patterns
        
        // Clear generic league cache
        _memoryCache.Remove($"league_{leagueId}");
        _memoryCache.Remove($"league_{leagueId}_anonymous");
        
        // Clear leagues list cache (all pages)
        for (int i = 1; i <= 100; i++)
        {
            _memoryCache.Remove($"leagues_page_{i}");
        }
        
        // Note: User-specific caches will eventually expire after 10 minutes
        // To force immediate refresh, users can reload the page after cache expiration
    }
}
