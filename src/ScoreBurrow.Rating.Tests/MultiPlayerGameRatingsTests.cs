using FluentAssertions;
using ScoreBurrow.Rating.Core;
using ScoreBurrow.Rating.Models;
using ScoreBurrow.Rating.Services;

namespace ScoreBurrow.Rating.Tests;

public class MultiPlayerGameRatingsTests
{
    private readonly RatingService _ratingService = new();
    private readonly Glicko2Calculator _calculator = new();

    [Fact]
    public void ThreePlayer_SymmetricDefaults_RatingChangesSumToZero()
    {
        var (updates, winnerId, _, _) = ThreePlayerGame(RatingSnapshot.CreateDefault());

        updates.Values.Sum(u => u.RatingChange).Should().BeApproximately(0.0, 0.01);
        updates[winnerId].RatingChange.Should().BeGreaterThan(0);
        updates.Where(u => u.Key != winnerId).Should().OnlyContain(u => u.Value.RatingChange < 0);
    }

    [Fact]
    public void ThreePlayer_MixedRd_RatingChangesSumToZero()
    {
        var winnerId = Guid.NewGuid();
        var loser1Id = Guid.NewGuid();
        var loser2Id = Guid.NewGuid();

        var participants = new Dictionary<Guid, RatingSnapshot>
        {
            [winnerId] = new RatingSnapshot(1500, 60, 0.06),
            [loser1Id] = new RatingSnapshot(1500, 350, 0.06),
            [loser2Id] = new RatingSnapshot(1500, 120, 0.06)
        };

        var updates = _ratingService.CalculateMultiPlayerGameRatings(participants, winnerId);

        updates.Values.Sum(u => u.RatingChange).Should().BeApproximately(0.0, 0.01);
    }

    [Fact]
    public void ThreePlayer_ConservationDoesNotChangeRatingDeviation()
    {
        var winner = new RatingSnapshot(1600, 80, 0.06);
        var loser1 = new RatingSnapshot(1500, 200, 0.06);
        var loser2 = new RatingSnapshot(1400, 350, 0.06);
        var winnerId = Guid.NewGuid();
        var loser1Id = Guid.NewGuid();
        var loser2Id = Guid.NewGuid();

        var participants = new Dictionary<Guid, RatingSnapshot>
        {
            [winnerId] = winner,
            [loser1Id] = loser1,
            [loser2Id] = loser2
        };

        var updates = _ratingService.CalculateMultiPlayerGameRatings(participants, winnerId);

        var rawWinner = _calculator.CalculateNewRating(winner, new List<GameMatchup>
        {
            GameMatchup.Win(loser1),
            GameMatchup.Win(loser2)
        });
        var rawLoser1 = _calculator.CalculateNewRating(loser1, new List<GameMatchup> { GameMatchup.Loss(winner) });
        var rawLoser2 = _calculator.CalculateNewRating(loser2, new List<GameMatchup> { GameMatchup.Loss(winner) });

        updates[winnerId].NewRating.RatingDeviation.Should().BeApproximately(rawWinner.NewRating.RatingDeviation, 0.0001);
        updates[loser1Id].NewRating.RatingDeviation.Should().BeApproximately(rawLoser1.NewRating.RatingDeviation, 0.0001);
        updates[loser2Id].NewRating.RatingDeviation.Should().BeApproximately(rawLoser2.NewRating.RatingDeviation, 0.0001);
    }

    [Fact]
    public void ThreePlayer_Symmetric_WinnerRdShrinksMoreThanLosers()
    {
        var (updates, winnerId, loser1Id, _) = ThreePlayerGame(RatingSnapshot.CreateDefault());

        updates[winnerId].RatingDeviationChange.Should().BeLessThan(updates[loser1Id].RatingDeviationChange);
        updates[winnerId].RatingDeviationChange.Should().BeLessThan(0);
    }

    [Fact]
    public void TwoPlayer_EqualRd_MatchesPlainOneVsOneGlicko2()
    {
        var winnerId = Guid.NewGuid();
        var loserId = Guid.NewGuid();
        var winnerRating = new RatingSnapshot(1600, 200, 0.06);
        var loserRating = new RatingSnapshot(1400, 200, 0.06);

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

        multiPlayerUpdates[winnerId].NewRating.Rating.Should().BeApproximately(plainWinner.NewRating.Rating, 0.01);
        multiPlayerUpdates[loserId].NewRating.Rating.Should().BeApproximately(plainLoser.NewRating.Rating, 0.01);
        multiPlayerUpdates.Values.Sum(u => u.RatingChange).Should().BeApproximately(0.0, 0.01);
    }

    [Fact]
    public void TechnicalLoss_SplitsPenaltyAmongOtherParticipants()
    {
        var culpritId = Guid.NewGuid();
        var other1Id = Guid.NewGuid();
        var other2Id = Guid.NewGuid();
        var culprit = new RatingSnapshot(1700, 180, 0.05);
        var other1 = new RatingSnapshot(1500, 100, 0.06);
        var other2 = new RatingSnapshot(1400, 250, 0.06);

        var participants = new Dictionary<Guid, RatingSnapshot>
        {
            [culpritId] = culprit,
            [other1Id] = other1,
            [other2Id] = other2
        };

        var updates = _ratingService.CalculateTechnicalLossRatings(participants, culpritId);
        var penalty = _ratingService.ApplyTechnicalLossPenalty(culprit);

        updates[culpritId].RatingChange.Should().BeLessThan(0);
        updates[culpritId].NewRating.Rating.Should().BeApproximately(penalty.NewRating.Rating, 0.0001);
        updates[culpritId].NewRating.RatingDeviation.Should().BeApproximately(penalty.NewRating.RatingDeviation, 0.0001);

        var share = -updates[culpritId].RatingChange / 2;
        updates[other1Id].RatingChange.Should().BeApproximately(share, 0.0001);
        updates[other2Id].RatingChange.Should().BeApproximately(share, 0.0001);
        updates.Values.Sum(u => u.RatingChange).Should().BeApproximately(0.0, 0.01);

        updates[other1Id].NewRating.RatingDeviation.Should().BeApproximately(other1.RatingDeviation, 0.0001);
        updates[other1Id].NewRating.Volatility.Should().BeApproximately(other1.Volatility, 0.0001);
        updates[other2Id].NewRating.RatingDeviation.Should().BeApproximately(other2.RatingDeviation, 0.0001);
        updates[other2Id].NewRating.Volatility.Should().BeApproximately(other2.Volatility, 0.0001);
    }

    [Fact]
    public void TechnicalLoss_TwoPlayer_SendsFullPenaltyToOpponent()
    {
        var culpritId = Guid.NewGuid();
        var opponentId = Guid.NewGuid();
        var culprit = new RatingSnapshot(1600, 120, 0.06);
        var opponent = new RatingSnapshot(1500, 200, 0.06);

        var participants = new Dictionary<Guid, RatingSnapshot>
        {
            [culpritId] = culprit,
            [opponentId] = opponent
        };

        var updates = _ratingService.CalculateTechnicalLossRatings(participants, culpritId);

        updates[culpritId].RatingChange.Should().BeLessThan(0);
        updates[opponentId].RatingChange.Should().BeApproximately(-updates[culpritId].RatingChange, 0.0001);
        updates[opponentId].NewRating.RatingDeviation.Should().BeApproximately(opponent.RatingDeviation, 0.0001);
        updates.Values.Sum(u => u.RatingChange).Should().BeApproximately(0.0, 0.01);
    }

    private (Dictionary<Guid, RatingUpdate> Updates, Guid WinnerId, Guid Loser1Id, Guid Loser2Id) ThreePlayerGame(
        RatingSnapshot rating)
    {
        var winnerId = Guid.NewGuid();
        var loser1Id = Guid.NewGuid();
        var loser2Id = Guid.NewGuid();
        var participants = new Dictionary<Guid, RatingSnapshot>
        {
            [winnerId] = rating,
            [loser1Id] = rating,
            [loser2Id] = rating
        };

        return (_ratingService.CalculateMultiPlayerGameRatings(participants, winnerId), winnerId, loser1Id, loser2Id);
    }
}
