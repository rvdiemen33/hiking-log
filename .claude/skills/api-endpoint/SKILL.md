---
name: api-endpoint
description: >
  This skill should be used when adding the API layer for a HikingLog feature: the controller,
  request/response models, and mapping extensions in HikingLog.Api/<Feature>/. Activate when the user
  asks to "add the RoutesController", "create the API endpoints for Stages", "add request/response
  models and mapping for HikeLogs", or expose handlers over HTTP. Enforces typed handler injection
  (never IMediator) and the project's HTTP status conventions. Do NOT use this for commands (use
  add-command), queries (use add-query), the entity/DbContext (use domain-entity), or DI registration
  (use register-di).
---

# API endpoint — HikingLog

Adds the HTTP surface for a feature: models, mapping, and a controller that injects the custom
`ICommandHandler<T, R>` / `IQueryHandler<T, R>` handlers directly.

*Scope: the API layer only. The command/query handlers must already exist (via add-command /
add-query), and must be registered with register-di; a full feature runs several skills in sequence,
which the slice-builder agent orchestrates.*

## Where things live (so this works without copying an existing slice)

- `HikingLog.Application.Common` — the handler interfaces, `ValidationFailed`, `NotFound`, `Success`.
- `HikingLog.Application.<Feature>.Commands` / `.Queries` — the command/query records and DTOs the
  controller references and injects.
- `HikingLog.Api.Extensions` — `ToModelStateDictionary()` on `ValidationFailed` (added once, below).
- `HikingLog.Domain.Enums` — enums used in request/response models.
- `Microsoft.AspNetCore.Mvc` — `[ApiController]`, `ControllerBase`, results.
- File layout: `namespace X.Y;` first (file-scoped), then `using` directives. Full XML docs.

## Interview mode

This skill needs the inputs under **Required inputs**. Before writing any code, gather them and
**confirm them with the user**: state what you intend to build — feature, which endpoints/verbs, and
the request/response shape — in one short message, ask about anything missing or ambiguous in that same
message, and wait for an explicit go-ahead. Do **not** assume, default, or silently proceed. Even
values that appear in `.claude/functional-plan.md` must be confirmed, not adopted on your own — treat
the functional-plan as a reference that informs your questions (a draft to confirm), never as a
substitute for the user's intent. Generate code only after the user confirms.

If an orchestrating agent invokes this skill, that agent must have gathered and confirmed these inputs
and pass them in — this skill never fabricates or silently defaults a missing input. If the inputs are
missing or ambiguous and no user is reachable, stop and report what's missing; do not invent.

Required inputs:
- **Feature** — which feature (Routes, Stages, HikeLogs)?
- **Actions** — which verbs/endpoints to expose? Confirm them with the user. The endpoint table in
  `.claude/functional-plan.md` may inform your question, but it is only a reference — verify, do not
  adopt its values on your own.
- **Handlers** — the commands/queries must already exist (via add-command / add-query). If they do
  not, scaffold those first with those skills, then return here — do not inline handler logic in the
  controller.

## 1. Request/response models

```csharp
namespace HikingLog.Api.Routes;

/// <summary>Request body for creating a route.</summary>
/// <param name="Name">The full name.</param>
/// <param name="Code">The abbreviation code.</param>
/// <param name="Country">The country or region.</param>
/// <param name="TotalDistanceKm">The total distance in kilometres.</param>
/// <param name="Description">An optional description.</param>
public record CreateRouteRequest(string Name, string Code, string Country, decimal TotalDistanceKm, string? Description);

/// <summary>Request body for updating a route. The id comes from the URL segment.</summary>
/// <param name="Name">The new full name.</param>
/// <param name="Code">The new abbreviation code.</param>
/// <param name="Country">The new country or region.</param>
/// <param name="TotalDistanceKm">The new total distance in kilometres.</param>
/// <param name="Description">The new optional description.</param>
public record UpdateRouteRequest(string Name, string Code, string Country, decimal TotalDistanceKm, string? Description);

/// <summary>Response model for a route.</summary>
/// <param name="Id">The primary key.</param>
/// <param name="Name">The full name.</param>
/// <param name="Code">The abbreviation code.</param>
/// <param name="Country">The country or region.</param>
/// <param name="TotalDistanceKm">The total distance in kilometres.</param>
/// <param name="Description">An optional description.</param>
public record RouteResponse(int Id, string Name, string Code, string Country, decimal TotalDistanceKm, string? Description);
```

`UpdateRouteRequest` omits `Id` — the controller passes the `{id}` route segment to `.ToCommand(id)`.
A model carrying an enum (e.g. a Stage request with `Difficulty`) needs `using HikingLog.Domain.Enums;`.

## 2. Mapping extensions (all `internal static`)

```csharp
namespace HikingLog.Api.Routes;

using HikingLog.Application.Routes.Commands;
using HikingLog.Application.Routes.Queries; // RouteDto

/// <summary>Mapping extensions for route models.</summary>
internal static class RouteMappingExtensions
{
    /// <summary>Maps a <see cref="CreateRouteRequest"/> to an <see cref="AddRoute"/> command.</summary>
    /// <param name="r">The create request.</param>
    /// <returns>The command.</returns>
    internal static AddRoute ToCommand(this CreateRouteRequest r) =>
        new(r.Name, r.Code, r.Country, r.TotalDistanceKm, r.Description);

    /// <summary>Maps an <see cref="UpdateRouteRequest"/> to an <see cref="UpdateRoute"/> command.</summary>
    /// <param name="r">The update request.</param>
    /// <param name="id">The route id from the URL segment.</param>
    /// <returns>The command.</returns>
    internal static UpdateRoute ToCommand(this UpdateRouteRequest r, int id) =>
        new(id, r.Name, r.Code, r.Country, r.TotalDistanceKm, r.Description);

    /// <summary>Maps a <see cref="RouteDto"/> to a <see cref="RouteResponse"/>.</summary>
    /// <param name="dto">The query DTO.</param>
    /// <returns>The response model.</returns>
    internal static RouteResponse ToResponse(this RouteDto dto) =>
        new(dto.Id, dto.Name, dto.Code, dto.Country, dto.TotalDistanceKm, dto.Description);
}
```

Never return entities at the API boundary — always map to response models.

## 3. Controller — single URL prefix (Routes)

Inject handlers directly via the primary constructor — **never IMediator**.

```csharp
namespace HikingLog.Api.Routes;

using HikingLog.Api.Extensions;          // ToModelStateDictionary
using HikingLog.Application.Common;
using HikingLog.Application.Routes.Commands;
using HikingLog.Application.Routes.Queries;
using Microsoft.AspNetCore.Mvc;
using OneOf;

/// <summary>Controller for managing routes.</summary>
[ApiController]
[Route("[controller]")]
public sealed class RoutesController(
    ICommandHandler<AddRoute, OneOf<AddRouteResult, ValidationFailed>> addHandler,
    ICommandHandler<UpdateRoute, OneOf<UpdateRouteResult, ValidationFailed, NotFound>> updateHandler,
    ICommandHandler<DeleteRoute, OneOf<Success, NotFound>> deleteHandler,
    IQueryHandler<GetRoutes, IReadOnlyList<RouteDto>> getRoutesHandler,
    IQueryHandler<GetRoute, OneOf<RouteDto, NotFound>> getRouteHandler) : ControllerBase
{
    /// <summary>Gets all routes.</summary>
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<RouteResponse>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        var result = await getRoutesHandler.Handle(new GetRoutes(), ct);
        return Ok(result.Select(r => r.ToResponse()));
    }

    /// <summary>Gets a route by id.</summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType<RouteResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(int id, CancellationToken ct)
    {
        var result = await getRouteHandler.Handle(new GetRoute(id), ct);
        return result.Match<IActionResult>(dto => Ok(dto.ToResponse()), _ => NotFound());
    }

    /// <summary>Creates a new route.</summary>
    [HttpPost]
    [ProducesResponseType<RouteResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] CreateRouteRequest request, CancellationToken ct)
    {
        var result = await addHandler.Handle(request.ToCommand(), ct);
        return result.Match<IActionResult>(
            r => CreatedAtAction(nameof(GetById), new { id = r.Id },
                new RouteResponse(r.Id, request.Name, request.Code, request.Country, request.TotalDistanceKm, request.Description)),
            v => ValidationProblem(v.ToModelStateDictionary()));
    }

    /// <summary>Updates an existing route.</summary>
    [HttpPut("{id:int}")]
    [ProducesResponseType<RouteResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateRouteRequest request, CancellationToken ct)
    {
        var result = await updateHandler.Handle(request.ToCommand(id), ct);
        return result.Match<IActionResult>(
            r => Ok(new RouteResponse(r.Id, request.Name, request.Code, request.Country, request.TotalDistanceKm, request.Description)),
            v => ValidationProblem(v.ToModelStateDictionary()),
            _ => NotFound());
    }

    /// <summary>Deletes a route.</summary>
    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        var result = await deleteHandler.Handle(new DeleteRoute(id), ct);
        return result.Match<IActionResult>(_ => NoContent(), _ => NotFound());
    }
}
```

**HTTP status rules:** `CreatedAtAction(nameof(GetById), ...)` → 201; `Ok(...)` → 200; `NoContent()`
→ 204; `NotFound()` → 404; `ValidationProblem(v.ToModelStateDictionary())` → 400. Every action carries
`[ProducesResponseType(...)]` for each status it can return. An Add for a **child** entity also returns
404 (parent missing) — add the third `Match` arm and `[ProducesResponseType(404)]`.

**Always map to the response model on the success arm.** Commands return a bare Application result
(e.g. `AddRouteResult`/`UpdateRouteResult` carrying only `Id`). Do **not** return that result directly —
it would leak an Application type onto the HTTP boundary and only expose the id, contradicting the
`[ProducesResponseType<RouteResponse>]` attribute. Build the full `RouteResponse` from the incoming
`request` plus `r.Id`, as the 201/200 arms above do.

This request-plus-id approach is valid only while the handler stores the request verbatim. If it
computes, trims, normalises, or defaults any field, the echoed response would lie — in that case have
the command result carry the full DTO (or re-read via the get handler) and map from that instead.

**Collection GETs never return 404.** A collection handler returns `IReadOnlyList<TDto>`, so an empty
list is the result when there is nothing to return — including a nested collection
(`GET /routes/{routeId}/stages`) whose parent has no children or does not exist. Do not add
`[ProducesResponseType(404)]` to a collection action; there is no `Match` arm that could produce it.

## 3b. Controller — mixed URL prefixes (Stages, HikeLogs)

Some features span two prefixes — e.g. the functional plan exposes Stages at
`GET /routes/{routeId}/stages` (collection, nested) **and** `GET|PUT|DELETE /stages/{id}`,
`POST /stages`. When a controller's actions don't share one prefix, omit the class-level
`[Route("[controller]")]` and give each action an **absolute** route template instead:

```csharp
namespace HikingLog.Api.Stages;

using HikingLog.Api.Extensions;
using HikingLog.Application.Common;
using HikingLog.Application.Stages.Commands;
using HikingLog.Application.Stages.Queries;
using Microsoft.AspNetCore.Mvc;
using OneOf;

/// <summary>Controller for managing stages.</summary>
[ApiController]
public sealed class StagesController(
    IQueryHandler<GetStages, IReadOnlyList<StageDto>> getStagesHandler,
    IQueryHandler<GetStage, OneOf<StageDto, NotFound>> getStageHandler /* + command handlers */) : ControllerBase
{
    /// <summary>Gets all stages for a route.</summary>
    [HttpGet("routes/{routeId:int}/stages")]
    [ProducesResponseType<IReadOnlyList<StageResponse>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(int routeId, CancellationToken ct)
    {
        var result = await getStagesHandler.Handle(new GetStages(routeId), ct);
        return Ok(result.Select(s => s.ToResponse()));
    }

    /// <summary>Gets a stage by id.</summary>
    [HttpGet("stages/{id:int}")]
    [ProducesResponseType<StageResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(int id, CancellationToken ct)
    {
        var result = await getStageHandler.Handle(new GetStage(id), ct);
        return result.Match<IActionResult>(dto => Ok(dto.ToResponse()), _ => NotFound());
    }

    // POST "stages", PUT "stages/{id:int}", DELETE "stages/{id:int}" — same per-action absolute routes.
}
```

## 4. ValidationFailed → ModelState (add once)

`src/HikingLog.Api/Extensions/ValidationFailedExtensions.cs` — add once, reused by all controllers:

```csharp
namespace HikingLog.Api.Extensions;

using HikingLog.Application.Common;
using Microsoft.AspNetCore.Mvc.ModelBinding;

/// <summary>Extension methods for converting <see cref="ValidationFailed"/> to ASP.NET Core types.</summary>
internal static class ValidationFailedExtensions
{
    /// <summary>Converts a <see cref="ValidationFailed"/> to a <see cref="ModelStateDictionary"/>.</summary>
    /// <param name="failed">The validation failure.</param>
    /// <returns>A populated model state dictionary.</returns>
    internal static ModelStateDictionary ToModelStateDictionary(this ValidationFailed failed)
    {
        var modelState = new ModelStateDictionary();
        foreach (var error in failed.Errors)
        {
            modelState.AddModelError(error.PropertyName, error.ErrorMessage);
        }

        return modelState;
    }
}
```

## After scaffolding

- The injected handlers must be registered in DI via **register-di**, or the controller fails to
  resolve at startup.
- Add Tier 0 HTTP contract tests via the **integration-test** skill.
