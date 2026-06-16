namespace HikingLog.Application.Common;

/// <summary>Defines the contract for a query handler.</summary>
/// <typeparam name="TQuery">The type of the query.</typeparam>
/// <typeparam name="TResult">The type of the result.</typeparam>
public interface IQueryHandler<TQuery, TResult>
{
    /// <summary>Handles the specified query.</summary>
    /// <param name="query">The query to handle.</param>
    /// <param name="ct">Token to cancel the operation.</param>
    /// <returns>The result of handling the query.</returns>
    Task<TResult> Handle(TQuery query, CancellationToken ct);
}
