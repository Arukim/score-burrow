using ScoreBurrow.Rating.Models;
using ScoreBurrow.Web.Models;

namespace ScoreBurrow.Web.Services;

public static class RatingOutcomeNotes
{
    public readonly record struct HistoryRow(
        Guid MembershipId,
        double PreviousRating,
        double PreviousDeviation,
        double NewRating,
        double NewDeviation,
        DateTime CalculatedAt);

    public readonly record struct ParticipantRow(
        Guid MembershipId,
        string Name,
        bool IsWinner,
        bool IsTechnicalLoss);

    public static Dictionary<Guid, RatingResultView> Results(IEnumerable<HistoryRow> history)
    {
        return history
            .GroupBy(row => row.MembershipId)
            .ToDictionary(
                group => group.Key,
                group =>
                {
                    var latest = group.OrderByDescending(row => row.CalculatedAt).First();
                    return new RatingResultView
                    {
                        PreviousRating = latest.PreviousRating,
                        PreviousDeviation = latest.PreviousDeviation,
                        NewRating = latest.NewRating,
                        NewDeviation = latest.NewDeviation
                    };
                });
    }

    public static IReadOnlyList<string> Explain(
        IEnumerable<ParticipantRow> participants,
        IReadOnlyDictionary<Guid, RatingResultView> results)
    {
        var players = new List<RatingOutcomePlayer>();
        foreach (var participant in participants)
        {
            if (!results.TryGetValue(participant.MembershipId, out var result))
            {
                continue;
            }

            players.Add(new RatingOutcomePlayer(
                participant.Name,
                result.PreviousRating,
                result.PreviousDeviation,
                result.NewDeviation,
                result.Change,
                participant.IsWinner,
                participant.IsTechnicalLoss));
        }

        return RatingOutcomeExplanation.Explain(players);
    }
}
