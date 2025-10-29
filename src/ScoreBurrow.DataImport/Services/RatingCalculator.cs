using ScoreBurrow.Data;
using ScoreBurrow.Data.Entities;
using ScoreBurrow.Rating.Models;
using ScoreBurrow.Rating.Services;

namespace ScoreBurrow.DataImport.Services;

/// <summary>
/// Manages rating calculations and tracking for game imports
/// </summary>
public class RatingCalculator
{
    private readonly IRatingService _ratingService;
    private readonly Dictionary<Guid, RatingSnapshot> _currentRatings;

    public RatingCalculator()
    {
        _ratingService = new RatingService();
        _currentRatings = new Dictionary<Guid, RatingSnapshot>();
    }

    /// <summary>
    /// Gets the current rating for a league membership, initializing if needed
    /// </summary>
    public RatingSnapshot GetCurrentRating(Guid leagueMembershipId)
    {
        if (!_currentRatings.ContainsKey(leagueMembershipId))
        {
            _currentRatings[leagueMembershipId] = RatingSnapshot.CreateDefault();
        }
        return _currentRatings[leagueMembershipId];
    }

    /// <summary>
    /// Calculates ratings for a normal multi-player game
    /// </summary>
    public Dictionary<Guid, RatingUpdate> CalculateGameRatings(
        List<GameParticipant> participants,
        Guid winnerId)
    {
        // Build rating snapshot dictionary
        var participantRatings = new Dictionary<Guid, RatingSnapshot>();
        
        foreach (var participant in participants)
        {
            var currentRating = GetCurrentRating(participant.LeagueMembershipId);
            participantRatings[participant.LeagueMembershipId] = currentRating;
            
            // Store snapshot in participant
            participant.RatingAtGameTime = currentRating.Rating;
            participant.RatingDeviationAtGameTime = currentRating.RatingDeviation;
            participant.VolatilityAtGameTime = currentRating.Volatility;
        }

        // Calculate new ratings
        var updates = _ratingService.CalculateMultiPlayerGameRatings(participantRatings, winnerId);

        // Update current ratings
        foreach (var update in updates)
        {
            _currentRatings[update.Key] = update.Value.NewRating;
        }

        return updates;
    }

    /// <summary>
    /// Applies technical loss penalty to the culprit
    /// Other players' ratings are not affected
    /// </summary>
    public RatingUpdate ApplyTechnicalLossPenalty(GameParticipant culpritParticipant)
    {
        var currentRating = GetCurrentRating(culpritParticipant.LeagueMembershipId);
        
        // Store snapshot in participant
        culpritParticipant.RatingAtGameTime = currentRating.Rating;
        culpritParticipant.RatingDeviationAtGameTime = currentRating.RatingDeviation;
        culpritParticipant.VolatilityAtGameTime = currentRating.Volatility;

        var update = _ratingService.ApplyTechnicalLossPenalty(currentRating);

        // Update current rating
        _currentRatings[culpritParticipant.LeagueMembershipId] = update.NewRating;

        return update;
    }

    /// <summary>
    /// Creates RatingHistory entry from a rating update
    /// </summary>
    public RatingHistory CreateRatingHistory(
        Guid leagueMembershipId,
        Guid gameId,
        RatingUpdate update,
        DateTime gameDate)
    {
        return new RatingHistory
        {
            Id = Guid.NewGuid(),
            LeagueMembershipId = leagueMembershipId,
            GameId = gameId,
            CalculatedAt = gameDate,
            PreviousRating = update.PreviousRating.Rating,
            PreviousRatingDeviation = update.PreviousRating.RatingDeviation,
            PreviousVolatility = update.PreviousRating.Volatility,
            NewRating = update.NewRating.Rating,
            NewRatingDeviation = update.NewRating.RatingDeviation,
            NewVolatility = update.NewRating.Volatility
        };
    }
}
