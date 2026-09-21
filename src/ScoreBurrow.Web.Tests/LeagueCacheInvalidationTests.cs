using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using ScoreBurrow.Data;
using ScoreBurrow.Data.Statistics;
using ScoreBurrow.Web.Services;

namespace ScoreBurrow.Web.Tests;

public class LeagueCacheInvalidationTests
{
    [Fact]
    public void InvalidateLeagueCache_DropsTokenBackedPlayerPerformanceEntry()
    {
        using var cache = new MemoryCache(new MemoryCacheOptions());
        using var context = CreateContext();
        var leagues = CreateLeagueService(context, cache);
        var leagueId = Guid.NewGuid();
        var options = LeagueCacheEntries.ForLeague(leagues, leagueId, TimeSpan.FromHours(1));

        cache.Set("player_performance_abc_3_365", "stale", options);
        cache.TryGetValue("player_performance_abc_3_365", out string? _).Should().BeTrue();

        leagues.InvalidateLeagueCache(leagueId);

        cache.TryGetValue("player_performance_abc_3_365", out string? _).Should().BeFalse();
    }

    [Fact]
    public void InvalidateLeagueCache_DoesNotDropAnotherLeaguesEntries()
    {
        using var cache = new MemoryCache(new MemoryCacheOptions());
        using var context = CreateContext();
        var leagues = CreateLeagueService(context, cache);
        var leagueA = Guid.NewGuid();
        var leagueB = Guid.NewGuid();

        cache.Set("stats-a", "keep-until-a", LeagueCacheEntries.ForLeague(leagues, leagueA, TimeSpan.FromHours(1)));
        cache.Set("stats-b", "keep-until-b", LeagueCacheEntries.ForLeague(leagues, leagueB, TimeSpan.FromHours(1)));

        leagues.InvalidateLeagueCache(leagueA);

        cache.TryGetValue("stats-a", out string? _).Should().BeFalse();
        cache.TryGetValue("stats-b", out string? value);
        value.Should().Be("keep-until-b");
    }

    private static ScoreBurrowDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ScoreBurrowDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new ScoreBurrowDbContext(options);
    }

    private static LeagueService CreateLeagueService(ScoreBurrowDbContext context, IMemoryCache cache) =>
        new(
            context,
            userManager: null!,
            cache,
            NullLogger<LeagueService>.Instance,
            new PlayerStatisticsProjector(context));
}
