using HikingLog.Application.Data.Contracts;
using HikingLog.Domain.Entities;
using HikingLog.Infrastructure.Data.Configurations;
using Microsoft.EntityFrameworkCore;

namespace HikingLog.Infrastructure.Data;

/// <summary>Entity Framework Core database context for the HikingLog application.</summary>
public class HikingLogDbContext(DbContextOptions<HikingLogDbContext> options)
    : DbContext(options), IHikingLogDataContext
{
    /// <summary>Gets the set of routes.</summary>
    public DbSet<Route> Routes => Set<Route>();

    /// <summary>Gets the set of stages.</summary>
    public DbSet<Stage> Stages => Set<Stage>();

    /// <summary>Gets the set of hike logs.</summary>
    public DbSet<HikeLog> HikeLogs => Set<HikeLog>();

    /// <inheritdoc/>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new RouteConfiguration());
        modelBuilder.ApplyConfiguration(new StageConfiguration());
        modelBuilder.ApplyConfiguration(new HikeLogConfiguration());
    }
}
