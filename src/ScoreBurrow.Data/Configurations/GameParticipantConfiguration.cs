using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ScoreBurrow.Data.Entities;

namespace ScoreBurrow.Data.Configurations;

public class GameParticipantConfiguration : IEntityTypeConfiguration<GameParticipant>
{
    public void Configure(EntityTypeBuilder<GameParticipant> builder)
    {
        builder.HasKey(gp => gp.Id);

        builder.Property(gp => gp.PlayerColor)
            .IsRequired();

        builder.Property(gp => gp.CreatedBy)
            .IsRequired()
            .HasMaxLength(450);

        builder.Property(gp => gp.ModifiedBy)
            .HasMaxLength(450);

        builder.HasIndex(gp => gp.GameId);
        builder.HasIndex(gp => gp.LeagueMembershipId);
        builder.HasIndex(gp => new { gp.GameId, gp.PlayerColor }).IsUnique();

        builder.HasOne(gp => gp.Game)
            .WithMany(g => g.Participants)
            .HasForeignKey(gp => gp.GameId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(gp => gp.LeagueMembership)
            .WithMany(m => m.GameParticipants)
            .HasForeignKey(gp => gp.LeagueMembershipId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(gp => gp.Town)
            .WithMany(t => t.GameParticipants)
            .HasForeignKey(gp => gp.TownId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(gp => gp.Hero)
            .WithMany(h => h.GameParticipants)
            .HasForeignKey(gp => gp.HeroId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
