namespace HikingLog.Application.Stages.Queries;

using HikingLog.Application.Common;
using HikingLog.Application.Data.Contracts;
using HikingLog.Domain.Enums;
using OneOf;

/// <summary>Query to retrieve a single stage by its primary key.</summary>
/// <param name="Id">The primary key of the stage to retrieve.</param>
public record GetStage(int Id);

/// <summary>DTO for a stage returned in query results.</summary>
/// <param name="Id">The primary key of the stage.</param>
/// <param name="RouteId">The foreign key of the parent route.</param>
/// <param name="Number">The sequence number within the route.</param>
/// <param name="Name">The stage name.</param>
/// <param name="StartPoint">The name of the start location.</param>
/// <param name="EndPoint">The name of the end location.</param>
/// <param name="DistanceKm">The length in kilometres.</param>
/// <param name="ElevationDifferenceM">The elevation difference in metres.</param>
/// <param name="Difficulty">The difficulty level.</param>
public record StageDto(int Id, int RouteId, int Number, string Name, string StartPoint, string EndPoint,
    decimal DistanceKm, decimal ElevationDifferenceM, Difficulty Difficulty);

/// <summary>Handles the <see cref="GetStage"/> query, returning the stage or <see cref="NotFound"/>.</summary>
public sealed class GetStageHandler(IHikingLogDataContext db)
    : IQueryHandler<GetStage, OneOf<StageDto, NotFound>>
{
    /// <inheritdoc/>
    public async Task<OneOf<StageDto, NotFound>> Handle(GetStage query, CancellationToken ct)
    {
        var stage = await db.Stages.FindAsync([query.Id], ct);
        if (stage is null)
        {
            return new NotFound();
        }

        return new StageDto(stage.Id, stage.RouteId, stage.Number, stage.Name, stage.StartPoint,
            stage.EndPoint, stage.DistanceKm, stage.ElevationDifferenceM, stage.Difficulty);
    }
}
