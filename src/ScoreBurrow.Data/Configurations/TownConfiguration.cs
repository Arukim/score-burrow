using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ScoreBurrow.Data.Entities;

namespace ScoreBurrow.Data.Configurations;

public class TownConfiguration : IEntityTypeConfiguration<Town>
{
    public void Configure(EntityTypeBuilder<Town> builder)
    {
        builder.HasKey(t => t.Id);

        builder.Property(t => t.Name)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(t => t.Description)
            .HasMaxLength(500);

        builder.HasIndex(t => t.Name).IsUnique();

        builder.HasMany(t => t.Heroes)
            .WithOne(h => h.Town)
            .HasForeignKey(h => h.TownId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(t => t.GameParticipants)
            .WithOne(gp => gp.Town)
            .HasForeignKey(gp => gp.TownId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(t => t.FavoriteInStatistics)
            .WithOne(ps => ps.FavoriteTown)
            .HasForeignKey(ps => ps.FavoriteTownId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
