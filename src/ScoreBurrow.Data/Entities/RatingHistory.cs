namespace ScoreBurrow.Data.Entities;

/// <summary>
/// Tracks the complete rating history for each player after each game
/// Provides full audit trail of rating evolution
/// </summary>
public class RatingHistory : IAuditableEntity
{
    public Guid Id { get; set; }
    public Guid LeagueMembershipId { get; set; }
    public Guid GameId { get; set; }
    public DateTime CalculatedAt { get; set; }
    
    // Rating before this game
    public double PreviousRating { get; set; }
    public double PreviousRatingDeviation { get; set; }
    public double PreviousVolatility { get; set; }
    
    // Rating after this game
    public double NewRating { get; set; }
    public double NewRatingDeviation { get; set; }
    public double NewVolatility { get; set; }
    
    // Computed property - change in rating points
    public double RatingChange => NewRating - PreviousRating;

    // Audit fields
    public string CreatedBy { get; set; } = string.Empty;
    public DateTime CreatedOn { get; set; }
    public string? ModifiedBy { get; set; }
    public DateTime? ModifiedOn { get; set; }

    // Navigation properties
    public LeagueMembership LeagueMembership { get; set; } = null!;
    public Game Game { get; set; } = null!;
}
