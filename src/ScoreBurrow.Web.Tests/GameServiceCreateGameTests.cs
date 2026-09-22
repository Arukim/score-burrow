using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Primitives;
using ScoreBurrow.Data;
using ScoreBurrow.Data.Entities;
using ScoreBurrow.Data.Enums;
using ScoreBurrow.Data.Statistics;
using ScoreBurrow.Rating.Services;
using ScoreBurrow.Web.Models;
using ScoreBurrow.Web.Services;

namespace ScoreBurrow.Web.Tests;

public class GameServiceCreateGameTests
{
    [Fact]
    public async Task CreateGame_PersistsParticipantsAndInvalidatesCache()
    {
        var leagueId = Guid.NewGuid();
        var playerA = Guid.NewGuid();
        var playerB = Guid.NewGuid();

        await using var context = CreateContext();
        SeedLeague(context, leagueId, playerA, playerB);
        await context.SaveChangesAsync();

        var leagues = new StubLeagueService();
        var service = CreateService(context, leagues);

        var gameId = await service.CreateGameAsync(leagueId, "admin", TwoPlayerRequest(playerA, playerB, heroIdA: 1));

        var game = await context.Games.Include(g => g.Participants).SingleAsync(g => g.Id == gameId);
        game.Status.Should().Be(GameStatus.InProgress);
        game.Participants.Should().HaveCount(2);
        game.Participants.Select(p => p.LeagueMembershipId).Should().BeEquivalentTo([playerA, playerB]);
        game.Participants.Single(p => p.LeagueMembershipId == playerA).HeroId.Should().Be(1);

        leagues.InvalidateCount.Should().Be(1);
        leagues.LastLeagueId.Should().Be(leagueId);
        leagues.LastGameId.Should().Be(gameId);
    }

    [Fact]
    public async Task CreateGame_PersistsNotesAndTownPool()
    {
        var leagueId = Guid.NewGuid();
        var playerA = Guid.NewGuid();
        var playerB = Guid.NewGuid();

        await using var context = CreateContext();
        SeedLeague(context, leagueId, playerA, playerB);
        await context.SaveChangesAsync();

        var request = TwoPlayerRequest(playerA, playerB);
        request.Notes = "  evening session  ";
        request.TownPoolTownIds = [1, 2];

        var service = CreateService(context, new StubLeagueService());
        var gameId = await service.CreateGameAsync(leagueId, "admin", request);

        var game = await context.Games.SingleAsync(g => g.Id == gameId);
        game.Notes.Should().Be("evening session");
        game.TownPoolTownIds.Should().Be("1,2");
    }

    [Fact]
    public async Task UpdateGameNotes_TrimsAndCanClear()
    {
        var leagueId = Guid.NewGuid();
        var playerA = Guid.NewGuid();
        var playerB = Guid.NewGuid();

        await using var context = CreateContext();
        SeedLeague(context, leagueId, playerA, playerB);
        await context.SaveChangesAsync();

        var leagues = new StubLeagueService();
        var service = CreateService(context, leagues);
        var gameId = await service.CreateGameAsync(leagueId, "admin", TwoPlayerRequest(playerA, playerB));

        leagues.InvalidateCount = 0;
        (await service.UpdateGameNotesAsync(gameId, "admin", "  restarted early  ")).Should().BeTrue();
        (await context.Games.SingleAsync(g => g.Id == gameId)).Notes.Should().Be("restarted early");

        (await service.UpdateGameNotesAsync(gameId, "admin", "   ")).Should().BeTrue();
        (await context.Games.SingleAsync(g => g.Id == gameId)).Notes.Should().BeNull();
        leagues.InvalidateCount.Should().Be(2);
    }

    [Fact]
    public async Task CreateGame_RejectsMembershipFromAnotherLeague()
    {
        var leagueId = Guid.NewGuid();
        var otherLeagueId = Guid.NewGuid();
        var playerA = Guid.NewGuid();
        var outsider = Guid.NewGuid();

        await using var context = CreateContext();
        SeedLeague(context, leagueId, playerA);
        SeedLeague(context, otherLeagueId, [outsider], "Other");
        await context.SaveChangesAsync();

        var service = CreateService(context, new StubLeagueService());

        var act = async () => await service.CreateGameAsync(
            leagueId,
            "admin",
            TwoPlayerRequest(playerA, outsider, heroIdA: 1));

        await act.Should().ThrowAsync<ArgumentException>().WithMessage($"*{outsider}*");
        (await context.Games.CountAsync()).Should().Be(0);
    }

    [Fact]
    public async Task CreateGame_RejectsDuplicateColors()
    {
        var leagueId = Guid.NewGuid();
        var playerA = Guid.NewGuid();
        var playerB = Guid.NewGuid();

        await using var context = CreateContext();
        SeedLeague(context, leagueId, playerA, playerB);
        await context.SaveChangesAsync();

        var request = TwoPlayerRequest(playerA, playerB);
        request.Participants[1].PlayerColor = PlayerColor.Red;

        var service = CreateService(context, new StubLeagueService());
        var act = async () => await service.CreateGameAsync(leagueId, "admin", request);

        await act.Should().ThrowAsync<ArgumentException>().WithMessage("*color*");
    }

    [Fact]
    public async Task CreateGame_RejectsHeroTownMismatch()
    {
        var leagueId = Guid.NewGuid();
        var playerA = Guid.NewGuid();
        var playerB = Guid.NewGuid();

        await using var context = CreateContext();
        SeedLeague(context, leagueId, playerA, playerB);
        await context.SaveChangesAsync();

        var service = CreateService(context, new StubLeagueService());
        var act = async () => await service.CreateGameAsync(
            leagueId,
            "admin",
            TwoPlayerRequest(playerA, playerB, heroIdA: 17));

        await act.Should().ThrowAsync<ArgumentException>().WithMessage("*Mephala*");
    }

    [Fact]
    public async Task CancelGame_InvalidatesLeagueAndGameCache()
    {
        var leagueId = Guid.NewGuid();
        var playerA = Guid.NewGuid();
        var playerB = Guid.NewGuid();

        await using var context = CreateContext();
        SeedLeague(context, leagueId, playerA, playerB);
        await context.SaveChangesAsync();

        var leagues = new StubLeagueService();
        var service = CreateService(context, leagues);
        var gameId = await service.CreateGameAsync(leagueId, "admin", TwoPlayerRequest(playerA, playerB));

        leagues.InvalidateCount = 0;
        var cancelled = await service.CancelGameAsync(gameId, "admin");

        cancelled.Should().BeTrue();
        (await context.Games.SingleAsync(g => g.Id == gameId)).Status.Should().Be(GameStatus.Cancelled);
        leagues.InvalidateCount.Should().Be(1);
        leagues.LastGameId.Should().Be(gameId);
        leagues.LastLeagueId.Should().Be(leagueId);
    }

    private static GameService CreateService(ScoreBurrowDbContext context, StubLeagueService leagues) =>
        new(context, leagues, new RatingService(), new PlayerStatisticsProjector(context));

    private static ScoreBurrowDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ScoreBurrowDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var context = new ScoreBurrowDbContext(options);
        context.Database.EnsureCreated();
        return context;
    }

    private static void SeedLeague(ScoreBurrowDbContext context, Guid leagueId, params Guid[] membershipIds) =>
        SeedLeague(context, leagueId, membershipIds, "Test League");

    private static void SeedLeague(ScoreBurrowDbContext context, Guid leagueId, Guid[] membershipIds, string name)
    {
        context.Leagues.Add(new League
        {
            Id = leagueId,
            Name = name,
            OwnerId = "admin",
            CreatedDate = DateTime.UtcNow,
            CreatedBy = "admin",
            CreatedOn = DateTime.UtcNow
        });

        foreach (var membershipId in membershipIds)
        {
            context.LeagueMemberships.Add(new LeagueMembership
            {
                Id = membershipId,
                LeagueId = leagueId,
                PlayerNickname = membershipId.ToString("N")[..8],
                Role = LeagueRole.Member,
                JoinedDate = DateTime.UtcNow,
                CreatedBy = "admin",
                CreatedOn = DateTime.UtcNow
            });
        }
    }

    private static CreateGameRequest TwoPlayerRequest(Guid playerA, Guid playerB, int? heroIdA = null) =>
        new()
        {
            MapName = "XL",
            Participants =
            [
                new ParticipantRequest
                {
                    LeagueMembershipId = playerA,
                    PlayerColor = PlayerColor.Red,
                    Position = 1,
                    TownId = 1,
                    HeroId = heroIdA
                },
                new ParticipantRequest
                {
                    LeagueMembershipId = playerB,
                    PlayerColor = PlayerColor.Blue,
                    Position = 2,
                    TownId = 2
                }
            ]
        };

    private sealed class StubLeagueService : ILeagueService
    {
        public bool IsAdmin { get; set; } = true;
        public int InvalidateCount { get; set; }
        public Guid? LastLeagueId { get; private set; }
        public Guid? LastGameId { get; private set; }

        public Task<bool> IsAdminOrOwnerAsync(string userId, Guid leagueId) => Task.FromResult(IsAdmin);

        public void InvalidateLeagueCache(Guid leagueId, string? userId = null, Guid? gameId = null)
        {
            InvalidateCount++;
            LastLeagueId = leagueId;
            LastGameId = gameId;
        }

        public Task<Guid> CreateLeagueAsync(string userId, string name, string? description) => throw new NotImplementedException();
        public Task<bool> UpdateLeagueAsync(Guid leagueId, string userId, string name, string? description) => throw new NotImplementedException();
        public Task<bool> ArchiveLeagueAsync(Guid leagueId, string userId) => throw new NotImplementedException();
        public Task<bool> UnarchiveLeagueAsync(Guid leagueId, string userId) => throw new NotImplementedException();
        public Task<bool> DeleteLeagueAsync(Guid leagueId, string userId) => throw new NotImplementedException();
        public Task<bool> AddMemberAsync(Guid leagueId, string userId, string memberEmail, string? displayName = null) => throw new NotImplementedException();
        public Task<bool> AddUnregisteredMemberAsync(Guid leagueId, string userId, string playerNickname, string? playerDisplayName = null) => throw new NotImplementedException();
        public Task<bool> LinkMemberAsync(Guid leagueId, string userId, Guid membershipId, string memberEmail) => throw new NotImplementedException();
        public Task<bool> UpdateMemberUserAsync(Guid leagueId, string userId, Guid membershipId, string newEmail) => throw new NotImplementedException();
        public Task<bool> UpdateMemberRoleAsync(Guid leagueId, string userId, Guid membershipId, LeagueRole newRole) => throw new NotImplementedException();
        public Task<bool> RemoveMemberAsync(Guid leagueId, string userId, Guid membershipId) => throw new NotImplementedException();
        public Task<bool> IsOwnerAsync(string userId, Guid leagueId) => throw new NotImplementedException();
        public Task<bool> IsMemberAsync(string userId, Guid leagueId) => throw new NotImplementedException();
        public Task<bool> RecalculateStatisticsAsync(Guid leagueId, string userId) => throw new NotImplementedException();
        public Task<bool> RecalculateRatingsAsync(Guid leagueId, string userId) => throw new NotImplementedException();
        public IChangeToken GetLeagueCacheExpirationToken(Guid leagueId) => throw new NotImplementedException();
    }
}
