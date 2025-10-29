namespace ScoreBurrow.Data.Entities;

public class League : IAuditableEntity
{
    public Guid Id { get; set; }
    public required string Name { get; set; }
    public string? Description { get; set; }
    public required string OwnerId { get; set; }
    public DateTime CreatedDate { get; set; }
    public bool IsActive { get; set; } = true;

    // Audit fields
    public string CreatedBy { get; set; } = string.Empty;
    public DateTime CreatedOn { get; set; }
    public string? ModifiedBy { get; set; }
    public DateTime? ModifiedOn { get; set; }

    // Navigation properties
    public ICollection<LeagueMembership> Memberships { get; set; } = new List<LeagueMembership>();
    public ICollection<Game> Games { get; set; } = new List<Game>();
}
