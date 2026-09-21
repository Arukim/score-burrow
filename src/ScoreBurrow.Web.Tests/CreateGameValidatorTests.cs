using FluentAssertions;
using ScoreBurrow.Data.Entities;
using ScoreBurrow.Data.Enums;
using ScoreBurrow.Web.Models;
using ScoreBurrow.Web.Services;

namespace ScoreBurrow.Web.Tests;

public class CreateGameValidatorTests
{
    private static readonly Guid PlayerA = Guid.NewGuid();
    private static readonly Guid PlayerB = Guid.NewGuid();

    [Fact]
    public void Validate_AcceptsTwoPlayersWithUniqueColorsAndMatchingHero()
    {
        var act = () => CreateGameValidator.Validate(
            TwoPlayers(heroIdA: 1),
            Memberships(),
            Towns(),
            Heroes());

        act.Should().NotThrow();
    }

    [Fact]
    public void Validate_RejectsDuplicatePlayers()
    {
        var participants = TwoPlayers();
        participants[1].LeagueMembershipId = PlayerA;

        var act = () => CreateGameValidator.Validate(participants, Memberships(), Towns(), Heroes());

        act.Should().Throw<ArgumentException>().WithMessage("*once*");
    }

    [Fact]
    public void Validate_RejectsDuplicateColors()
    {
        var participants = TwoPlayers();
        participants[1].PlayerColor = PlayerColor.Red;

        var act = () => CreateGameValidator.Validate(participants, Memberships(), Towns(), Heroes());

        act.Should().Throw<ArgumentException>().WithMessage("*color*");
    }

    [Fact]
    public void Validate_RejectsMembershipOutsideLeague()
    {
        var outsider = Guid.NewGuid();
        var participants = TwoPlayers();
        participants[1].LeagueMembershipId = outsider;

        var act = () => CreateGameValidator.Validate(participants, Memberships(), Towns(), Heroes());

        act.Should().Throw<ArgumentException>().WithMessage($"*{outsider}*");
    }

    [Fact]
    public void Validate_RejectsHeroFromDifferentTown()
    {
        var act = () => CreateGameValidator.Validate(
            TwoPlayers(heroIdA: 17),
            Memberships(),
            Towns(),
            Heroes());

        act.Should().Throw<ArgumentException>().WithMessage("*Mephala*");
    }

    [Fact]
    public void Validate_RejectsPlayerTownOutsidePool()
    {
        var act = () => CreateGameValidator.Validate(
            TwoPlayers(),
            Memberships(),
            new HashSet<int> { 1, 2, 3 },
            Heroes(),
            townPoolTownIds: [1, 3]);

        act.Should().Throw<ArgumentException>().WithMessage("*not in the town pool*");
    }

    [Fact]
    public void Validate_AllowsMissingHero()
    {
        var act = () => CreateGameValidator.Validate(
            TwoPlayers(heroIdA: null),
            Memberships(),
            Towns(),
            Heroes());

        act.Should().NotThrow();
    }

    [Fact]
    public void Validate_RejectsTooFewPlayers()
    {
        var act = () => CreateGameValidator.Validate(
            TwoPlayers().Take(1).ToList(),
            Memberships(),
            Towns(),
            Heroes());

        act.Should().Throw<ArgumentException>().WithMessage("*between 2 and 8*");
    }

    [Fact]
    public void NormalizeHeroId_TreatsZeroAsNone()
    {
        CreateGameValidator.NormalizeHeroId(0).Should().BeNull();
        CreateGameValidator.NormalizeHeroId(null).Should().BeNull();
        CreateGameValidator.NormalizeHeroId(1).Should().Be(1);
    }

    private static List<ParticipantRequest> TwoPlayers(int? heroIdA = 1) =>
    [
        new()
        {
            LeagueMembershipId = PlayerA,
            PlayerColor = PlayerColor.Red,
            Position = 1,
            TownId = 1,
            HeroId = heroIdA
        },
        new()
        {
            LeagueMembershipId = PlayerB,
            PlayerColor = PlayerColor.Blue,
            Position = 2,
            TownId = 2
        }
    ];

    private static Dictionary<Guid, LeagueMembership> Memberships() => new()
    {
        [PlayerA] = new LeagueMembership { Id = PlayerA, PlayerNickname = "A", LeagueId = Guid.NewGuid() },
        [PlayerB] = new LeagueMembership { Id = PlayerB, PlayerNickname = "B", LeagueId = Guid.NewGuid() }
    };

    private static HashSet<int> Towns() => [1, 2];

    private static Dictionary<int, Hero> Heroes() => new()
    {
        [1] = new Hero { Id = 1, Name = "Orrin", TownId = 1, HeroClass = "Knight" },
        [17] = new Hero { Id = 17, Name = "Mephala", TownId = 2, HeroClass = "Ranger" }
    };
}
