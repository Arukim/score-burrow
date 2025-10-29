namespace ScoreBurrow.Rating.Models;

/// <summary>
/// Represents the outcome of a match from a player's perspective
/// </summary>
public enum MatchOutcome
{
    /// <summary>
    /// Player lost the match
    /// </summary>
    Loss,

    /// <summary>
    /// Player drew the match
    /// </summary>
    Draw,

    /// <summary>
    /// Player won the match
    /// </summary>
    Win
}
