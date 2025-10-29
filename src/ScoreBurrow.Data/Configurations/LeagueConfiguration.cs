using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ScoreBurrow.Data.Entities;

namespace ScoreBurrow.Data.Configurations;

public class LeagueConfiguration : IEntityTypeConfiguration<League>
{
    public void Configure(EntityTypeBuilder<League> builder)
    {
        builder.HasKey(l => l.Id);

        builder.Property(l => l.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(l => l.Description)
            .HasMaxLength(1000);

        builder.Property(l => l.OwnerId)
            .IsRequired()
            .HasMaxLength(450);

        builder.Property(l => l.CreatedBy)
            .IsRequired()
            .HasMaxLength(450);

        builder.Property(l => l.ModifiedBy)
            .HasMaxLength(450);

        builder.HasIndex(l => l.OwnerId);
        builder.HasIndex(l => l.IsActive);

        builder.HasMany(l => l.Memberships)
            .WithOne(m => m.League)
            .HasForeignKey(m => m.LeagueId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(l => l.Games)
            .WithOne(g => g.League)
            .HasForeignKey(g => g.LeagueId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
