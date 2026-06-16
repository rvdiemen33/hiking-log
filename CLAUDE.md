# Hiking Log

## What is this project?

REST API for tracking completed stages on long-distance hiking trails (LAW, Pieterpad, GR5).
Built with **Clean Architecture + Vertical Slice Architecture** across four projects in a single .NET 10 solution.

Clean Architecture defines the layer boundary (Domain → Application → Infrastructure → Api) and ensures inner layers know nothing about outer layers. Vertical Slice Architecture determines the internal organization of `HikingLog.Application`: each feature (Route, Stage, HikeLog) is a standalone vertical slice with its own commands, queries, and validators. Slices share no logic with each other — shared contracts live in `Data/Contracts/`.

```
HikingLog.sln
├── src/
│   ├── HikingLog.Domain          → Entities, enums, constants
│   ├── HikingLog.Application     → Commands, queries, validators, IDataContext interface
│   ├── HikingLog.Infrastructure  → DbContext, Fluent API configs, migrations, seed data
│   └── HikingLog.Api             → Controllers, API models, Program.cs
└── tests/
    ├── HikingLog.Application.Tests
    ├── HikingLog.Api.Tests
    └── HikingLog.IntegrationTests  → Testcontainers + Respawn, Tier 0 & Tier 3
```

## Branch workflow

- Never work directly on `main` — always create a feature branch before starting.
- Naming convention: `feature/<short-description>` (e.g. `feature/add-stage-query`).
- For larger or parallel tasks: use `git worktree` so branches can be built and tested in isolation.

## Verification

Run after every change in this order:

```powershell
dotnet build
dotnet format --verify-no-changes
dotnet test tests/HikingLog.Application.Tests
dotnet test tests/HikingLog.Api.Tests
```

Only deliver code when all four pass.

Integration tests require Docker (Testcontainers starts a SQL Server container):

```powershell
dotnet test tests/HikingLog.IntegrationTests
```

Run the API locally:

```powershell
dotnet run --project src/HikingLog.Api
```

## Local setup

Prerequisites:
- .NET 10 SDK
- Docker Desktop (required for integration tests via Testcontainers)
- SQL Server (local or via Docker) for the API itself

Connection string for local development — configure via user secrets:

```powershell
dotnet user-secrets set "ConnectionStrings:HikingLog" "Server=localhost;Database=HikingLog;Trusted_Connection=True;" --project src/HikingLog.Api
```

## Stack

- .NET 10 · ASP.NET Core Web API
- Entity Framework Core 10 (Code First, SQL Server)
- Custom CQRS interfaces — `ICommandHandler<TCommand, TResult>` and `IQueryHandler<TQuery, TResult>` defined in `HikingLog.Application`
- Riok.Mapperly (source-gen mapping) or manual extension methods
- OneOf (discriminated unions for success/failure)
- FluentValidation · Swagger / Scalar
- xUnit · NSubstitute · Bogus (tests)

## Architecture rules

- Domain knows no one. Application knows Domain. Infrastructure knows Application + Domain. Api knows Application + Infrastructure.
- Never add a project reference that causes an inner layer to reference an outer layer.
- Do not use the repository pattern — handlers inject `IHikingLogDataContext` directly.
- Define `IHikingLogDataContext` in `HikingLog.Application`, implement in `HikingLog.Infrastructure`.
- Never return entities at the API boundary — always use API models with mapping via extension methods.

## CQRS structure (Application)

Each feature is a vertical slice: commands, queries, and validators are co-located, not spread across type folders. Add a new feature by creating a new folder — do not touch existing slices.

```
Application/
├── Routes/
│   ├── Commands/
│   │   ├── AddRoute.cs        ← record + validator + handler in one file
│   │   └── UpdateRoute.cs
│   └── Queries/
│       ├── GetRoute.cs        ← record + validator + handler in one file
│       └── GetRoutes.cs
├── Stages/
│   ├── Commands/
│   └── Queries/
├── HikeLogs/
│   ├── Commands/
│   └── Queries/
├── Data/
│   └── Contracts/
│       └── IHikingLogDataContext.cs
└── Extensions/
    └── ServiceCollectionExtensions.cs
```

## Coding standards

- Validators are `internal sealed class` in the same file as the command or query.
- Use Riok.Mapperly or static extension methods for API model mapping — never AutoMapper.
- Always use FluentValidation for input validation.
- Use C# 13 features where applicable (primary constructors, collection expressions).
- Add an XML doc comment to all type and member declarations: classes, records, enums, interfaces, and all methods (regardless of access modifier). Minimum a `<summary>`; add `<param>`, `<returns>`, and `<exception>` where relevant.
- Write inline body comments only when the reason is not apparent from the code itself.
- Never remove existing inline comments when modifying code.
- Write all comments in English.
- Register DI via `ServiceCollectionExtensions` per layer — not directly in `Program.cs`.
- Register handlers manually in `ServiceCollectionExtensions` — no automatic assembly scanning.

### OneOf patterns

- Queries that may return nothing: `OneOf<TResult, NotFound>`
- Commands that fail due to validation or missing resource: `OneOf<TResult, ValidationFailed, NotFound>`
- Never use exceptions for expected error paths — always use OneOf.

## Test conventions

- `HikingLog.Application.Tests` — pure unit tests; handlers tested with NSubstitute mocks of `IHikingLogDataContext` and Bogus for test data.
- `HikingLog.Api.Tests` — controller/integration tests; tests HTTP status code, response body, and routing.
- `HikingLog.IntegrationTests` — integration tests with real SQL Server via Testcontainers and Respawn; Tier 0 (HTTP contract) and Tier 3 (handler with database).
- No shared state between tests; use `[Fact]` for single cases, `[Theory]` + `[InlineData]` for multiple inputs.
- File structure in `tests/` mirrors `src/`: test class ↔ handler/controller is one-to-one.

See @.claude/integration-testing.md for the full conventions, structure, and code patterns.

For EF Core migrations (add, apply, undo, list), use the `dotnet-ef-migration` skill.

## HTTP status codes

- 200 OK — successful GET, PUT
- 201 Created — successful POST (with `CreatedAtAction`)
- 204 No Content — successful DELETE
- 404 Not Found — resource does not exist

## Skills

- `dotnet-ef-migration` — EF Core migrations (add, apply, undo, list)
- `vertical-slice` — implement a complete CRUD slice across all four layers (Application, Infrastructure, Api, DI)
- `integration-test` — write Tier 0 (HTTP contract) and Tier 3 (handler + database) integration tests

## Functional plan

See @.claude/functional-plan.md for the domain model, API endpoints, business rules, and seed data.

## Scope

- Work exclusively within this repository (`C:\github\hiking-log`).
- Do not add packages without an explicit request from the user.
