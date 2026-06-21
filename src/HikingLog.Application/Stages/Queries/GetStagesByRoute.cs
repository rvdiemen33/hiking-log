namespace HikingLog.Application.Stages.Queries;

using HikingLog.Application.Common;
using HikingLog.Application.Data.Contracts;
using Microsoft.EntityFrameworkCore;

/// <summary>Query to retrieve all stages belonging to a specific route.</summary>
/// <param name="RouteId">The primary key of the parent route.</param>
public record GetStagesByRoute(int RouteId);

/// <summary>Handles the <see cref="GetStagesByRoute"/> query by returning all stages for the given route as a read-only list.</summary>
public sealed class GetStagesByRouteHandler(IHikingLogDataContext db)
    : IQueryHandler<GetStagesByRoute, IReadOnlyList<StageDto>>
{
    /// <inheritdoc/>
    public async Task<IReadOnlyList<StageDto>> Handle(GetStagesByRoute query, CancellationToken ct)
        => await db.Stages
            .Where(s => s.RouteId == query.RouteId)
            .OrderBy(s => s.Number)
            .Select(s => new StageDto(s.Id, s.RouteId, s.Number, s.Name, s.StartPoint, s.EndPoint,
                s.DistanceKm, s.ElevationDifferenceM, s.Difficulty))
            .ToListAsync(ct);
}
