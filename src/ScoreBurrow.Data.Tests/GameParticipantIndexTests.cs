using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using ScoreBurrow.Data.Entities;

namespace ScoreBurrow.Data.Tests;

public class GameParticipantIndexTests
{
    [Fact]
    public void GameParticipant_HasUniqueIndexOnGameAndMembership()
    {
        var options = new DbContextOptionsBuilder<ScoreBurrowDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        using var context = new ScoreBurrowDbContext(options);
        var entity = context.Model.FindEntityType(typeof(GameParticipant));
        entity.Should().NotBeNull();

        var uniqueIndexes = entity!.GetIndexes()
            .Where(i => i.IsUnique)
            .Select(i => i.Properties.Select(p => p.Name).ToArray())
            .ToList();

        uniqueIndexes.Should().ContainEquivalentOf(new[] { "GameId", "PlayerColor" });
        uniqueIndexes.Should().ContainEquivalentOf(new[] { "GameId", "LeagueMembershipId" });
    }
}
