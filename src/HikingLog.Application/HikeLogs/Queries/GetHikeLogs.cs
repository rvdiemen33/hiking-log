namespace HikingLog.Application.HikeLogs.Queries;

using HikingLog.Application.Common;
using HikingLog.Application.Data.Contracts;
using Microsoft.EntityFrameworkCore;

/// <summary>Query to retrieve all hike log entries, optionally filtered by year.</summary>
/// <param name="Year">When provided, restricts results to logs whose hike date falls in this year.</param>
public record GetHikeLogs(int? Year);

/// <summary>Handles the <see cref="GetHikeLogs"/> query by returning a filtered read-only list of hike logs.</summary>
public sealed class GetHikeLogsHandler(IHikingLogDataContext db)
    : IQueryHandler<GetHikeLogs, IReadOnlyList<HikeLogDto>>
{
    /// <inheritdoc/>
    public async Task<IReadOnlyList<HikeLogDto>> Handle(GetHikeLogs query, CancellationToken ct)
        => await db.HikeLogs
            .Where(h => query.Year == null || h.DateHiked.Year == query.Year)
            .Select(h => new HikeLogDto(h.Id, h.StageId, h.DateHiked, h.DurationMinutes, h.Weather, h.Notes, h.Rating))
            .ToListAsync(ct);
}
