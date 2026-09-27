namespace ScoreBurrow.Rating.Services;

using ScoreBurrow.Rating.Models;

/// <summary>
/// Service interface for calculating player ratings
/// </summary>
public interface IRatingService
{
    /// <summary>
    /// Calculates rating updates for a multi-player game (N players, 1 winner).
    /// Winner plays N-1 wins; each loser plays one loss vs the winner.
    /// The net change is redistributed so the participants' rating changes sum to zero.
    /// </summary>
    /// <param name="participants">List of participants with their current ratings</param>
    /// <param name="winnerId">ID of the winning participant</param>
    /// <returns>Dictionary of participant IDs to their rating updates</returns>
    Dictionary<Guid, RatingUpdate> CalculateMultiPlayerGameRatings(
        Dictionary<Guid, RatingSnapshot> participants,
        Guid winnerId);

    /// <summary>
    /// Applies technical loss penalty to a player who caused technical loss
    /// The player "plays against themselves" and loses
    /// </summary>
    /// <param name="culpritRating">Current rating of the player who caused technical loss</param>
    /// <returns>Rating update with penalty applied</returns>
    RatingUpdate ApplyTechnicalLossPenalty(RatingSnapshot culpritRating);

    /// <summary>
    /// Technical loss for a whole game. The culprit takes the self-loss penalty and that
    /// loss is split equally among the other participants so the pool is unchanged.
    /// </summary>
    /// <param name="participants">All participants in the game, including the culprit</param>
    /// <param name="culpritId">Participant who caused the technical loss</param>
    /// <returns>Rating update for every participant</returns>
    Dictionary<Guid, RatingUpdate> CalculateTechnicalLossRatings(
        Dictionary<Guid, RatingSnapshot> participants,
        Guid culpritId);

    /// <summary>
    /// Calculates new rating based on direct matchups
    /// </summary>
    RatingUpdate CalculateRating(RatingSnapshot currentRating, List<GameMatchup> matchups);
}
