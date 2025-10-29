namespace ScoreBurrow.Data.Entities;

public class PlayerStatistics
{
    public Guid Id { get; set; }
    public Guid LeagueMembershipId { get; set; }
    public int GamesPlayed { get; set; }
    public int GamesWon { get; set; }
    public int TechnicalLosses { get; set; }
    public decimal WinRate { get; set; }
    public decimal AveragePosition { get; set; }
    public int? FavoriteTownId { get; set; }
    public int? FavoriteHeroId { get; set; }
    public DateTime LastUpdated { get; set; }

    // Navigation properties
    public LeagueMembership LeagueMembership { get; set; } = null!;
    public Town? FavoriteTown { get; set; }
    public Hero? FavoriteHero { get; set; }
}
