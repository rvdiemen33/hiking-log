namespace HikingLog.Infrastructure.Data.Configurations;

using HikingLog.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

/// <summary>Fluent API configuration for the <see cref="HikeLog"/> entity.</summary>
internal sealed class HikeLogConfiguration : IEntityTypeConfiguration<HikeLog>
{
    /// <inheritdoc/>
    public void Configure(EntityTypeBuilder<HikeLog> builder)
    {
        builder.HasKey(h => h.Id);

        builder.Property(h => h.Weather)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(h => h.Notes)
            .HasMaxLength(2000);
    }
}
