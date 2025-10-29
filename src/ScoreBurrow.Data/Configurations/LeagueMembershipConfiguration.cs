using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ScoreBurrow.Data.Entities;

namespace ScoreBurrow.Data.Configurations;

public class LeagueMembershipConfiguration : IEntityTypeConfiguration<LeagueMembership>
{
    public void Configure(EntityTypeBuilder<LeagueMembership> builder)
    {
        builder.HasKey(m => m.Id);

        builder.Property(m => m.UserId)
            .HasMaxLength(450);

        builder.Property(m => m.PlayerNickname)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(m => m.PlayerDisplayName)
            .HasMaxLength(200);

        builder.Property(m => m.Role)
            .IsRequired();

        builder.Property(m => m.CreatedBy)
            .IsRequired()
            .HasMaxLength(450);

        builder.Property(m => m.ModifiedBy)
            .HasMaxLength(450);

        builder.Ignore(m => m.IsRegistered);

        builder.HasIndex(m => m.LeagueId);
        builder.HasIndex(m => m.UserId);
        builder.HasIndex(m => new { m.LeagueId, m.PlayerNickname }).IsUnique();

        builder.HasOne(m => m.League)
            .WithMany(l => l.Memberships)
            .HasForeignKey(m => m.LeagueId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(m => m.GameParticipants)
            .WithOne(gp => gp.LeagueMembership)
            .HasForeignKey(gp => gp.LeagueMembershipId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(m => m.GamesWon)
            .WithOne(g => g.Winner)
            .HasForeignKey(g => g.WinnerId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(m => m.Statistics)
            .WithOne(s => s.LeagueMembership)
            .HasForeignKey<PlayerStatistics>(s => s.LeagueMembershipId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
