namespace HikingLog.Application.Routes.Queries;

using HikingLog.Application.Common;
using HikingLog.Application.Data.Contracts;
using OneOf;

/// <summary>Query to retrieve a single route by its primary key.</summary>
/// <param name="Id">The primary key of the route to retrieve.</param>
public record GetRoute(int Id);

/// <summary>Handles the <see cref="GetRoute"/> query, returning the route or <see cref="NotFound"/>.</summary>
public sealed class GetRouteHandler(IHikingLogDataContext db)
    : IQueryHandler<GetRoute, OneOf<RouteDto, NotFound>>
{
    /// <inheritdoc/>
    public async Task<OneOf<RouteDto, NotFound>> Handle(GetRoute query, CancellationToken ct)
    {
        var route = await db.Routes.FindAsync([query.Id], ct);
        if (route is null)
        {
            return new NotFound();
        }

        return new RouteDto(route.Id, route.Name, route.Code, route.Country, route.TotalDistanceKm, route.Description);
    }
}
