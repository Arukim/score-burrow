using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ScoreBurrow.Data.Entities;

namespace ScoreBurrow.Data.Configurations;

public class PlayerStatisticsConfiguration : IEntityTypeConfiguration<PlayerStatistics>
{
    public void Configure(EntityTypeBuilder<PlayerStatistics> builder)
    {
        builder.HasKey(ps => ps.Id);

        builder.Property(ps => ps.WinRate)
            .HasPrecision(5, 2);

        builder.Property(ps => ps.AveragePosition)
            .HasPrecision(5, 2);

        builder.HasIndex(ps => ps.LeagueMembershipId).IsUnique();

        builder.HasOne(ps => ps.LeagueMembership)
            .WithOne(m => m.Statistics)
            .HasForeignKey<PlayerStatistics>(ps => ps.LeagueMembershipId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(ps => ps.FavoriteTown)
            .WithMany(t => t.FavoriteInStatistics)
            .HasForeignKey(ps => ps.FavoriteTownId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(ps => ps.FavoriteHero)
            .WithMany(h => h.FavoriteInStatistics)
            .HasForeignKey(ps => ps.FavoriteHeroId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
