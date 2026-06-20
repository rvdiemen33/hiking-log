---
name: register-di
description: >
  This skill should be used when registering CQRS handlers and FluentValidation validators in the
  HikingLog dependency-injection container. Activate when the user asks to "register the handlers in
  DI", "wire up the Stage handlers", "add the HikeLog registrations to AddApplication()", or when new
  commands/queries exist but are not yet resolvable at startup. Edits
  Application/Extensions/ServiceCollectionExtensions.cs with full generic signatures. Do NOT use this
  for writing commands (use add-command), queries (use add-query), the entity (use domain-entity), or
  the controller (use api-endpoint).
---

# DI registration — HikingLog

Registers handlers and validators manually (no assembly scanning) in
`src/HikingLog.Application/Extensions/ServiceCollectionExtensions.cs`. The full generic signature of
each handler must match exactly what the controller injects, or resolution fails at startup.

*Scope: DI registration only. A full feature also needs the entity, commands, queries, an API
endpoint, a migration, and tests — run those skills in turn; the slice-builder agent
orchestrates them.*

## Where things live (so this works without copying an existing slice)

- `HikingLog.Application.Common` — `ICommandHandler<,>`, `IQueryHandler<,>`, `ValidationFailed`,
  `NotFound`, `Success`.
- `HikingLog.Application.<Feature>.Commands` / `.Queries` — the records, results, handlers, validators.
- `FluentValidation` — `IValidator<T>`. `Microsoft.Extensions.DependencyInjection` — `IServiceCollection`,
  `AddScoped`. `OneOf` — `OneOf<...>`.
- File layout: `namespace X.Y;` first (file-scoped), then `using` directives. Full XML docs.

## Interview mode

This skill registers handlers/validators that already exist. The exact generic signature of each
handler is a **fact in the scaffolded command/query files** — read those files and derive it; do not
ask the user to restate signatures, and never guess one. What you *do* confirm with the user is the
**set of operations** to register (and that those handlers actually exist). Ask if the set is unclear;
never register an operation whose handler you have not seen.

If an orchestrating agent invokes this skill, it supplies the operation set; if that set is missing or
ambiguous and no user is reachable, stop and report — never fabricate a registration.

Required inputs:
- **Feature** — which feature's handlers to register (Routes, Stages, HikeLogs)?
- **Operations** — which commands/queries exist for it? Each must already be scaffolded (via
  add-command / add-query) so its exact `OneOf<...>` signature is known.

## Signature rules

- **Add** (top-level): `ICommandHandler<AddX, OneOf<AddXResult, ValidationFailed>>`
- **Add** (child with parent FK): `ICommandHandler<AddX, OneOf<AddXResult, ValidationFailed, NotFound>>`
- **Update**: `ICommandHandler<UpdateX, OneOf<UpdateXResult, ValidationFailed, NotFound>>`
- **Delete**: `ICommandHandler<DeleteX, OneOf<Success, NotFound>>` — no validator
- **Get single**: `IQueryHandler<GetX, OneOf<XDto, NotFound>>`
- **Get collection**: `IQueryHandler<GetXs, IReadOnlyList<XDto>>`
- A validator is registered for every command that has one (Add and Update); Delete has none.
- Always `AddScoped` — never `AddTransient` or `AddSingleton`.

## Registration

> **Register only handlers and validators that actually exist.** The example below registers one
> feature (Routes). Add lines **only** for the operations you are wiring up — do not reproduce a
> feature you are not adding, or you will reference types that do not exist and break the build. When
> `AddApplication()` already exists, add your lines **into** it — do not create a second method or
> duplicate the class.

```csharp
namespace HikingLog.Application.Extensions;

using FluentValidation;
using HikingLog.Application.Common;
using HikingLog.Application.Routes.Commands;
using HikingLog.Application.Routes.Queries;
using Microsoft.Extensions.DependencyInjection;
using OneOf;

/// <summary>Registers Application layer services.</summary>
public static class ServiceCollectionExtensions
{
    /// <summary>Adds Application handlers and validators to the DI container.</summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The same service collection, for chaining.</returns>
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        // Routes — handlers (top-level: Add has no NotFound)
        services.AddScoped<ICommandHandler<AddRoute, OneOf<AddRouteResult, ValidationFailed>>, AddRouteHandler>();
        services.AddScoped<ICommandHandler<UpdateRoute, OneOf<UpdateRouteResult, ValidationFailed, NotFound>>, UpdateRouteHandler>();
        services.AddScoped<ICommandHandler<DeleteRoute, OneOf<Success, NotFound>>, DeleteRouteHandler>();
        services.AddScoped<IQueryHandler<GetRoutes, IReadOnlyList<RouteDto>>, GetRoutesHandler>();
        services.AddScoped<IQueryHandler<GetRoute, OneOf<RouteDto, NotFound>>, GetRouteHandler>();

        // Routes — validators
        services.AddScoped<IValidator<AddRoute>, AddRouteValidator>();
        services.AddScoped<IValidator<UpdateRoute>, UpdateRouteValidator>();

        return services;
    }
}
```

**Child feature** — when a feature's Add checks a parent FK, its Add handler carries `NotFound`. Add a
labelled block (only if these handlers exist), e.g. for Stages:

```csharp
// Stages — handlers (child of Route: AddStage carries NotFound)
services.AddScoped<ICommandHandler<AddStage, OneOf<AddStageResult, ValidationFailed, NotFound>>, AddStageHandler>();
// ... Update/Delete/GetStages/GetStage as above ...

// Stages — validators
services.AddScoped<IValidator<AddStage>, AddStageValidator>();
services.AddScoped<IValidator<UpdateStage>, UpdateStageValidator>();
```

## Program.cs wiring (verify once)

`AddApplication()` must be called in `src/HikingLog.Api/Program.cs`, alongside `AddInfrastructure`:

```csharp
builder.Services.AddApplication();
```

If already present, leave it; only the per-feature handler lines inside `AddApplication()` change as
features are added.

## After scaffolding

- Build to confirm DI resolves: a signature typo usually fails the build; a missing registration
  surfaces as a runtime resolution error when the controller is hit.
