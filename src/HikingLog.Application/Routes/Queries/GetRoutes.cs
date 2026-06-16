namespace HikingLog.Application.Routes.Queries;

using HikingLog.Application.Common;
using HikingLog.Application.Data.Contracts;
using Microsoft.EntityFrameworkCore;

/// <summary>Query to retrieve all routes.</summary>
public record GetRoutes;

/// <summary>DTO for a route returned in query results.</summary>
/// <param name="Id">The primary key of the route.</param>
/// <param name="Name">The full name of the route.</param>
/// <param name="Code">The abbreviation code (e.g. "LAW 1", "GR5").</param>
/// <param name="Country">The country or region.</param>
/// <param name="TotalDistanceKm">The total distance in kilometres.</param>
/// <param name="Description">An optional description of the route.</param>
public record RouteDto(int Id, string Name, string Code, string Country, decimal TotalDistanceKm, string? Description);

/// <summary>Handles the <see cref="GetRoutes"/> query by returning all routes as a read-only list.</summary>
public sealed class GetRoutesHandler(IHikingLogDataContext db)
    : IQueryHandler<GetRoutes, IReadOnlyList<RouteDto>>
{
    /// <inheritdoc/>
    public async Task<IReadOnlyList<RouteDto>> Handle(GetRoutes query, CancellationToken ct)
        => await db.Routes
            .Select(r => new RouteDto(r.Id, r.Name, r.Code, r.Country, r.TotalDistanceKm, r.Description))
            .ToListAsync(ct);
}
