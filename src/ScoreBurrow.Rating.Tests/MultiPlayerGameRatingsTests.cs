using FluentAssertions;
using ScoreBurrow.Rating.Core;
using ScoreBurrow.Rating.Models;
using ScoreBurrow.Rating.Services;
using Xunit;

namespace ScoreBurrow.Rating.Tests;

public class MultiPlayerGameRatingsTests
{
    private readonly RatingService _ratingService = new();
    private readonly Glicko2Calculator _calculator = new();

    [Fact]
    public void ThreePlayer_SymmetricDefaults_RatingChangesSumToZero()
    {
        var winnerId = Guid.NewGuid();
        var loser1Id = Guid.NewGuid();
        var loser2Id = Guid.NewGuid();
        var defaultRating = RatingSnapshot.CreateDefault();

        var participants = new Dictionary<Guid, RatingSnapshot>
        {
            [winnerId] = defaultRating,
            [loser1Id] = defaultRating,
            [loser2Id] = defaultRating
        };

        var updates = _ratingService.CalculateMultiPlayerGameRatings(participants, winnerId);

        updates.Values.Sum(u => u.RatingChange).Should().BeApproximately(0.0, 0.01);
    }

    [Fact]
    public void ThreePlayer_SymmetricDefaults_AllParticipantsHaveSameRdShrink()
    {
        var winnerId = Guid.NewGuid();
        var loser1Id = Guid.NewGuid();
        var loser2Id = Guid.NewGuid();
        var defaultRating = RatingSnapshot.CreateDefault();

        var participants = new Dictionary<Guid, RatingSnapshot>
        {
            [winnerId] = defaultRating,
            [loser1Id] = defaultRating,
            [loser2Id] = defaultRating
        };

        var updates = _ratingService.CalculateMultiPlayerGameRatings(participants, winnerId);

        var rdChanges = updates.Values.Select(u => u.RatingDeviationChange).ToList();
        rdChanges[0].Should().BeApproximately(rdChanges[1], 0.01);
        rdChanges[1].Should().BeApproximately(rdChanges[2], 0.01);
        rdChanges[0].Should().BeLessThan(0);
    }

    [Fact]
    public void ThreePlayer_SymmetricDefaults_PinsWinnerAndLoserChangesAtRd350()
    {
        var winnerId = Guid.NewGuid();
        var loser1Id = Guid.NewGuid();
        var loser2Id = Guid.NewGuid();
        var defaultRating = RatingSnapshot.CreateDefault();

        var participants = new Dictionary<Guid, RatingSnapshot>
        {
            [winnerId] = defaultRating,
            [loser1Id] = defaultRating,
            [loser2Id] = defaultRating
        };

        var updates = _ratingService.CalculateMultiPlayerGameRatings(participants, winnerId);

        updates[winnerId].RatingChange.Should().BeApproximately(247.3, 0.1);
        updates[loser1Id].RatingChange.Should().BeApproximately(-123.7, 0.1);
        updates[loser2Id].RatingChange.Should().BeApproximately(-123.7, 0.1);
    }

    [Fact]
    public void TwoPlayer_MatchesPlainOneVsOneGlicko2()
    {
        var winnerId = Guid.NewGuid();
        var loserId = Guid.NewGuid();
        var winnerRating = new RatingSnapshot(1600, 200, 0.06);
        var loserRating = new RatingSnapshot(1400, 150, 0.06);

        var participants = new Dictionary<Guid, RatingSnapshot>
        {
            [winnerId] = winnerRating,
            [loserId] = loserRating
        };

        var multiPlayerUpdates = _ratingService.CalculateMultiPlayerGameRatings(participants, winnerId);

        var plainWinner = _calculator.CalculateNewRating(
            winnerRating,
            new List<GameMatchup> { GameMatchup.Win(loserRating) });
        var plainLoser = _calculator.CalculateNewRating(
            loserRating,
            new List<GameMatchup> { GameMatchup.Loss(winnerRating) });

        multiPlayerUpdates[winnerId].NewRating.Rating.Should().BeApproximately(plainWinner.NewRating.Rating, 0.0001);
        multiPlayerUpdates[winnerId].NewRating.RatingDeviation.Should()
            .BeApproximately(plainWinner.NewRating.RatingDeviation, 0.0001);
        multiPlayerUpdates[loserId].NewRating.Rating.Should().BeApproximately(plainLoser.NewRating.Rating, 0.0001);
        multiPlayerUpdates[loserId].NewRating.RatingDeviation.Should()
            .BeApproximately(plainLoser.NewRating.RatingDeviation, 0.0001);
    }

    [Fact]
    public void TwoPlayer_DoesNotAddDrawMatchups()
    {
        var winnerId = Guid.NewGuid();
        var loserId = Guid.NewGuid();
        var defaultRating = RatingSnapshot.CreateDefault();

        var participants = new Dictionary<Guid, RatingSnapshot>
        {
            [winnerId] = defaultRating,
            [loserId] = defaultRating
        };

        var updates = _ratingService.CalculateMultiPlayerGameRatings(participants, winnerId);

        // With only one loser there is no draw partner; changes must mirror plain 1v1 defaults.
        var plainWinner = _calculator.CalculateNewRating(
            defaultRating,
            new List<GameMatchup> { GameMatchup.Win(defaultRating) });
        var plainLoser = _calculator.CalculateNewRating(
            defaultRating,
            new List<GameMatchup> { GameMatchup.Loss(defaultRating) });

        updates[winnerId].RatingChange.Should().BeApproximately(plainWinner.RatingChange, 0.0001);
        updates[loserId].RatingChange.Should().BeApproximately(plainLoser.RatingChange, 0.0001);
    }

    [Theory]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    public void EveryParticipantPlaysExactlyNMinusOneMatchWorthOfRdShrink(int playerCount)
    {
        var ids = Enumerable.Range(0, playerCount).Select(_ => Guid.NewGuid()).ToList();
        var winnerId = ids[0];
        var defaultRating = RatingSnapshot.CreateDefault();
        var participants = ids.ToDictionary(id => id, _ => defaultRating);

        var updates = _ratingService.CalculateMultiPlayerGameRatings(participants, winnerId);

        updates.Should().HaveCount(playerCount);
        // All start identical and each plays N-1 virtual matches, so RD change is shared.
        var rdChange = updates[winnerId].RatingDeviationChange;
        foreach (var update in updates.Values)
        {
            update.RatingDeviationChange.Should().BeApproximately(rdChange, 0.01);
        }
    }
}
