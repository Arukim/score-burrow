namespace ScoreBurrow.Rating.Models;

/// <summary>
/// Represents a single match outcome between two players
/// </summary>
public record GameMatchup
{
    /// <summary>
    /// The opponent's rating snapshot
    /// </summary>
    public RatingSnapshot OpponentRating { get; init; }

    /// <summary>
    /// Match outcome from the player's perspective
    /// </summary>
    public MatchOutcome Outcome { get; init; }

    /// <summary>
    /// Glicko-2 score value: 1.0 = win, 0.5 = draw, 0.0 = loss
    /// </summary>
    public double Score => Outcome switch
    {
        MatchOutcome.Win => 1.0,
        MatchOutcome.Draw => 0.5,
        MatchOutcome.Loss => 0.0,
        _ => throw new InvalidOperationException("Unknown match outcome")
    };

    public GameMatchup(RatingSnapshot opponentRating, MatchOutcome outcome)
    {
        OpponentRating = opponentRating ?? throw new ArgumentNullException(nameof(opponentRating));
        Outcome = outcome;
    }

    /// <summary>
    /// Creates a matchup representing a win
    /// </summary>
    public static GameMatchup Win(RatingSnapshot opponentRating) => new(opponentRating, MatchOutcome.Win);

    /// <summary>
    /// Creates a matchup representing a loss
    /// </summary>
    public static GameMatchup Loss(RatingSnapshot opponentRating) => new(opponentRating, MatchOutcome.Loss);

    /// <summary>
    /// Creates a matchup representing a draw
    /// </summary>
    public static GameMatchup Draw(RatingSnapshot opponentRating) => new(opponentRating, MatchOutcome.Draw);
}
