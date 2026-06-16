using HikingLog.Domain.Entities;
using HikingLog.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HikingLog.Infrastructure.Data.Configurations;

/// <summary>Fluent API configuration for the <see cref="Stage"/> entity.</summary>
internal sealed class StageConfiguration : IEntityTypeConfiguration<Stage>
{
    /// <inheritdoc/>
    public void Configure(EntityTypeBuilder<Stage> builder)
    {
        builder.HasKey(s => s.Id);

        builder.Property(s => s.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(s => s.StartPoint)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(s => s.EndPoint)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(s => s.DistanceKm)
            .HasPrecision(8, 2);

        builder.Property(s => s.ElevationDifferenceM)
            .HasPrecision(8, 1);

        builder.Property(s => s.Difficulty)
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.HasMany(s => s.HikeLogs)
            .WithOne(h => h.Stage)
            .HasForeignKey(h => h.StageId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
