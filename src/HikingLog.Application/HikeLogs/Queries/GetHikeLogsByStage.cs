namespace HikingLog.Application.HikeLogs.Queries;

using HikingLog.Application.Common;
using HikingLog.Application.Data.Contracts;
using Microsoft.EntityFrameworkCore;

/// <summary>Query to retrieve all hike log entries for a specific stage.</summary>
/// <param name="StageId">The primary key of the parent stage.</param>
public record GetHikeLogsByStage(int StageId);

/// <summary>Handles the <see cref="GetHikeLogsByStage"/> query by returning all hike logs for the given stage as a read-only list.</summary>
public sealed class GetHikeLogsByStageHandler(IHikingLogDataContext db)
    : IQueryHandler<GetHikeLogsByStage, IReadOnlyList<HikeLogDto>>
{
    /// <inheritdoc/>
    public async Task<IReadOnlyList<HikeLogDto>> Handle(GetHikeLogsByStage query, CancellationToken ct)
        => await db.HikeLogs
            .Where(h => h.StageId == query.StageId)
            .Select(h => new HikeLogDto(h.Id, h.StageId, h.DateHiked, h.DurationMinutes, h.Weather, h.Notes, h.Rating))
            .ToListAsync(ct);
}
