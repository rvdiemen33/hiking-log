namespace HikingLog.Api.HikeLogs;

/// <summary>Request body for logging a completed hiking stage.</summary>
/// <param name="StageId">The primary key of the completed stage.</param>
/// <param name="DateHiked">The date on which the stage was hiked.</param>
/// <param name="DurationMinutes">The total duration of the hike in minutes.</param>
/// <param name="Weather">A description of the weather conditions during the hike.</param>
/// <param name="Notes">An optional personal note about the hike.</param>
/// <param name="Rating">The hiker's rating for this stage, on a scale from 1 to 5.</param>
public record CreateHikeLogRequest(int StageId, DateOnly DateHiked, int DurationMinutes, string Weather, string? Notes, int Rating);

/// <summary>Request body for updating a hike log. The hike log id is supplied via the URL segment.</summary>
/// <param name="StageId">The primary key of the completed stage.</param>
/// <param name="DateHiked">The new date on which the stage was hiked.</param>
/// <param name="DurationMinutes">The new total duration of the hike in minutes.</param>
/// <param name="Weather">The new description of the weather conditions during the hike.</param>
/// <param name="Notes">The new optional personal note about the hike.</param>
/// <param name="Rating">The new hiker's rating for this stage, on a scale from 1 to 5.</param>
public record UpdateHikeLogRequest(int StageId, DateOnly DateHiked, int DurationMinutes, string Weather, string? Notes, int Rating);

/// <summary>Response model for a hike log entry returned at the API boundary.</summary>
/// <param name="Id">The primary key of the hike log.</param>
/// <param name="StageId">The primary key of the completed stage.</param>
/// <param name="DateHiked">The date on which the stage was hiked.</param>
/// <param name="DurationMinutes">The total duration of the hike in minutes.</param>
/// <param name="Weather">A description of the weather conditions during the hike.</param>
/// <param name="Notes">An optional personal note about the hike.</param>
/// <param name="Rating">The hiker's rating for this stage, on a scale from 1 to 5.</param>
public record HikeLogResponse(int Id, int StageId, DateOnly DateHiked, int DurationMinutes, string Weather, string? Notes, int Rating);
