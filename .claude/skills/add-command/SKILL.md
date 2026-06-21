---
name: add-command
description: >
  This skill should be used when adding a SINGLE CQRS command (create, update, or delete) to the
  HikingLog Application layer. Activate when the user asks to add or scaffold a command such as
  "add an AddStage command", "create an UpdateRoute command", "scaffold a DeleteHikeLog command",
  or any write operation (POST/PUT/DELETE behaviour) for a feature. Produces one file containing
  the record + FluentValidation validator + handler in Application/<Feature>/Commands/. Do NOT use
  this for queries (use add-query), for the entity/DbContext (use domain-entity), for the controller
  (use api-endpoint), or for DI registration (use register-di).
---

# Add a command — HikingLog

Scaffolds exactly one command in `src/HikingLog.Application/<Feature>/Commands/<CommandName>.cs`.
Each file contains the **record + validator + handler** together — no separate files per type.

*Scope: one command only. A full feature also needs the entity, queries, an API endpoint, DI
registration, a migration, and tests — run those skills in turn; the slice-builder agent
orchestrates them.*

## Where things live (so this works without copying an existing slice)

- `HikingLog.Application.Common` — `ICommandHandler<TCommand, TResult>`, `IQueryHandler<,>`,
  `ValidationFailed`, `NotFound`, `Success`.
- `HikingLog.Application.Data.Contracts` — `IHikingLogDataContext` (handlers inject this directly; no
  repository pattern).
- `HikingLog.Domain.Entities` — entities. `HikingLog.Domain.Enums` — enums (e.g. `Difficulty`).
- `OneOf` (NuGet) — `OneOf<...>` only. `ValidationFailed`, `NotFound`, and `Success` are
  project-defined types in `HikingLog.Application.Common`, not from the OneOf package.
- File layout: `namespace X.Y;` first (file-scoped), then `using` directives. Full XML docs
  (`<summary>` on every type/member, `<param>` on record parameters).

## Interview mode

This skill needs the inputs under **Required inputs**. Before writing any code, gather them and
**confirm them with the user**: state what you intend to build — entity, operation, and the exact
fields and rules — in one short message, ask about anything missing or ambiguous in that same message,
and wait for an explicit go-ahead. Do **not** assume, default, or silently proceed. Even values that
appear in `.claude/functional-plan.md` must be confirmed, not adopted on your own — treat the
functional-plan as a reference that informs your questions (a draft to confirm), never as a substitute
for the user's intent. Generate code only after the user confirms.

If an orchestrating agent invokes this skill, that agent must have gathered and confirmed these inputs
and pass them in — this skill never fabricates or silently defaults a missing input. If the inputs are
missing or ambiguous and no user is reachable, stop and report what's missing; do not invent.

Required inputs:
- **Feature/entity** — which feature folder (e.g. Routes, Stages, HikeLogs)?
- **Operation** — Add, Update, or Delete?
- **Parent FK** — for an Add: is this a child entity whose parent must exist (e.g. AddStage checks
  RouteId, AddHikeLog checks StageId)? If yes, the signature must include `NotFound`.
- **Fields + rules** — which properties does the command carry, and their validation rules (max
  length, ranges, required)? Confirm the exact properties and rules with the user (e.g. `Rating`
  must be `InclusiveBetween(1, 5)`). `.claude/functional-plan.md` may inform your question, but it is
  only a reference — verify, do not adopt its values on your own.

## OneOf signatures (pick by operation)

- **Add** (top-level entity): `OneOf<TResult, ValidationFailed>`
- **Add** (child with parent FK check): `OneOf<TResult, ValidationFailed, NotFound>`
- **Update**: `OneOf<TResult, ValidationFailed, NotFound>`
- **Delete**: `OneOf<Success, NotFound>` — no validator (existence is checked in the handler)

Never use exceptions for these expected error paths.

## Example — Add, top-level entity (POST)

Complete file. Note the header: namespace first, then usings naming where each type comes from.

```csharp
namespace HikingLog.Application.Routes.Commands;

using FluentValidation;
using HikingLog.Application.Common;          // ICommandHandler, ValidationFailed
using HikingLog.Application.Data.Contracts;  // IHikingLogDataContext
using HikingLog.Domain.Entities;             // Route
using OneOf;

/// <summary>Command to create a new route.</summary>
/// <param name="Name">The full name of the route.</param>
/// <param name="Code">The abbreviation code (e.g. "LAW 1", "GR5").</param>
/// <param name="Country">The country or region.</param>
/// <param name="TotalDistanceKm">The total distance in kilometres.</param>
/// <param name="Description">An optional description.</param>
public record AddRoute(string Name, string Code, string Country, decimal TotalDistanceKm, string? Description);

/// <summary>Result returned after a route is created.</summary>
/// <param name="Id">The primary key of the new route.</param>
public record AddRouteResult(int Id);

/// <summary>Validates the <see cref="AddRoute"/> command.</summary>
internal sealed class AddRouteValidator : AbstractValidator<AddRoute>
{
    /// <summary>Initializes a new instance of <see cref="AddRouteValidator"/>.</summary>
    public AddRouteValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Code).NotEmpty().MaximumLength(20);
        RuleFor(x => x.Country).NotEmpty().MaximumLength(100);
        RuleFor(x => x.TotalDistanceKm).GreaterThan(0);
    }
}

/// <summary>Handles the <see cref="AddRoute"/> command.</summary>
public sealed class AddRouteHandler(IHikingLogDataContext db, IValidator<AddRoute> validator)
    : ICommandHandler<AddRoute, OneOf<AddRouteResult, ValidationFailed>>
{
    /// <inheritdoc/>
    public async Task<OneOf<AddRouteResult, ValidationFailed>> Handle(AddRoute command, CancellationToken ct)
    {
        var validation = await validator.ValidateAsync(command, ct);
        if (!validation.IsValid)
        {
            return new ValidationFailed(validation.Errors);
        }

        var route = new Route
        {
            Name = command.Name,
            Code = command.Code,
            Country = command.Country,
            TotalDistanceKm = command.TotalDistanceKm,
            Description = command.Description
        };
        db.Routes.Add(route);
        await db.SaveChangesAsync(ct);
        return new AddRouteResult(route.Id);
    }
}
```

## Example — Add, child entity with a parent FK + an enum field (POST)

`AddStage` belongs to a `Route`, so the parent must exist (→ `NotFound`), and it carries the
`Difficulty` enum. This is the pattern to follow for any child entity (AddHikeLog checks StageId, etc.).

```csharp
namespace HikingLog.Application.Stages.Commands;

using FluentValidation;
using HikingLog.Application.Common;
using HikingLog.Application.Data.Contracts;
using HikingLog.Domain.Entities;             // Stage
using HikingLog.Domain.Enums;                // Difficulty
using OneOf;

/// <summary>Command to create a new stage on an existing route.</summary>
/// <param name="RouteId">The primary key of the parent route.</param>
/// <param name="Number">The sequence number within the route.</param>
/// <param name="Name">The stage name.</param>
/// <param name="StartPoint">The start location name.</param>
/// <param name="EndPoint">The end location name.</param>
/// <param name="DistanceKm">The length in kilometres.</param>
/// <param name="ElevationDifferenceM">The elevation difference in metres.</param>
/// <param name="Difficulty">The difficulty level.</param>
public record AddStage(int RouteId, int Number, string Name, string StartPoint, string EndPoint,
    decimal DistanceKm, decimal ElevationDifferenceM, Difficulty Difficulty);

/// <summary>Result returned after a stage is successfully created.</summary>
/// <param name="Id">The primary key of the newly created stage.</param>
public record AddStageResult(int Id);

/// <summary>Validates the <see cref="AddStage"/> command.</summary>
internal sealed class AddStageValidator : AbstractValidator<AddStage>
{
    /// <summary>Initializes a new instance of <see cref="AddStageValidator"/> with all rules.</summary>
    public AddStageValidator()
    {
        RuleFor(x => x.RouteId).GreaterThan(0);
        RuleFor(x => x.Number).GreaterThan(0);
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.StartPoint).NotEmpty().MaximumLength(200);
        RuleFor(x => x.EndPoint).NotEmpty().MaximumLength(200);
        RuleFor(x => x.DistanceKm).GreaterThan(0);
        RuleFor(x => x.ElevationDifferenceM).GreaterThanOrEqualTo(0);
        RuleFor(x => x.Difficulty).IsInEnum();
    }
}

/// <summary>Handles the <see cref="AddStage"/> command by persisting a new stage entity.</summary>
public sealed class AddStageHandler(IHikingLogDataContext db, IValidator<AddStage> validator)
    : ICommandHandler<AddStage, OneOf<AddStageResult, ValidationFailed, NotFound>>
{
    /// <inheritdoc/>
    public async Task<OneOf<AddStageResult, ValidationFailed, NotFound>> Handle(AddStage command, CancellationToken ct)
    {
        var validation = await validator.ValidateAsync(command, ct);
        if (!validation.IsValid)
        {
            return new ValidationFailed(validation.Errors);
        }

        // Business rule: the referenced route must exist before a stage can be added to it.
        var parentRoute = await db.Routes.FindAsync([command.RouteId], ct);
        if (parentRoute is null)
        {
            return new NotFound();
        }

        var stage = new Stage
        {
            RouteId = command.RouteId,
            Number = command.Number,
            Name = command.Name,
            StartPoint = command.StartPoint,
            EndPoint = command.EndPoint,
            DistanceKm = command.DistanceKm,
            ElevationDifferenceM = command.ElevationDifferenceM,
            Difficulty = command.Difficulty
        };
        db.Stages.Add(stage);
        await db.SaveChangesAsync(ct);
        return new AddStageResult(stage.Id);
    }
}
```

For `AddHikeLog`, apply the same shape: check `StageId` exists, and enforce business rules such as
`RuleFor(x => x.Rating).InclusiveBetween(1, 5)`.

**Use `FindAsync` for the parent-existence check, not the `AnyAsync` LINQ operator.** `FindAsync` is a
`DbSet` method that NSubstitute can mock directly in unit tests; `AnyAsync` needs an
`IAsyncQueryProvider` and throws against a substituted `DbSet`, making the handler impossible to
unit-test. This also mirrors the Update/Delete existence checks. (Same rule applies anywhere a handler
checks existence — Update and Delete already use `FindAsync`.)

## Example — Update (PUT)

```csharp
namespace HikingLog.Application.Routes.Commands;

using FluentValidation;
using HikingLog.Application.Common;
using HikingLog.Application.Data.Contracts;
using OneOf;

/// <summary>Command to update an existing route.</summary>
/// <param name="Id">The primary key of the route to update.</param>
/// <param name="Name">The new full name.</param>
/// <param name="Code">The new abbreviation code.</param>
/// <param name="Country">The new country or region.</param>
/// <param name="TotalDistanceKm">The new total distance in kilometres.</param>
/// <param name="Description">The new optional description.</param>
public record UpdateRoute(int Id, string Name, string Code, string Country, decimal TotalDistanceKm, string? Description);

/// <summary>Result returned after a route is updated.</summary>
/// <param name="Id">The primary key of the updated route.</param>
public record UpdateRouteResult(int Id);

/// <summary>Validates the <see cref="UpdateRoute"/> command.</summary>
internal sealed class UpdateRouteValidator : AbstractValidator<UpdateRoute>
{
    /// <summary>Initializes a new instance of <see cref="UpdateRouteValidator"/>.</summary>
    public UpdateRouteValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Code).NotEmpty().MaximumLength(20);
        RuleFor(x => x.Country).NotEmpty().MaximumLength(100);
        RuleFor(x => x.TotalDistanceKm).GreaterThan(0);
    }
}

/// <summary>Handles the <see cref="UpdateRoute"/> command.</summary>
public sealed class UpdateRouteHandler(IHikingLogDataContext db, IValidator<UpdateRoute> validator)
    : ICommandHandler<UpdateRoute, OneOf<UpdateRouteResult, ValidationFailed, NotFound>>
{
    /// <inheritdoc/>
    public async Task<OneOf<UpdateRouteResult, ValidationFailed, NotFound>> Handle(UpdateRoute command, CancellationToken ct)
    {
        var validation = await validator.ValidateAsync(command, ct);
        if (!validation.IsValid)
        {
            return new ValidationFailed(validation.Errors);
        }

        var route = await db.Routes.FindAsync([command.Id], ct);
        if (route is null)
        {
            return new NotFound();
        }

        route.Name = command.Name;
        route.Code = command.Code;
        route.Country = command.Country;
        route.TotalDistanceKm = command.TotalDistanceKm;
        route.Description = command.Description;
        await db.SaveChangesAsync(ct);
        return new UpdateRouteResult(route.Id);
    }
}
```

## Example — Delete (DELETE)

Delete needs no validator — existence is the only check, done in the handler.

```csharp
namespace HikingLog.Application.Routes.Commands;

using HikingLog.Application.Common;
using HikingLog.Application.Data.Contracts;
using OneOf;

/// <summary>Command to delete a route.</summary>
/// <param name="Id">The primary key of the route to delete.</param>
public record DeleteRoute(int Id);

/// <summary>Handles the <see cref="DeleteRoute"/> command.</summary>
public sealed class DeleteRouteHandler(IHikingLogDataContext db)
    : ICommandHandler<DeleteRoute, OneOf<Success, NotFound>>
{
    /// <inheritdoc/>
    public async Task<OneOf<Success, NotFound>> Handle(DeleteRoute command, CancellationToken ct)
    {
        var route = await db.Routes.FindAsync([command.Id], ct);
        if (route is null)
        {
            return new NotFound();
        }

        db.Routes.Remove(route);
        await db.SaveChangesAsync(ct);
        return new Success();
    }
}
```

## After scaffolding

- Register the handler (and validator, if any) with **register-di**.
- If the entity or DbSet does not yet exist, scaffold it first via **domain-entity**.
