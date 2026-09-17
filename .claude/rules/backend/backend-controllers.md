---
paths:
  - "src/HikingLog.Api/**"
---

# Backend controller pattern (Api layer)

`HikingLog.Api` groups everything for a feature in one folder: `Api/<Feature>/` holds the controller
(`<Feature>Controller.cs`), the request/response records (`Models.cs`) and the mapping extensions
(`MappingExtensions.cs`). Cross-cutting helpers live in `Api/Extensions/` — today
`ValidationFailedExtensions.ToModelStateDictionary()`.

## Controller declaration

```csharp
[ApiController]
[Route("[controller]")]                      // omit when the actions span two URL prefixes — see below
public sealed class RoutesController(
    ICommandHandler<AddRoute, OneOf<AddRouteResult, ValidationFailed>> addHandler,
    IQueryHandler<GetRoutes, IReadOnlyList<RouteDto>> getRoutesHandler,
    /* one typed handler per action */) : ControllerBase
```

- **Typed handler injection through the primary constructor**: `ICommandHandler<TCommand, TResult>` /
  `IQueryHandler<TQuery, TResult>` with the exact `OneOf<...>` signature registered in `AddApplication()`.
  Never `IMediator`, never `[FromServices]`, never resolving handlers from `IServiceProvider`.
- Controllers, not minimal APIs. `public sealed class` deriving from `ControllerBase`.
- No authentication or authorization yet (out of scope per `functional-plan.md`): no `[Authorize]`, no policies.
- **Mixed URL prefixes** — Stages (`GET /routes/{routeId}/stages` plus `/stages/{id}`) and HikeLogs
  (`/hikelogs` plus `/stages/{stageId}/hikelogs`): drop the class-level `[Route]` and give every action an
  absolute template (`[HttpGet("routes/{routeId:int}/stages")]`, `[HttpPost("hikelogs")]`, …).
- Every action takes `CancellationToken ct` as its last parameter and forwards it to the handler.
- Every action carries one `[ProducesResponseType]` per status it can actually return; use the generic form for
  bodies (`[ProducesResponseType<RouteResponse>(StatusCodes.Status200OK)]`).
- Actions keep XML doc comments (`<summary>`, `<param>`, `<returns>`) — Swagger includes them.

## API models and mapping

Request/response records are separate from the Application records. **Never return an entity, a command result
or a DTO from an action** — always the mapped response model.

- `Create<Entity>Request`, `Update<Entity>Request` (omits `Id` — it comes from the URL segment) and
  `<Entity>Response`: positional `public record`s in `Api/<Feature>/Models.cs`.
- Mapping is **hand-written static extension methods** in `Api/<Feature>/MappingExtensions.cs`
  (`internal static class <Feature>MappingExtensions`): `request.ToCommand()`, `request.ToCommand(id)`,
  `dto.ToResponse()`. No AutoMapper, no Mapperly, no source-generated mappers.
- POST/PUT success arms build the `<Entity>Response` from the incoming request plus the returned `Id`, because
  the handler stores the request verbatim. If a handler ever computes, trims or normalises a field, make the
  command result carry the full DTO and map from that instead — never echo data the server changed.

## Status code per verb — and the `Match` arm that produces it

| Verb | Codes | Controller code |
|---|---|---|
| GET collection | 200 | `Ok(result.Select(d => d.ToResponse()))` — **never 404**; an empty list is the answer, also for a missing parent |
| GET single | 200 / 404 | `result.Match<IActionResult>(dto => Ok(dto.ToResponse()), _ => NotFound())` |
| POST | 201 / 400 (+ 404 for a child whose parent is missing) | `CreatedAtAction(nameof(GetById), new { id = r.Id }, response)` / `ValidationProblem(v.ToModelStateDictionary())` / `NotFound()` |
| PUT | 200 / 400 / 404 | `Ok(response)` / `ValidationProblem(v.ToModelStateDictionary())` / `NotFound()` |
| DELETE | 204 / 404 | `NoContent()` / `NotFound()` |

The `Match` arms follow the `OneOf` order of the handler signature, so the mapping is exhaustive at compile
time. Validation failures always surface as `400` via `ValidationProblem(...)` (problem+json) — never a
hand-rolled error body. Do not add a `[ProducesResponseType]` for a status no arm can produce.

## Program.cs

Composition only: `AddServiceDefaults()`, Swagger with XML comments, `AddControllers()`, `AddApplication()`,
`AddInfrastructure(configuration)`, `MigrateAsync()` on startup, `DataSeeder.SeedAsync` in Development,
`MapControllers()`. Register services through the per-layer `ServiceCollectionExtensions` — never inline in
`Program.cs`. `Program.cs` uses top-level statements (so it has no namespace and keeps its `using`s at the top)
and ends with the `public partial class Program { }` marker that `WebApplicationFactory` needs.
