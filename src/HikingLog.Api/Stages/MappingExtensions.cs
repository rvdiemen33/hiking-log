namespace HikingLog.Api.Stages;

using HikingLog.Application.Stages.Commands;
using HikingLog.Application.Stages.Queries;

/// <summary>Mapping extensions for converting between stage API models and Application layer types.</summary>
internal static class StageMappingExtensions
{
    /// <summary>Maps a <see cref="CreateStageRequest"/> to an <see cref="AddStage"/> command.</summary>
    /// <param name="request">The incoming create request.</param>
    /// <returns>The corresponding <see cref="AddStage"/> command.</returns>
    internal static AddStage ToCommand(this CreateStageRequest request) =>
        new(request.RouteId, request.Number, request.Name, request.StartPoint, request.EndPoint,
            request.DistanceKm, request.ElevationDifferenceM, request.Difficulty);

    /// <summary>Maps an <see cref="UpdateStageRequest"/> to an <see cref="UpdateStage"/> command, injecting the stage id from the URL.</summary>
    /// <param name="request">The incoming update request.</param>
    /// <param name="id">The stage id from the URL segment.</param>
    /// <returns>The corresponding <see cref="UpdateStage"/> command.</returns>
    internal static UpdateStage ToCommand(this UpdateStageRequest request, int id) =>
        new(id, request.RouteId, request.Number, request.Name, request.StartPoint, request.EndPoint,
            request.DistanceKm, request.ElevationDifferenceM, request.Difficulty);

    /// <summary>Maps a <see cref="StageDto"/> to a <see cref="StageResponse"/>.</summary>
    /// <param name="dto">The Application layer DTO.</param>
    /// <returns>The corresponding API response model.</returns>
    internal static StageResponse ToResponse(this StageDto dto) =>
        new(dto.Id, dto.RouteId, dto.Number, dto.Name, dto.StartPoint, dto.EndPoint,
            dto.DistanceKm, dto.ElevationDifferenceM, dto.Difficulty);
}
