using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ScoreBurrow.Data.Entities;

namespace ScoreBurrow.Data.Configurations;

public class RatingHistoryConfiguration : IEntityTypeConfiguration<RatingHistory>
{
    public void Configure(EntityTypeBuilder<RatingHistory> builder)
    {
        builder.ToTable("RatingHistory");

        builder.HasKey(rh => rh.Id);

        builder.Property(rh => rh.LeagueMembershipId)
            .IsRequired();

        builder.Property(rh => rh.GameId)
            .IsRequired();

        builder.Property(rh => rh.CalculatedAt)
            .IsRequired();

        builder.Property(rh => rh.PreviousRating)
            .IsRequired()
            .HasColumnType("decimal(18,2)");

        builder.Property(rh => rh.PreviousRatingDeviation)
            .IsRequired()
            .HasColumnType("decimal(18,2)");

        builder.Property(rh => rh.PreviousVolatility)
            .IsRequired()
            .HasColumnType("decimal(18,6)");

        builder.Property(rh => rh.NewRating)
            .IsRequired()
            .HasColumnType("decimal(18,2)");

        builder.Property(rh => rh.NewRatingDeviation)
            .IsRequired()
            .HasColumnType("decimal(18,2)");

        builder.Property(rh => rh.NewVolatility)
            .IsRequired()
            .HasColumnType("decimal(18,6)");

        // Audit fields
        builder.Property(rh => rh.CreatedBy)
            .IsRequired()
            .HasMaxLength(256);

        builder.Property(rh => rh.CreatedOn)
            .IsRequired();

        builder.Property(rh => rh.ModifiedBy)
            .HasMaxLength(256);

        // Relationships
        builder.HasOne(rh => rh.LeagueMembership)
            .WithMany()
            .HasForeignKey(rh => rh.LeagueMembershipId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(rh => rh.Game)
            .WithMany()
            .HasForeignKey(rh => rh.GameId)
            .OnDelete(DeleteBehavior.Cascade);

        // Indexes
        builder.HasIndex(rh => rh.LeagueMembershipId)
            .HasDatabaseName("IX_RatingHistory_LeagueMembershipId");

        builder.HasIndex(rh => rh.GameId)
            .HasDatabaseName("IX_RatingHistory_GameId");

        builder.HasIndex(rh => rh.CalculatedAt)
            .HasDatabaseName("IX_RatingHistory_CalculatedAt");
    }
}
