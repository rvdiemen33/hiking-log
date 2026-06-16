namespace HikingLog.Api.Routes;

/// <summary>Request body for creating a route.</summary>
/// <param name="Name">The full name of the route.</param>
/// <param name="Code">The abbreviation code (e.g. "LAW 1", "GR5").</param>
/// <param name="Country">The country or region.</param>
/// <param name="TotalDistanceKm">The total distance in kilometres.</param>
/// <param name="Description">An optional description of the route.</param>
public record CreateRouteRequest(string Name, string Code, string Country, decimal TotalDistanceKm, string? Description);

/// <summary>Request body for updating a route. The route id is supplied via the URL segment.</summary>
/// <param name="Name">The new full name of the route.</param>
/// <param name="Code">The new abbreviation code.</param>
/// <param name="Country">The new country or region.</param>
/// <param name="TotalDistanceKm">The new total distance in kilometres.</param>
/// <param name="Description">The new optional description.</param>
public record UpdateRouteRequest(string Name, string Code, string Country, decimal TotalDistanceKm, string? Description);

/// <summary>Response model for a route returned at the API boundary.</summary>
/// <param name="Id">The primary key of the route.</param>
/// <param name="Name">The full name of the route.</param>
/// <param name="Code">The abbreviation code (e.g. "LAW 1", "GR5").</param>
/// <param name="Country">The country or region.</param>
/// <param name="TotalDistanceKm">The total distance in kilometres.</param>
/// <param name="Description">An optional description of the route.</param>
public record RouteResponse(int Id, string Name, string Code, string Country, decimal TotalDistanceKm, string? Description);
