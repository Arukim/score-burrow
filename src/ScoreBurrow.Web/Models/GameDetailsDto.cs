using ScoreBurrow.Data.Enums;

namespace ScoreBurrow.Web.Models;

public class GameDetailsDto
{
    public Guid Id { get; set; }
    public Guid LeagueId { get; set; }
    public required string MapName { get; set; }
    public DateTime StartTime { get; set; }
    public GameStatus Status { get; set; }
    public List<ParticipantDto> Participants { get; set; } = new();
}

public class ParticipantDto
{
    public Guid Id { get; set; }
    public Guid LeagueMembershipId { get; set; }
    public required string PlayerName { get; set; }
    public PlayerColor PlayerColor { get; set; }
    public int Position { get; set; }
    public required string TownName { get; set; }
    public string? HeroName { get; set; }
    public int GoldTrade { get; set; }
    public bool IsWinner { get; set; }
    public bool IsTechnicalLoss { get; set; }
}
