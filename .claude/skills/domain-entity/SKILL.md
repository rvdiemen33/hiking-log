---
name: domain-entity
description: >
  This skill should be used when adding or changing a domain ENTITY and its persistence in HikingLog:
  the entity class in HikingLog.Domain, its EF Core Fluent API configuration in HikingLog.Infrastructure,
  and its DbSet on both HikingLogDbContext and IHikingLogDataContext. Activate when the user asks to
  "add a Route entity", "create the HikeLog entity and its DbContext mapping", "add a Fluent
  configuration", "add a DbSet", or change entity properties/relationships. Do NOT use this for
  commands (use add-command), queries (use add-query), the controller/models (use api-endpoint), or DI
  registration of handlers (use register-di). After the entity changes, hand off to the
  dotnet-ef-migration skill to create the migration.
---

# Domain entity + persistence — HikingLog

Covers one entity end-to-end at the persistence level: the Domain class, its Fluent API
configuration, and its `DbSet`. This is the foundation a feature's commands and queries build on.

*Scope: the entity and its persistence only. A full feature also needs commands, queries, an API
endpoint, DI registration, a migration, and tests — run those skills in turn; the phase-2
slice-builder agent orchestrates them.*

## Where things live (so this works without copying an existing slice)

- `HikingLog.Domain.Entities` — entity classes. `HikingLog.Domain.Enums` — enums (e.g. `Difficulty`).
- `HikingLog.Infrastructure.Data` — `HikingLogDbContext`.
- `HikingLog.Infrastructure.Data.Configurations` — the `IEntityTypeConfiguration<T>` classes.
- `HikingLog.Application.Data.Contracts` — `IHikingLogDataContext` (the read-side abstraction handlers
  use; every `DbSet` on the context must also appear here).
- `Microsoft.EntityFrameworkCore` / `Microsoft.EntityFrameworkCore.Metadata.Builders` — `DbSet<T>`,
  `IEntityTypeConfiguration<T>`, `EntityTypeBuilder<T>`.
- File layout: `namespace X.Y;` first (file-scoped), then `using` directives. Full XML docs
  (`<summary>` on every type/member).

## Interview mode

This skill needs the inputs under **Required inputs**. Before writing any code, gather them and
**confirm them with the user**: state what you intend to build — entity name, the exact properties and
types, and the relationships — in one short message, ask about anything missing or ambiguous in that
same message, and wait for an explicit go-ahead. Do **not** assume, default, or silently proceed. Even
values that appear in `.claude/functional-plan.md` must be confirmed, not adopted on your own — treat
the functional-plan as a reference that informs your questions (a draft to confirm), never as a
substitute for the user's intent. Generate code only after the user confirms.

If an orchestrating agent invokes this skill, that agent must have gathered and confirmed these inputs
and pass them in — this skill never fabricates or silently defaults a missing input. If the inputs are
missing or ambiguous and no user is reachable, stop and report what's missing; do not invent.

Required inputs:
- **Entity name** — e.g. Route, Stage, HikeLog.
- **Properties** — names, types, nullability, constraints (max length, decimal precision, enums).
  Confirm the property list with the user. `.claude/functional-plan.md` may inform your question, but
  it is only a reference — verify, do not adopt its values on your own.
- **Relationships** — parent/child navigation (e.g. Route 1→n Stage, Stage 1→n HikeLog). Which side
  owns the foreign key?

## 1. Entity (Domain)

Declare navigation properties on **both** sides of every relationship so Fluent API can configure them.

```csharp
namespace HikingLog.Domain.Entities;

/// <summary>Represents a long-distance hiking trail.</summary>
public class Route
{
    /// <summary>Gets or sets the primary key.</summary>
    public int Id { get; set; }

    /// <summary>Gets or sets the full name of the route.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets the route code (e.g. "LAW 1", "GR5").</summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>Gets or sets the country or region.</summary>
    public string Country { get; set; } = string.Empty;

    /// <summary>Gets or sets the total distance in kilometres.</summary>
    public decimal TotalDistanceKm { get; set; }

    /// <summary>Gets or sets the optional description.</summary>
    public string? Description { get; set; }

    /// <summary>Gets or sets the navigation to stages on this route.</summary>
    public ICollection<Stage> Stages { get; set; } = [];
}
```

A **child entity** carries the FK property, the parent navigation, and (optionally) an enum:

```csharp
namespace HikingLog.Domain.Entities;

using HikingLog.Domain.Enums; // Difficulty

/// <summary>Represents a single day-stage of a route.</summary>
public class Stage
{
    /// <summary>Gets or sets the primary key.</summary>
    public int Id { get; set; }

    /// <summary>Gets or sets the foreign key to the parent route.</summary>
    public int RouteId { get; set; }

    /// <summary>Gets or sets the difficulty level.</summary>
    public Difficulty Difficulty { get; set; }

    // ... Number, Name, StartPoint, EndPoint, DistanceKm, ElevationDifferenceM ...

    /// <summary>Gets or sets the navigation to the parent route.</summary>
    public Route Route { get; set; } = null!;

    /// <summary>Gets or sets the navigation to hike logs for this stage.</summary>
    public ICollection<HikeLog> HikeLogs { get; set; } = [];
}
```

## 2. DbSet on context and interface

Both must be updated — handlers read from `IHikingLogDataContext`; EF Core reads/writes through
`HikingLogDbContext`.

**These are additive edits to existing files.** `HikingLogDbContext`, `IHikingLogDataContext`, and the
`OnModelCreating` body already contain other entities' `DbSet`s and `ApplyConfiguration` calls. Read the
files and **add** your line; do not regenerate them from the snippets here, or you will clobber the other
features' registrations.

In `src/HikingLog.Infrastructure/Data/HikingLogDbContext.cs` (inside the `HikingLogDbContext` class):

```csharp
/// <summary>Gets or sets the routes.</summary>
public DbSet<Route> Routes { get; set; }
```

In `src/HikingLog.Application/Data/Contracts/IHikingLogDataContext.cs` (inside the interface):

```csharp
/// <summary>Gets the routes DbSet.</summary>
DbSet<Route> Routes { get; }
```

## 3. Fluent API configuration

```csharp
namespace HikingLog.Infrastructure.Data.Configurations;

using HikingLog.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

/// <summary>Configures the <see cref="Route"/> entity.</summary>
internal sealed class RouteConfiguration : IEntityTypeConfiguration<Route>
{
    /// <inheritdoc/>
    public void Configure(EntityTypeBuilder<Route> builder)
    {
        builder.HasKey(r => r.Id);
        builder.Property(r => r.Name).IsRequired().HasMaxLength(200);
        builder.Property(r => r.Code).IsRequired().HasMaxLength(20);
        builder.Property(r => r.Country).IsRequired().HasMaxLength(100);
        builder.Property(r => r.TotalDistanceKm).HasPrecision(8, 2);
        builder.Property(r => r.Description).HasMaxLength(2000);

        // Navigation: Route (1) → Stage (n)
        builder.HasMany(r => r.Stages)
               .WithOne(e => e.Route)
               .HasForeignKey(e => e.RouteId)
               .OnDelete(DeleteBehavior.Cascade);
    }
}
```

A **child configuration** owns the FK relationship and converts the enum to a string column:

```csharp
namespace HikingLog.Infrastructure.Data.Configurations;

using HikingLog.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

/// <summary>Configures the <see cref="Stage"/> entity.</summary>
internal sealed class StageConfiguration : IEntityTypeConfiguration<Stage>
{
    /// <inheritdoc/>
    public void Configure(EntityTypeBuilder<Stage> builder)
    {
        builder.HasKey(s => s.Id);
        builder.Property(s => s.Name).IsRequired().HasMaxLength(200);
        builder.Property(s => s.DistanceKm).HasPrecision(8, 2);
        builder.Property(s => s.ElevationDifferenceM).HasPrecision(8, 1);
        builder.Property(s => s.Difficulty).HasConversion<string>().HasMaxLength(20);

        // FK is configured in the CHILD's configuration, not the parent's.
        builder.HasOne(s => s.Route)
               .WithMany(r => r.Stages)
               .HasForeignKey(s => s.RouteId)
               .OnDelete(DeleteBehavior.Cascade);
    }
}
```

Register each configuration in `OnModelCreating` in `HikingLogDbContext`:

```csharp
modelBuilder.ApplyConfiguration(new RouteConfiguration());
```

**Fluent API conventions:**
- `HasMaxLength` on every `string` column.
- `HasPrecision(precision, scale)` for every `decimal` property — e.g. `HasPrecision(8, 2)` for
  distances, `HasPrecision(8, 1)` for elevation.
- `HasConversion<string>()` for enum properties (stored as a string column).
- Navigation properties in both directions; FK declared explicitly.
- Define each FK relationship in the **child entity's** configuration, not the parent's.

## 4. Migration (required final step)

After the changes build, hand off to the **`dotnet-ef-migration` skill** to add and apply the
migration. Do not consider the entity done until `dotnet build` passes and the migration exists.

## After scaffolding

- The entity is now ready for **add-command** / **add-query** to target it.
