namespace HikingLog.Application.Data.Contracts;

/// <summary>Defines the data access contract for the HikingLog database context.</summary>
public interface IHikingLogDataContext
{
    /// <summary>Saves all pending changes to the database.</summary>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>The number of state entries written to the database.</returns>
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
