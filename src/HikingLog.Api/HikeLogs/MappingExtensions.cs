namespace HikingLog.Api.HikeLogs;

using HikingLog.Application.HikeLogs.Commands;
using HikingLog.Application.HikeLogs.Queries;

/// <summary>Mapping extensions for converting between hike log API models and Application layer types.</summary>
internal static class HikeLogMappingExtensions
{
    /// <summary>Maps a <see cref="CreateHikeLogRequest"/> to an <see cref="AddHikeLog"/> command.</summary>
    /// <param name="request">The incoming create request.</param>
    /// <returns>The corresponding <see cref="AddHikeLog"/> command.</returns>
    internal static AddHikeLog ToCommand(this CreateHikeLogRequest request) =>
        new(request.StageId, request.DateHiked, request.DurationMinutes, request.Weather, request.Notes, request.Rating);

    /// <summary>Maps an <see cref="UpdateHikeLogRequest"/> to an <see cref="UpdateHikeLog"/> command, injecting the hike log id from the URL.</summary>
    /// <param name="request">The incoming update request.</param>
    /// <param name="id">The hike log id from the URL segment.</param>
    /// <returns>The corresponding <see cref="UpdateHikeLog"/> command.</returns>
    internal static UpdateHikeLog ToCommand(this UpdateHikeLogRequest request, int id) =>
        new(id, request.StageId, request.DateHiked, request.DurationMinutes, request.Weather, request.Notes, request.Rating);

    /// <summary>Maps a <see cref="HikeLogDto"/> to a <see cref="HikeLogResponse"/>.</summary>
    /// <param name="dto">The Application layer DTO.</param>
    /// <returns>The corresponding API response model.</returns>
    internal static HikeLogResponse ToResponse(this HikeLogDto dto) =>
        new(dto.Id, dto.StageId, dto.DateHiked, dto.DurationMinutes, dto.Weather, dto.Notes, dto.Rating);
}
