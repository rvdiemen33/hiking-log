using HikingLog.Domain.Enums;

namespace HikingLog.Domain.Entities;

/// <summary>
///     Represents a single day-stage of a long-distance hiking route.
/// </summary>
public class Stage
{
    /// <summary>Gets or sets the primary key.</summary>
    public int Id { get; set; }

    /// <summary>Gets or sets the foreign key to the parent <see cref="Route"/>.</summary>
    public int RouteId { get; set; }

    /// <summary>Gets or sets the sequence number of this stage within the route.</summary>
    public int Number { get; set; }

    /// <summary>Gets or sets the name of the stage.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets the name of the start location.</summary>
    public string StartPoint { get; set; } = string.Empty;

    /// <summary>Gets or sets the name of the end location.</summary>
    public string EndPoint { get; set; } = string.Empty;

    /// <summary>Gets or sets the length of the stage in kilometres.</summary>
    public decimal DistanceKm { get; set; }

    /// <summary>Gets or sets the elevation difference in metres.</summary>
    public decimal ElevationDifferenceM { get; set; }

    /// <summary>Gets or sets the physical difficulty level of the stage.</summary>
    public Difficulty Difficulty { get; set; }

    /// <summary>Gets the parent route this stage belongs to.</summary>
    public Route Route { get; init; } = null!;

    /// <summary>Gets the collection of hike log entries recorded for this stage.</summary>
    public ICollection<HikeLog> HikeLogs { get; init; } = [];
}
