---
name: add-query
description: >
  This skill should be used when adding a SINGLE CQRS query (read operation) to the HikingLog
  Application layer. Activate when the user asks to add or scaffold a query such as "add a GetRoutes
  query", "create a GetStage query", "scaffold a GetHikeLogs query with a year filter", or any read
  operation (GET behaviour) for a feature. Produces one file containing the record + DTO + handler in
  Application/<Feature>/Queries/. Do NOT use this for commands/write operations (use add-command),
  for the entity/DbContext (use domain-entity), for the controller (use api-endpoint), or for DI
  registration (use register-di).
---

# Add a query — HikingLog

Scaffolds exactly one query in `src/HikingLog.Application/<Feature>/Queries/<QueryName>.cs`.
Each file contains the **record + DTO + handler** together — no separate files per type. Queries
never validate input with FluentValidation — they have no validator.

*Scope: one query only. A full feature also needs the entity, commands, an API endpoint, DI
registration, and tests — run those skills in turn; the slice-builder agent orchestrates them.*

## Where things live (so this works without copying an existing slice)

- `HikingLog.Application.Common` — `IQueryHandler<TQuery, TResult>`, `NotFound`.
- `HikingLog.Application.Data.Contracts` — `IHikingLogDataContext`.
- `HikingLog.Domain.Enums` — enums (e.g. `Difficulty`) used in DTOs.
- `Microsoft.EntityFrameworkCore` — `ToListAsync`, `Where`, etc.
- `OneOf` (NuGet) — `OneOf<...>`.
- File layout: `namespace X.Y;` first (file-scoped), then `using` directives. Full XML docs
  (`<summary>` on every type/member, `<param>` on record parameters).

## Interview mode

This skill needs the inputs under **Required inputs**. Before writing any code, gather them and
**confirm them with the user**: state what you intend to build — entity, query shape, filters, and the
exact DTO fields — in one short message, ask about anything missing or ambiguous in that same message,
and wait for an explicit go-ahead. Do **not** assume, default, or silently proceed. Even values that
appear in `.claude/functional-plan.md` must be confirmed, not adopted on your own — treat the
functional-plan as a reference that informs your questions (a draft to confirm), never as a substitute
for the user's intent. Generate code only after the user confirms.

If an orchestrating agent invokes this skill, that agent must have gathered and confirmed these inputs
and pass them in — this skill never fabricates or silently defaults a missing input. If the inputs are
missing or ambiguous and no user is reachable, stop and report what's missing; do not invent.

Required inputs:
- **Feature/entity** — which feature folder (e.g. Routes, Stages, HikeLogs)?
- **Shape** — single (by id) or collection (all)?
- **Filter** — does the collection query take a filter parameter (e.g. `?year=2024`, `?routeId=`)?
- **DTO fields** — which fields appear in the response DTO? Confirm them with the user.
  `.claude/functional-plan.md` may inform your question, but it is only a reference — verify, do not
  adopt its values on your own.

## OneOf signatures (pick by shape)

- **Get single** (by id): `OneOf<TDto, NotFound>`
- **Get collection**: `IReadOnlyList<TDto>` (never fails — an empty list is a valid result)

## Example — Get collection (GET all)

Complete file. `RouteDto` is declared here and reused by the single-item query in the same feature.

```csharp
namespace HikingLog.Application.Routes.Queries;

using HikingLog.Application.Common;          // IQueryHandler
using HikingLog.Application.Data.Contracts;  // IHikingLogDataContext
using Microsoft.EntityFrameworkCore;         // ToListAsync

/// <summary>Query to retrieve all routes.</summary>
public record GetRoutes;

/// <summary>DTO for a route in a query response.</summary>
/// <param name="Id">The primary key.</param>
/// <param name="Name">The full name.</param>
/// <param name="Code">The abbreviation code.</param>
/// <param name="Country">The country or region.</param>
/// <param name="TotalDistanceKm">The total distance in kilometres.</param>
/// <param name="Description">The optional description.</param>
public record RouteDto(int Id, string Name, string Code, string Country, decimal TotalDistanceKm, string? Description);

/// <summary>Handles the <see cref="GetRoutes"/> query.</summary>
public sealed class GetRoutesHandler(IHikingLogDataContext db)
    : IQueryHandler<GetRoutes, IReadOnlyList<RouteDto>>
{
    /// <inheritdoc/>
    public async Task<IReadOnlyList<RouteDto>> Handle(GetRoutes query, CancellationToken ct)
        => await db.Routes
            .Select(r => new RouteDto(r.Id, r.Name, r.Code, r.Country, r.TotalDistanceKm, r.Description))
            .ToListAsync(ct);
}
```

## Example — Filtered collection (child entity + DateOnly + enum)

`GetHikeLogs` filters by year. This shows a child-entity DTO, a `DateOnly` field, and a conditional
filter applied only when the parameter is supplied.

```csharp
namespace HikingLog.Application.HikeLogs.Queries;

using HikingLog.Application.Common;
using HikingLog.Application.Data.Contracts;
using Microsoft.EntityFrameworkCore;

/// <summary>Query to retrieve hike logs, optionally filtered by year.</summary>
/// <param name="Year">When provided, restricts results to logs whose hike date falls in this year.</param>
public record GetHikeLogs(int? Year);

/// <summary>DTO for a hike log in a query response.</summary>
/// <param name="Id">The primary key.</param>
/// <param name="StageId">The foreign key of the completed stage.</param>
/// <param name="DateHiked">The date the stage was hiked.</param>
/// <param name="DurationMinutes">The duration in minutes.</param>
/// <param name="Weather">The weather conditions.</param>
/// <param name="Notes">An optional personal note.</param>
/// <param name="Rating">The rating from 1 to 5.</param>
public record HikeLogDto(int Id, int StageId, DateOnly DateHiked, int DurationMinutes, string Weather, string? Notes, int Rating);

/// <summary>Handles the <see cref="GetHikeLogs"/> query.</summary>
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
```

A "by parent" collection (e.g. `GetStagesByRoute(int RouteId)`) follows the same shape: carry the
parent id on the record and `.Where(s => s.RouteId == query.RouteId)`. If the entity has a natural
sequence field (e.g. `Stage.Number`), add `.OrderBy(...)` — collection order is otherwise unspecified
and the database may return rows in any order. It does **not** check that the parent exists — a
collection returns `IReadOnlyList<TDto>`, so a missing parent yields an empty list (HTTP 200), never
`NotFound`. If a 404 on a missing parent is genuinely required, use the single-item query
(`OneOf<TDto, NotFound>`) or a command instead.

## Example — Get single (GET by id)

```csharp
namespace HikingLog.Application.Routes.Queries;

using HikingLog.Application.Common;
using HikingLog.Application.Data.Contracts;
using OneOf;

/// <summary>Query to retrieve a single route by id.</summary>
/// <param name="Id">The primary key of the route.</param>
public record GetRoute(int Id);

/// <summary>Handles the <see cref="GetRoute"/> query.</summary>
public sealed class GetRouteHandler(IHikingLogDataContext db)
    : IQueryHandler<GetRoute, OneOf<RouteDto, NotFound>>
{
    /// <inheritdoc/>
    public async Task<OneOf<RouteDto, NotFound>> Handle(GetRoute query, CancellationToken ct)
    {
        var route = await db.Routes.FindAsync([query.Id], ct);
        if (route is null)
        {
            return new NotFound();
        }
        return new RouteDto(route.Id, route.Name, route.Code, route.Country, route.TotalDistanceKm, route.Description);
    }
}
```

Reuse the same `<Entity>Dto` record across a feature's single and collection queries — declare it once
in whichever query file you scaffold first for that feature, and reference it from the others (Routes
declares `RouteDto` in the collection file; Stages declares `StageDto` in the single-item file — either
is fine, just never declare it twice). A DTO carrying an enum (e.g. `StageDto` with `Difficulty`) needs
`using HikingLog.Domain.Enums;`.

## Aggregate / computed queries

Some queries are computed aggregates, not row-to-DTO projections — e.g. `/routes/{id}/progress`
(completed vs total stages, % done, total km/time) and `/statistics` (totals across all routes, most
recent hike date). They still live in `Queries/` as a record + DTO + handler, but the handler computes
values with `Count`/`Sum`/`Max` over joins instead of `Select`-projecting one entity:
- `/routes/{id}/progress` is keyed on a route, so it can miss → `OneOf<ProgressDto, NotFound>` (verify
  the route exists first).
- `/statistics` aggregates everything and always returns one DTO → no `OneOf`, no `NotFound`.

## Unit-test mockability (ToListAsync vs FindAsync)

Collection handlers use EF Core's `ToListAsync`, which requires an `IAsyncQueryProvider` and therefore
**cannot** be exercised against a plain NSubstitute `DbSet<T>` mock (it throws at runtime). Do not write
a behavioural unit test for a collection handler — keep a placeholder unit test that asserts the handler
constructs, and cover the real filtering/ordering/projection with a Tier 0 endpoint test (and/or a Tier 3
handler test) via the **integration-test** skill. Single-item handlers use `FindAsync` (a `DbSet` method),
which IS mockable with NSubstitute — unit-test those normally.

## After scaffolding

- Register the query handler with **register-di**.
- If the entity or DbSet does not yet exist, scaffold it first via **domain-entity**.
