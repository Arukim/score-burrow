using ScoreBurrow.Data.Enums;

namespace ScoreBurrow.Data.Entities;

public class GameParticipant : IAuditableEntity
{
    public Guid Id { get; set; }
    public Guid GameId { get; set; }
    public Guid LeagueMembershipId { get; set; }
    public int TownId { get; set; }
    public int? HeroId { get; set; }
    public PlayerColor PlayerColor { get; set; }
    public int Position { get; set; }
    public bool IsWinner { get; set; }
    public bool IsTechnicalLoss { get; set; }
    public int GoldTrade { get; set; } // Positive = received, negative = gave away

    // Audit fields
    public string CreatedBy { get; set; } = string.Empty;
    public DateTime CreatedOn { get; set; }
    public string? ModifiedBy { get; set; }
    public DateTime? ModifiedOn { get; set; }

    // Navigation properties
    public Game Game { get; set; } = null!;
    public LeagueMembership LeagueMembership { get; set; } = null!;
    public Town Town { get; set; } = null!;
    public Hero Hero { get; set; } = null!;
}
