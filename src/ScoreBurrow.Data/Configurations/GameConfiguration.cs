using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ScoreBurrow.Data.Entities;

namespace ScoreBurrow.Data.Configurations;

public class GameConfiguration : IEntityTypeConfiguration<Game>
{
    public void Configure(EntityTypeBuilder<Game> builder)
    {
        builder.HasKey(g => g.Id);

        builder.Property(g => g.MapName)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(g => g.Status)
            .IsRequired();

        builder.Property(g => g.Notes)
            .HasMaxLength(2000);

        builder.Property(g => g.CreatedBy)
            .IsRequired()
            .HasMaxLength(450);

        builder.Property(g => g.ModifiedBy)
            .HasMaxLength(450);

        builder.HasIndex(g => g.LeagueId);
        builder.HasIndex(g => g.StartTime);
        builder.HasIndex(g => g.Status);

        builder.HasOne(g => g.League)
            .WithMany(l => l.Games)
            .HasForeignKey(g => g.LeagueId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(g => g.Winner)
            .WithMany(m => m.GamesWon)
            .HasForeignKey(g => g.WinnerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(g => g.Participants)
            .WithOne(gp => gp.Game)
            .HasForeignKey(gp => gp.GameId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
