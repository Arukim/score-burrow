namespace ScoreBurrow.Data.Entities;

public class Hero
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public int TownId { get; set; }
    public required string HeroClass { get; set; }

    // Navigation properties
    public Town Town { get; set; } = null!;
    public ICollection<GameParticipant> GameParticipants { get; set; } = new List<GameParticipant>();
    public ICollection<PlayerStatistics> FavoriteInStatistics { get; set; } = new List<PlayerStatistics>();
}
