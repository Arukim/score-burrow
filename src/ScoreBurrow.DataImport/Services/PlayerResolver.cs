using Microsoft.EntityFrameworkCore;
using ScoreBurrow.Data;
using ScoreBurrow.Data.Entities;
using ScoreBurrow.Data.Enums;

namespace ScoreBurrow.DataImport.Services;

public class PlayerResolver
{
    private readonly ScoreBurrowDbContext _dbContext;
    private readonly Dictionary<string, LeagueMembership> _playerCache = new(StringComparer.OrdinalIgnoreCase);

    public PlayerResolver(ScoreBurrowDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Dictionary<string, LeagueMembership>> ResolvePlayersAsync(
        IEnumerable<string> playerNames, 
        Guid leagueId)
    {
        var uniquePlayers = playerNames.Distinct(StringComparer.OrdinalIgnoreCase).ToList();

        // Load existing memberships for this league
        var existingMemberships = await _dbContext.LeagueMemberships
            .Where(m => m.LeagueId == leagueId)
            .ToListAsync();

        foreach (var playerName in uniquePlayers)
        {
            var existing = existingMemberships.FirstOrDefault(
                m => m.PlayerNickname.Equals(playerName, StringComparison.OrdinalIgnoreCase));

            if (existing != null)
            {
                _playerCache[playerName] = existing;
            }
            else
            {
                // Create new membership for unregistered player
                var newMembership = new LeagueMembership
                {
                    Id = Guid.NewGuid(),
                    LeagueId = leagueId,
                    UserId = null, // Unregistered player
                    PlayerNickname = playerName,
                    PlayerDisplayName = playerName,
                    Role = LeagueRole.Member,
                    Glicko2Rating = 1500,
                    Glicko2RatingDeviation = 350,
                    Glicko2Volatility = 0.06
                };

                _dbContext.LeagueMemberships.Add(newMembership);
                _playerCache[playerName] = newMembership;
                
                Console.WriteLine($"Created membership for unregistered player: {playerName}");
            }
        }

        return _playerCache;
    }

    public LeagueMembership GetMembership(string playerName)
    {
        if (_playerCache.TryGetValue(playerName, out var membership))
        {
            return membership;
        }

        throw new InvalidOperationException($"Player membership not found: {playerName}");
    }
}
