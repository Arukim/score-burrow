using Microsoft.Extensions.Caching.Memory;

namespace ScoreBurrow.Web.Services;

public static class LeagueCacheEntries
{
    public static MemoryCacheEntryOptions ForLeague(ILeagueService leagueService, Guid leagueId, TimeSpan absoluteExpiration)
    {
        return new MemoryCacheEntryOptions()
            .SetAbsoluteExpiration(absoluteExpiration)
            .AddExpirationToken(leagueService.GetLeagueCacheExpirationToken(leagueId));
    }

    public static MemoryCacheEntryOptions Absolute(TimeSpan absoluteExpiration)
    {
        return new MemoryCacheEntryOptions()
            .SetAbsoluteExpiration(absoluteExpiration);
    }
}
