using Microsoft.EntityFrameworkCore;
using ScoreBurrow.Data.Entities;
using ScoreBurrow.Data.Enums;

namespace ScoreBurrow.Data.Statistics;

public class PlayerStatisticsProjector
{
    private readonly ScoreBurrowDbContext _context;

    public PlayerStatisticsProjector(ScoreBurrowDbContext context)
    {
        _context = context;
    }

    public async Task<int> RecalculateLeagueAsync(Guid leagueId, CancellationToken cancellationToken = default)
    {
        var membershipIds = await _context.LeagueMemberships
            .Where(m => m.LeagueId == leagueId)
            .Select(m => m.Id)
            .ToListAsync(cancellationToken);

        return await RecalculateMembershipsAsync(leagueId, membershipIds, cancellationToken);
    }

    public async Task<int> RecalculateMembershipsAsync(
        Guid leagueId,
        IReadOnlyCollection<Guid> membershipIds,
        CancellationToken cancellationToken = default)
    {
        if (membershipIds.Count == 0)
        {
            return 0;
        }

        var participations = await _context.GameParticipants
            .Where(gp => gp.Game.LeagueId == leagueId
                && gp.Game.Status == GameStatus.Completed
                && membershipIds.Contains(gp.LeagueMembershipId))
            .Select(gp => new
            {
                gp.LeagueMembershipId,
                Participation = new PlayerParticipation(
                    gp.TownId,
                    gp.HeroId,
                    gp.Position,
                    gp.IsWinner,
                    gp.IsTechnicalLoss)
            })
            .ToListAsync(cancellationToken);

        var participationsByMembership = participations
            .GroupBy(p => p.LeagueMembershipId)
            .ToDictionary(g => g.Key, g => g.Select(x => x.Participation).ToList());

        var existingStats = await _context.PlayerStatistics
            .Where(s => membershipIds.Contains(s.LeagueMembershipId))
            .ToListAsync(cancellationToken);

        var statsByMembership = existingStats.ToDictionary(s => s.LeagueMembershipId);
        var lastUpdated = DateTime.UtcNow;
        var updatedCount = 0;

        foreach (var membershipId in membershipIds.Distinct())
        {
            if (!participationsByMembership.TryGetValue(membershipId, out var memberParticipations)
                || memberParticipations.Count == 0)
            {
                if (statsByMembership.TryGetValue(membershipId, out var staleStats))
                {
                    _context.PlayerStatistics.Remove(staleStats);
                }

                continue;
            }

            var projection = PlayerStatisticsCalculator.FromParticipations(memberParticipations);

            if (!statsByMembership.TryGetValue(membershipId, out var stats))
            {
                stats = new PlayerStatistics
                {
                    Id = Guid.NewGuid(),
                    LeagueMembershipId = membershipId
                };
                _context.PlayerStatistics.Add(stats);
            }

            PlayerStatisticsCalculator.Apply(stats, projection, lastUpdated);
            updatedCount++;
        }

        return updatedCount;
    }
}
