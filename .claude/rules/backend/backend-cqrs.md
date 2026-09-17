---
paths:
  - "src/HikingLog.Application/**"
---

# Backend CQRS pattern (Application layer)

`HikingLog.Application` is organised as **vertical slices**: one folder per feature (`Routes/`, `Stages/`,
`HikeLogs/`), each with its own `Commands/` and `Queries/`. Slices share no logic with each other — the only
shared code is `Common/` (handler interfaces + result types) and `Data/Contracts/` (`IHikingLogDataContext`).
Add a feature by adding a folder; never touch another slice to do so.

```
Application/
├── Common/                          ← ICommandHandler<,>, IQueryHandler<,>, ValidationFailed, NotFound, Success
├── Data/Contracts/
│   └── IHikingLogDataContext.cs     ← DbSet<T> per entity + SaveChangesAsync; implemented in Infrastructure
├── Extensions/
│   └── ServiceCollectionExtensions.cs ← AddApplication(): manual handler + validator registration
└── <Feature>/
    ├── Commands/<Verb><Entity>.cs   ← record + validator + handler in ONE file
    └── Queries/Get<Entity>(s).cs    ← record + DTO + handler in ONE file (queries have no validator)
```

## Single-file structure

```csharp
public record AddRoute(string Name, ...);                                    // command (positional record)
public record AddRouteResult(int Id);                                        // result
internal sealed class AddRouteValidator : AbstractValidator<AddRoute> { }    // validator — commands only
public sealed class AddRouteHandler(IHikingLogDataContext db, IValidator<AddRoute> validator)
    : ICommandHandler<AddRoute, OneOf<AddRouteResult, ValidationFailed>> { } // handler (primary constructor)
```

- Commands, queries, results and DTOs are positional `public record`s. Handlers are `public sealed class` with a
  primary constructor. Validators are `internal sealed class`, in the same file as their command.
- Naming: `<Verb><Entity>` for commands (`AddStage`, `UpdateRoute`, `DeleteHikeLog`); `Get<Entity>`,
  `Get<Entities>` and `Get<Entities>By<Parent>` for queries; `<Command>Result` for command results;
  `<Entity>Dto` for query DTOs (declared **once** per feature, reused by that feature's other queries);
  `<Command>Validator`; `<CommandOrQuery>Handler`.
- The handler interfaces are the project's own (`HikingLog.Application.Common`), not MediatR:
  `ICommandHandler<TCommand, TResult>` / `IQueryHandler<TQuery, TResult>` expose
  `Task<TResult> Handle(TCommand command, CancellationToken ct)`. Forward `ct` to every EF Core async call.

## Data access — no repository pattern

Handlers inject `IHikingLogDataContext` directly and use its `DbSet<T>` properties (`db.Routes`, `db.Stages`,
`db.HikeLogs`) plus `SaveChangesAsync(ct)`. Do not introduce repositories, unit-of-work wrappers, or
"service"/"manager" classes around handlers.

- **Existence checks use `FindAsync([id], ct)`** — a `DbSet` method NSubstitute can stub — never `AnyAsync` or
  `FirstOrDefaultAsync`. Those need an `IAsyncQueryProvider` and throw against a substituted `DbSet`, which makes
  the handler impossible to unit-test. This applies to the parent-FK check in a child Add/Update as well as to
  the entity lookup in Update and Delete.
- Collection queries project straight to the DTO inside the query:
  `.Where(...).Select(x => new XDto(...)).ToListAsync(ct)`. Add `.OrderBy(...)` when the entity has a natural
  sequence (`Stage.Number`); otherwise row order is unspecified.
- Query handlers never mutate state: no `SaveChangesAsync`, no `Add`/`Remove`, no assignments to tracked entities.

## Return types — `OneOf`, never exceptions for expected failures

| Operation | Signature |
|---|---|
| Add — top-level entity (Route) | `OneOf<TResult, ValidationFailed>` |
| Add — child entity whose parent must exist (Stage → Route, HikeLog → Stage) | `OneOf<TResult, ValidationFailed, NotFound>` |
| Update | `OneOf<TResult, ValidationFailed, NotFound>` |
| Delete | `OneOf<Success, NotFound>` — **no validator**; existence is the only check |
| Get single | `OneOf<TDto, NotFound>` |
| Get collection, incl. by-parent | `IReadOnlyList<TDto>` — never fails; a missing parent yields an empty list |
| Aggregate keyed on an entity (`/routes/{id}/progress`) | `OneOf<TDto, NotFound>` — verify the entity exists first |
| Aggregate over everything (`/statistics`) | `TDto` — always exactly one result |

`ValidationFailed`, `NotFound` and `Success` are the project's own classes in `HikingLog.Application.Common`
(`ValidationFailed` wraps `IEnumerable<ValidationFailure>`); only `OneOf<>` itself comes from the OneOf package.
Never `throw` for a validation failure or a missing entity — return the variant.

## Validators (FluentValidation)

- Every Add and Update command has a validator. Delete commands and all queries have none.
- **The handler runs the validator itself** — `var validation = await validator.ValidateAsync(command, ct);`
  then `return new ValidationFailed(validation.Errors);` on failure, **before** touching the database. There is
  no pipeline behaviour that validates for you.
- Chain all rules for one property in a single `RuleFor` call. Typical rules: `NotEmpty().MaximumLength(n)` for
  strings (`n` equals the Fluent API `HasMaxLength(n)`), `GreaterThan(0)` for ids, distances and sequence
  numbers, `GreaterThanOrEqualTo(0)` for elevation, `InclusiveBetween(1, 5)` for `Rating`, `IsInEnum()` for enums.

## DI registration — manual, in `AddApplication()`

Every handler and validator is registered explicitly in `Extensions/ServiceCollectionExtensions.cs` with its
**full generic signature** (the exact `OneOf<...>` the controller injects) and `AddScoped` — no assembly
scanning, no `AddTransient`/`AddSingleton`. Group registrations per feature under `// <Feature> — handlers` and
`// <Feature> — validators` comments. Edits to this file are additive; a missing or mistyped registration only
fails at runtime, when the controller is resolved.

## Business rules enforced in this layer

- Creating a stage requires the referenced route to exist; creating or updating a hike log requires the
  referenced stage to exist — both return `NotFound`, checked with `FindAsync`.
- `Rating` must be between 1 and 5 (validator).
