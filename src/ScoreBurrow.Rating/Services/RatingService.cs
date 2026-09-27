namespace ScoreBurrow.Rating.Services;

using ScoreBurrow.Rating.Core;
using ScoreBurrow.Rating.Models;

/// <summary>
/// Implementation of rating service for Heroes of Might and Magic games
/// Handles multi-player games and technical losses
/// </summary>
public class RatingService : IRatingService
{
    private readonly Glicko2Calculator _calculator;

    public RatingService()
    {
        _calculator = new Glicko2Calculator();
    }

    /// <summary>
    /// Calculates rating updates for a multi-player game where one player wins.
    /// Winner plays N-1 matches (against each loser, all wins).
    /// Each loser plays 1 match against the winner.
    /// The net Glicko change is then redistributed so the participants' ratings sum to zero change.
    /// </summary>
    public Dictionary<Guid, RatingUpdate> CalculateMultiPlayerGameRatings(
        Dictionary<Guid, RatingSnapshot> participants,
        Guid winnerId)
    {
        if (!participants.ContainsKey(winnerId))
        {
            throw new ArgumentException("Winner must be in participants list", nameof(winnerId));
        }

        if (participants.Count < 2)
        {
            throw new ArgumentException("Must have at least 2 participants", nameof(participants));
        }

        var results = new Dictionary<Guid, RatingUpdate>();
        var winnerRating = participants[winnerId];
        var losers = participants.Where(p => p.Key != winnerId).ToList();

        // Winner plays N-1 matches, one against each loser (all wins)
        var winnerMatchups = new List<GameMatchup>();
        foreach (var loser in losers)
        {
            winnerMatchups.Add(GameMatchup.Win(loser.Value));
        }
        results[winnerId] = _calculator.CalculateNewRating(winnerRating, winnerMatchups);

        // Each loser plays 1 match against the winner (all losses)
        foreach (var loser in losers)
        {
            var loserMatchups = new List<GameMatchup>
            {
                GameMatchup.Loss(winnerRating)
            };
            results[loser.Key] = _calculator.CalculateNewRating(loser.Value, loserMatchups);
        }

        return ConservePool(results);
    }

    /// <summary>
    /// Applies penalty to player who caused technical loss
    /// Implementation: Player plays against themselves and loses
    /// This naturally scales the penalty with the player's current rating
    /// Higher rated players lose more rating from technical losses
    /// </summary>
    public RatingUpdate ApplyTechnicalLossPenalty(RatingSnapshot culpritRating)
    {
        // Culprit plays against themselves with their current rating
        var matchups = new List<GameMatchup>
        {
            GameMatchup.Loss(culpritRating)
        };

        return _calculator.CalculateNewRating(culpritRating, matchups);
    }

    /// <summary>
    /// Technical loss for a whole game: culprit takes the self-loss penalty, and that
    /// rating loss is split equally among the other participants so the pool is unchanged.
    /// Recipients' rating deviation and volatility stay the same.
    /// </summary>
    public Dictionary<Guid, RatingUpdate> CalculateTechnicalLossRatings(
        Dictionary<Guid, RatingSnapshot> participants,
        Guid culpritId)
    {
        if (!participants.ContainsKey(culpritId))
        {
            throw new ArgumentException("Culprit must be in participants list", nameof(culpritId));
        }

        if (participants.Count < 2)
        {
            throw new ArgumentException("Must have at least 2 participants", nameof(participants));
        }

        var culpritUpdate = ApplyTechnicalLossPenalty(participants[culpritId]);
        var share = -culpritUpdate.RatingChange / (participants.Count - 1);

        var results = new Dictionary<Guid, RatingUpdate>
        {
            [culpritId] = culpritUpdate
        };

        foreach (var (id, snapshot) in participants)
        {
            if (id == culpritId)
            {
                continue;
            }

            results[id] = AdjustedRating(
                snapshot,
                snapshot.Rating + share,
                snapshot.RatingDeviation,
                snapshot.Volatility);
        }

        return results;
    }

    /// <summary>
    /// Direct calculation using provided matchups
    /// </summary>
    public RatingUpdate CalculateRating(RatingSnapshot currentRating, List<GameMatchup> matchups)
    {
        return _calculator.CalculateNewRating(currentRating, matchups);
    }

    /// <summary>
    /// Shifts every participant's new rating by the same amount so the sum of rating
    /// changes is zero. Rating deviation and volatility are left as Glicko computed them.
    /// </summary>
    private static Dictionary<Guid, RatingUpdate> ConservePool(Dictionary<Guid, RatingUpdate> updates)
    {
        var net = updates.Values.Sum(u => u.RatingChange);
        var shift = -net / updates.Count;

        if (Math.Abs(shift) < 1e-9)
        {
            return updates;
        }

        return updates.ToDictionary(
            pair => pair.Key,
            pair => AdjustedRating(
                pair.Value.PreviousRating,
                pair.Value.NewRating.Rating + shift,
                pair.Value.NewRating.RatingDeviation,
                pair.Value.NewRating.Volatility));
    }

    private static RatingUpdate AdjustedRating(
        RatingSnapshot previous,
        double rating,
        double ratingDeviation,
        double volatility)
    {
        return new RatingUpdate(previous, new RatingSnapshot(rating, ratingDeviation, volatility));
    }
}
