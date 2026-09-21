using ScoreBurrow.Data.Entities;
using ScoreBurrow.Web.Models;

namespace ScoreBurrow.Web.Services;

public static class CreateGameValidator
{
    public const int MinPlayers = 2;
    public const int MaxPlayers = 8;

    public static void Validate(
        IReadOnlyList<ParticipantRequest> participants,
        IReadOnlyDictionary<Guid, LeagueMembership> membershipsInLeague,
        IReadOnlySet<int> existingTownIds,
        IReadOnlyDictionary<int, Hero> heroesById,
        IReadOnlyList<int>? townPoolTownIds = null)
    {
        ArgumentNullException.ThrowIfNull(participants);
        ArgumentNullException.ThrowIfNull(membershipsInLeague);
        ArgumentNullException.ThrowIfNull(existingTownIds);
        ArgumentNullException.ThrowIfNull(heroesById);

        if (participants.Count < MinPlayers || participants.Count > MaxPlayers)
        {
            throw new ArgumentException($"A game must have between {MinPlayers} and {MaxPlayers} players.");
        }

        if (participants.Select(p => p.LeagueMembershipId).Distinct().Count() != participants.Count)
        {
            throw new ArgumentException("Each player can only appear once in a game.");
        }

        if (participants.Select(p => p.PlayerColor).Distinct().Count() != participants.Count)
        {
            throw new ArgumentException("Each player color can only be used once in a game.");
        }

        foreach (var participant in participants)
        {
            if (!membershipsInLeague.ContainsKey(participant.LeagueMembershipId))
            {
                throw new ArgumentException($"League membership {participant.LeagueMembershipId} is not a member of this league.");
            }

            if (!existingTownIds.Contains(participant.TownId))
            {
                throw new ArgumentException($"Town {participant.TownId} was not found.");
            }

            var heroId = NormalizeHeroId(participant.HeroId);
            if (heroId is null)
            {
                continue;
            }

            if (!heroesById.TryGetValue(heroId.Value, out var hero))
            {
                throw new ArgumentException($"Hero {heroId.Value} was not found.");
            }

            if (hero.TownId != participant.TownId)
            {
                throw new ArgumentException($"Hero {hero.Name} does not belong to the selected town.");
            }
        }

        if (townPoolTownIds is not { Count: > 0 })
        {
            return;
        }

        var pool = new List<int>();
        var seen = new HashSet<int>();
        foreach (var townId in townPoolTownIds)
        {
            if (townId <= 0 || !seen.Add(townId))
            {
                throw new ArgumentException("The town pool cannot contain duplicate or empty towns.");
            }

            if (!existingTownIds.Contains(townId))
            {
                throw new ArgumentException($"Town {townId} was not found.");
            }

            pool.Add(townId);
        }

        if (pool.Count != participants.Count)
        {
            throw new ArgumentException("The town pool must contain one town per player.");
        }

        foreach (var participant in participants)
        {
            if (!seen.Contains(participant.TownId))
            {
                throw new ArgumentException($"Town {participant.TownId} is not in the town pool.");
            }
        }
    }

    public static int? NormalizeHeroId(int? heroId) => heroId is > 0 ? heroId : null;
}
