using ScoreBurrow.Data.Enums;

namespace ScoreBurrow.Data.Entities;

public class LeagueMembership : IAuditableEntity
{
    public Guid Id { get; set; }
    public Guid LeagueId { get; set; }
    public string? UserId { get; set; } // Nullable to support unregistered players
    public required string PlayerNickname { get; set; }
    public string? PlayerDisplayName { get; set; } // For unregistered users
    public LeagueRole Role { get; set; }
    public DateTime JoinedDate { get; set; }
    
    // Glicko-2 Rating System
    public double Glicko2Rating { get; set; } = 1500.0;
    public double Glicko2RatingDeviation { get; set; } = 350.0;
    public double Glicko2Volatility { get; set; } = 0.06;
    public DateTime? LastRatingUpdate { get; set; }

    // Audit fields
    public string CreatedBy { get; set; } = string.Empty;
    public DateTime CreatedOn { get; set; }
    public string? ModifiedBy { get; set; }
    public DateTime? ModifiedOn { get; set; }

    // Computed property
    public bool IsRegistered => !string.IsNullOrEmpty(UserId);

    // Navigation properties
    public League League { get; set; } = null!;
    public ICollection<GameParticipant> GameParticipants { get; set; } = new List<GameParticipant>();
    public ICollection<Game> GamesWon { get; set; } = new List<Game>();
    public PlayerStatistics? Statistics { get; set; }
}
