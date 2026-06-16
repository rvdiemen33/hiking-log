namespace HikingLog.Domain.Enums;

/// <summary>
///     Represents the physical difficulty of a hiking stage.
/// </summary>
public enum Difficulty
{
    /// <summary>Suitable for most walkers; minimal elevation gain.</summary>
    Easy,

    /// <summary>Some challenging terrain or elevation; reasonable fitness required.</summary>
    Moderate,

    /// <summary>Demanding terrain and/or significant elevation; good fitness required.</summary>
    Hard
}
