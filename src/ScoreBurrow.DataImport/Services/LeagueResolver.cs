using Microsoft.EntityFrameworkCore;
using ScoreBurrow.Data;
using ScoreBurrow.Data.Entities;
using ScoreBurrow.DataImport.Models;

namespace ScoreBurrow.DataImport.Services;

public class LeagueResolver
{
    private readonly ScoreBurrowDbContext _dbContext;

    public LeagueResolver(ScoreBurrowDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<League> ResolveLeagueAsync(ImportOptions options)
    {
        if (options.LeagueId.HasValue)
        {
            return await GetExistingLeagueByIdAsync(options.LeagueId.Value);
        }
        else if (!string.IsNullOrWhiteSpace(options.LeagueName))
        {
            if (!options.OwnerId.HasValue)
            {
                throw new InvalidOperationException("Owner ID is required when creating a new league");
            }

            return await GetOrCreateLeagueByNameAsync(options.LeagueName, options.OwnerId.Value);
        }
        else
        {
            throw new InvalidOperationException("Either LeagueId or LeagueName must be provided");
        }
    }

    private async Task<League> GetExistingLeagueByIdAsync(Guid leagueId)
    {
        var league = await _dbContext.Leagues
            .FirstOrDefaultAsync(l => l.Id == leagueId);

        if (league == null)
        {
            throw new InvalidOperationException($"League with ID {leagueId} not found");
        }

        return league;
    }

    private async Task<League> GetOrCreateLeagueByNameAsync(string leagueName, Guid ownerId)
    {
        // Check if owner exists
        var owner = await _dbContext.Users.FindAsync(ownerId.ToString());
        if (owner == null)
        {
            throw new InvalidOperationException($"User with ID {ownerId} not found");
        }

        // Check if league with this name already exists
        var existingLeague = await _dbContext.Leagues
            .FirstOrDefaultAsync(l => l.Name == leagueName);

        if (existingLeague != null)
        {
            Console.WriteLine($"Using existing league: {existingLeague.Name} ({existingLeague.Id})");
            return existingLeague;
        }

        // Create new league
        var newLeague = new League
        {
            Id = Guid.NewGuid(),
            Name = leagueName,
            Description = "Imported historical game data from CSV",
            OwnerId = ownerId.ToString(),
            IsActive = true
        };

        _dbContext.Leagues.Add(newLeague);
        Console.WriteLine($"Created new league: {newLeague.Name} ({newLeague.Id})");

        return newLeague;
    }
}
