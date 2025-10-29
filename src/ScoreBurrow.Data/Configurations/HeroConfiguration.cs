using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ScoreBurrow.Data.Entities;

namespace ScoreBurrow.Data.Configurations;

public class HeroConfiguration : IEntityTypeConfiguration<Hero>
{
    public void Configure(EntityTypeBuilder<Hero> builder)
    {
        builder.HasKey(h => h.Id);

        builder.Property(h => h.Name)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(h => h.HeroClass)
            .IsRequired()
            .HasMaxLength(100);
        builder.HasIndex(h => h.TownId);

        builder.HasOne(h => h.Town)
            .WithMany(t => t.Heroes)
            .HasForeignKey(h => h.TownId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(h => h.GameParticipants)
            .WithOne(gp => gp.Hero)
            .HasForeignKey(gp => gp.HeroId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(h => h.FavoriteInStatistics)
            .WithOne(ps => ps.FavoriteHero)
            .HasForeignKey(ps => ps.FavoriteHeroId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
