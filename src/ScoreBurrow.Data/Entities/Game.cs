using ScoreBurrow.Data.Enums;

namespace ScoreBurrow.Data.Entities;

public class Game : IAuditableEntity
{
    public Guid Id { get; set; }
    public Guid LeagueId { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime? EndTime { get; set; }
    public required string MapName { get; set; }
    public GameStatus Status { get; set; }
    public Guid? WinnerId { get; set; }
    public string? Notes { get; set; }

    // Audit fields
    public string CreatedBy { get; set; } = string.Empty;
    public DateTime CreatedOn { get; set; }
    public string? ModifiedBy { get; set; }
    public DateTime? ModifiedOn { get; set; }

    // Navigation properties
    public League League { get; set; } = null!;
    public LeagueMembership? Winner { get; set; }
    public ICollection<GameParticipant> Participants { get; set; } = new List<GameParticipant>();
}
