namespace ScoreBurrow.Rating.Models;

/// <summary>
/// Represents an immutable snapshot of a player's rating at a specific point in time
/// </summary>
public record RatingSnapshot
{
    /// <summary>
    /// Player's rating on the Glicko-2 scale (typically starts at 1500)
    /// </summary>
    public double Rating { get; init; }

    /// <summary>
    /// Rating Deviation - measure of uncertainty in the rating (typically starts at 350)
    /// Lower RD = more certain about the rating
    /// </summary>
    public double RatingDeviation { get; init; }

    /// <summary>
    /// Volatility - degree of expected fluctuation in rating
    /// Indicates consistency of performance
    /// </summary>
    public double Volatility { get; init; }

    public RatingSnapshot(double rating, double ratingDeviation, double volatility)
    {
        Rating = rating;
        RatingDeviation = ratingDeviation;
        Volatility = volatility;
    }

    /// <summary>
    /// Creates a default rating snapshot for new players
    /// </summary>
    public static RatingSnapshot CreateDefault()
    {
        return new RatingSnapshot(
            Core.Glicko2Constants.DefaultRating,
            Core.Glicko2Constants.DefaultRatingDeviation,
            Core.Glicko2Constants.DefaultVolatility
        );
    }

    /// <summary>
    /// Converts to Glicko-2 scale (mu, phi, sigma)
    /// </summary>
    public (double mu, double phi, double sigma) ToGlicko2Scale()
    {
        var mu = (Rating - Core.Glicko2Constants.DefaultRating) / Core.Glicko2Constants.ScalingFactor;
        var phi = RatingDeviation / Core.Glicko2Constants.ScalingFactor;
        var sigma = Volatility;
        return (mu, phi, sigma);
    }

    /// <summary>
    /// Creates from Glicko-2 scale values
    /// </summary>
    public static RatingSnapshot FromGlicko2Scale(double mu, double phi, double sigma)
    {
        var rating = mu * Core.Glicko2Constants.ScalingFactor + Core.Glicko2Constants.DefaultRating;
        var rd = phi * Core.Glicko2Constants.ScalingFactor;
        return new RatingSnapshot(rating, rd, sigma);
    }
}
