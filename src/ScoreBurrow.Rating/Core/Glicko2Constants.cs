namespace ScoreBurrow.Rating.Core;

/// <summary>
/// Constants used in the Glicko-2 rating system.
/// Based on the paper by Mark Glickman: http://www.glicko.net/glicko/glicko2.pdf
/// </summary>
public static class Glicko2Constants
{
    /// <summary>
    /// Default initial rating for new players (1500 on Glicko scale)
    /// </summary>
    public const double DefaultRating = 1500.0;

    /// <summary>
    /// Default initial rating deviation for new players (350 on Glicko scale)
    /// Represents uncertainty in the rating
    /// </summary>
    public const double DefaultRatingDeviation = 350.0;

    /// <summary>
    /// Default volatility for new players (0.06)
    /// Represents the degree of expected fluctuation in a player's rating
    /// </summary>
    public const double DefaultVolatility = 0.06;

    /// <summary>
    /// System constant tau that constrains volatility over time
    /// Typical values range from 0.3 to 1.2
    /// Lower values prevent volatility from changing too quickly
    /// </summary>
    public const double Tau = 0.5;

    /// <summary>
    /// Convergence tolerance for iterative calculations
    /// </summary>
    public const double ConvergenceTolerance = 0.000001;

    /// <summary>
    /// Scaling factor to convert between Glicko-1 and Glicko-2 scales
    /// Glicko-2 scale: rating / 173.7178, RD / 173.7178
    /// </summary>
    public const double ScalingFactor = 173.7178;

    /// <summary>
    /// Minimum rating deviation - prevents RD from getting too small
    /// </summary>
    public const double MinRatingDeviation = 30.0;

    /// <summary>
    /// Maximum rating deviation - caps uncertainty
    /// </summary>
    public const double MaxRatingDeviation = 500.0;
}
