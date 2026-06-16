namespace HikingLog.Application.Data.Contracts;

using HikingLog.Domain.Entities;
using Microsoft.EntityFrameworkCore;

/// <summary>Defines the data access contract for the HikingLog database context.</summary>
public interface IHikingLogDataContext
{
    /// <summary>Gets the set of routes.</summary>
    DbSet<Route> Routes { get; }

    /// <summary>Gets the set of stages.</summary>
    DbSet<Stage> Stages { get; }

    /// <summary>Gets the set of hike logs.</summary>
    DbSet<HikeLog> HikeLogs { get; }

    /// <summary>Saves all pending changes to the database.</summary>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>The number of state entries written to the database.</returns>
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
