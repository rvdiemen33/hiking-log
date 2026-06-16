namespace HikingLog.Domain.Entities;

/// <summary>
///     Represents a long-distance hiking trail (e.g. LAW 1, Pieterpad, GR5).
/// </summary>
public class Route
{
    /// <summary>Gets or sets the primary key.</summary>
    public int Id { get; set; }

    /// <summary>Gets or sets the full name of the route.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets the abbreviation code (e.g. "LAW 1", "GR5").</summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>Gets or sets the country or region where the route is located.</summary>
    public string Country { get; set; } = string.Empty;

    /// <summary>Gets or sets the total length of the route in kilometres.</summary>
    public decimal TotalDistanceKm { get; set; }

    /// <summary>Gets or sets an optional description of the route.</summary>
    public string? Description { get; set; }

    /// <summary>Gets the collection of stages that make up this route.</summary>
    public ICollection<Stage> Stages { get; init; } = [];
}
