namespace ScoreBurrow.Data.Entities;

public class Town
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public string? Description { get; set; }

    // Navigation properties
    public ICollection<Hero> Heroes { get; set; } = new List<Hero>();
    public ICollection<GameParticipant> GameParticipants { get; set; } = new List<GameParticipant>();
    public ICollection<PlayerStatistics> FavoriteInStatistics { get; set; } = new List<PlayerStatistics>();
}
