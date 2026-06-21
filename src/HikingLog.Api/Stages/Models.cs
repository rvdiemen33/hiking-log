namespace HikingLog.Api.Stages;

using HikingLog.Domain.Enums;

/// <summary>Request body for creating a stage.</summary>
/// <param name="RouteId">The primary key of the parent route.</param>
/// <param name="Number">The sequence number within the route.</param>
/// <param name="Name">The stage name.</param>
/// <param name="StartPoint">The start location name.</param>
/// <param name="EndPoint">The end location name.</param>
/// <param name="DistanceKm">The length in kilometres.</param>
/// <param name="ElevationDifferenceM">The elevation difference in metres.</param>
/// <param name="Difficulty">The difficulty level.</param>
public record CreateStageRequest(int RouteId, int Number, string Name, string StartPoint, string EndPoint,
    decimal DistanceKm, decimal ElevationDifferenceM, Difficulty Difficulty);

/// <summary>Request body for updating a stage. The stage id is supplied via the URL segment.</summary>
/// <param name="RouteId">The primary key of the parent route.</param>
/// <param name="Number">The new sequence number within the route.</param>
/// <param name="Name">The new stage name.</param>
/// <param name="StartPoint">The new start location name.</param>
/// <param name="EndPoint">The new end location name.</param>
/// <param name="DistanceKm">The new length in kilometres.</param>
/// <param name="ElevationDifferenceM">The new elevation difference in metres.</param>
/// <param name="Difficulty">The new difficulty level.</param>
public record UpdateStageRequest(int RouteId, int Number, string Name, string StartPoint, string EndPoint,
    decimal DistanceKm, decimal ElevationDifferenceM, Difficulty Difficulty);

/// <summary>Response model for a stage returned at the API boundary.</summary>
/// <param name="Id">The primary key of the stage.</param>
/// <param name="RouteId">The primary key of the parent route.</param>
/// <param name="Number">The sequence number within the route.</param>
/// <param name="Name">The stage name.</param>
/// <param name="StartPoint">The start location name.</param>
/// <param name="EndPoint">The end location name.</param>
/// <param name="DistanceKm">The length in kilometres.</param>
/// <param name="ElevationDifferenceM">The elevation difference in metres.</param>
/// <param name="Difficulty">The difficulty level.</param>
public record StageResponse(int Id, int RouteId, int Number, string Name, string StartPoint, string EndPoint,
    decimal DistanceKm, decimal ElevationDifferenceM, Difficulty Difficulty);
