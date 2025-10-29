namespace ScoreBurrow.Rating.Models;

/// <summary>
/// Represents the result of a rating calculation - before and after states
/// </summary>
public record RatingUpdate
{
    /// <summary>
    /// Rating before the update
    /// </summary>
    public RatingSnapshot PreviousRating { get; init; }

    /// <summary>
    /// Rating after the update
    /// </summary>
    public RatingSnapshot NewRating { get; init; }

    /// <summary>
    /// Change in rating points
    /// </summary>
    public double RatingChange => NewRating.Rating - PreviousRating.Rating;

    /// <summary>
    /// Change in rating deviation
    /// </summary>
    public double RatingDeviationChange => NewRating.RatingDeviation - PreviousRating.RatingDeviation;

    public RatingUpdate(RatingSnapshot previousRating, RatingSnapshot newRating)
    {
        PreviousRating = previousRating ?? throw new ArgumentNullException(nameof(previousRating));
        NewRating = newRating ?? throw new ArgumentNullException(nameof(newRating));
    }
}
