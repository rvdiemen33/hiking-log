using HikingLog.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HikingLog.Infrastructure.Data.Configurations;

/// <summary>Fluent API configuration for the <see cref="Route"/> entity.</summary>
internal sealed class RouteConfiguration : IEntityTypeConfiguration<Route>
{
    /// <inheritdoc/>
    public void Configure(EntityTypeBuilder<Route> builder)
    {
        builder.HasKey(r => r.Id);

        builder.Property(r => r.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(r => r.Code)
            .IsRequired()
            .HasMaxLength(20);

        builder.Property(r => r.Country)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(r => r.TotalDistanceKm)
            .HasPrecision(8, 2);

        builder.Property(r => r.Description)
            .HasMaxLength(2000);

        builder.HasMany(r => r.Stages)
            .WithOne(s => s.Route)
            .HasForeignKey(s => s.RouteId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
