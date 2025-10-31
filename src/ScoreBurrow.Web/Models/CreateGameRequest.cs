using ScoreBurrow.Data.Enums;

namespace ScoreBurrow.Web.Models;

public class CreateGameRequest
{
    public required string MapName { get; set; }
    public List<ParticipantRequest> Participants { get; set; } = new();
}

public class ParticipantRequest
{
    public Guid LeagueMembershipId { get; set; }
    public PlayerColor PlayerColor { get; set; }
    public int Position { get; set; }
    public int TownId { get; set; }
    public int? HeroId { get; set; }
    public int GoldTrade { get; set; }
}
