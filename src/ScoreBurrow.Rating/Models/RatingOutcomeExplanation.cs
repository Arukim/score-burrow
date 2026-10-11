using System.Globalization;

namespace ScoreBurrow.Rating.Models;

/// <summary>
/// Plain-language notes for a completed game's rating changes.
/// Uses the stored results. It does not recalculate them.
/// </summary>
public static class RatingOutcomeExplanation
{
    public const string DeviationMeaning =
        "Deviation is how uncertain a rating is. A higher deviation lets one game move the rating further.";

    public static IReadOnlyList<string> Explain(IReadOnlyList<RatingOutcomePlayer> players)
    {
        if (players.Count == 0)
        {
            return Array.Empty<string>();
        }

        var notes = new List<string> { DeviationMeaning };
        var culprits = players.Where(p => p.IsTechnicalLoss).ToList();
        if (culprits.Count > 0)
        {
            foreach (var culprit in culprits)
            {
                notes.Add(
                    $"{culprit.Name}'s change was {Points(culprit.RatingChange)} from a technical loss at rating {Number(culprit.PreviousRating)} ± {Number(culprit.PreviousDeviation)}.");
            }

            notes.Add("The penalty is split equally across the other players, and their deviation stays the same.");
            return notes;
        }

        var losers = players.Where(p => !p.IsWinner).OrderByDescending(p => p.PreviousRating).ToList();
        var explainedPairs = 0;
        for (var i = 0; i < losers.Count; i++)
        {
            for (var j = i + 1; j < losers.Count; j++)
            {
                var higher = losers[i];
                var lower = losers[j];
                if (higher.PreviousRating <= lower.PreviousRating + 0.05)
                {
                    continue;
                }

                if (higher.RatingChange <= lower.RatingChange + 0.05)
                {
                    continue;
                }

                if (lower.PreviousDeviation <= higher.PreviousDeviation + 0.5)
                {
                    continue;
                }

                notes.Add(
                    $"{lower.Name}'s change was {Points(lower.RatingChange)} and {higher.Name}'s was {Points(higher.RatingChange)}. {lower.Name}'s deviation was {Number(lower.PreviousDeviation)} and {higher.Name}'s was {Number(higher.PreviousDeviation)}, so the less certain rating moved further.");
                explainedPairs++;
            }
        }

        if (explainedPairs == 0 && losers.Count > 1)
        {
            var largestLoss = losers.OrderBy(p => p.RatingChange).First();
            var smallestLoss = losers.OrderByDescending(p => p.RatingChange).First();
            if (largestLoss.PreviousRating > smallestLoss.PreviousRating + 0.05)
            {
                notes.Add(
                    $"{largestLoss.Name}'s change was {Points(largestLoss.RatingChange)}, the largest loss. A higher rating means a win was more expected, so that loss costs more points.");
            }
        }

        return notes;
    }

    private static string Points(double change) =>
        change.ToString("+0.0;-0.0", CultureInfo.InvariantCulture);

    private static string Number(double value) =>
        value.ToString("0.0", CultureInfo.InvariantCulture);
}
