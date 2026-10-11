namespace ScoreBurrow.Rating.Models;

/// <summary>
/// One player's stored rating result for a completed game, used to explain the point changes.
/// </summary>
public sealed record RatingOutcomePlayer(
    string Name,
    double PreviousRating,
    double PreviousDeviation,
    double NewDeviation,
    double RatingChange,
    bool IsWinner,
    bool IsTechnicalLoss);
