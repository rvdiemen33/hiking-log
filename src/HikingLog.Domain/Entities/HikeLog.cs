namespace HikingLog.Domain.Entities;

/// <summary>
///     Represents a log entry for a completed hiking stage.
/// </summary>
public class HikeLog
{
    /// <summary>Gets or sets the primary key.</summary>
    public int Id { get; set; }

    /// <summary>Gets or sets the foreign key to the completed <see cref="Stage"/>.</summary>
    public int StageId { get; set; }

    /// <summary>Gets or sets the date on which the stage was hiked.</summary>
    public DateOnly DateHiked { get; set; }

    /// <summary>Gets or sets the total duration of the hike in minutes.</summary>
    public int DurationMinutes { get; set; }

    /// <summary>Gets or sets a description of the weather conditions during the hike.</summary>
    public string Weather { get; set; } = string.Empty;

    /// <summary>Gets or sets an optional personal note about the hike.</summary>
    public string? Notes { get; set; }

    /// <summary>Gets or sets the hiker's rating for this stage, on a scale from 1 to 5.</summary>
    public int Rating { get; set; }

    /// <summary>Gets the stage that was completed during this hike.</summary>
    public Stage Stage { get; init; } = null!;
}
