namespace HikingLog.Application.HikeLogs.Queries;

using HikingLog.Application.Common;
using HikingLog.Application.Data.Contracts;
using OneOf;

/// <summary>Query to retrieve a single hike log entry by its primary key.</summary>
/// <param name="Id">The primary key of the hike log to retrieve.</param>
public record GetHikeLog(int Id);

/// <summary>DTO for a hike log returned in query results.</summary>
/// <param name="Id">The primary key of the hike log.</param>
/// <param name="StageId">The foreign key of the completed stage.</param>
/// <param name="DateHiked">The date on which the stage was hiked.</param>
/// <param name="DurationMinutes">The total duration of the hike in minutes.</param>
/// <param name="Weather">A description of the weather conditions during the hike.</param>
/// <param name="Notes">An optional personal note about the hike.</param>
/// <param name="Rating">The hiker's rating for this stage, on a scale from 1 to 5.</param>
public record HikeLogDto(int Id, int StageId, DateOnly DateHiked, int DurationMinutes, string Weather, string? Notes, int Rating);

/// <summary>Handles the <see cref="GetHikeLog"/> query, returning the hike log or <see cref="NotFound"/>.</summary>
public sealed class GetHikeLogHandler(IHikingLogDataContext db)
    : IQueryHandler<GetHikeLog, OneOf<HikeLogDto, NotFound>>
{
    /// <inheritdoc/>
    public async Task<OneOf<HikeLogDto, NotFound>> Handle(GetHikeLog query, CancellationToken ct)
    {
        var hikeLog = await db.HikeLogs.FindAsync([query.Id], ct);
        if (hikeLog is null)
        {
            return new NotFound();
        }

        return new HikeLogDto(hikeLog.Id, hikeLog.StageId, hikeLog.DateHiked, hikeLog.DurationMinutes,
            hikeLog.Weather, hikeLog.Notes, hikeLog.Rating);
    }
}
