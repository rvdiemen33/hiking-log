# Hiking Log

## What is this project?

REST API for tracking completed stages on long-distance hiking trails (LAW, Pieterpad, GR5).
Built with **Clean Architecture + Vertical Slice Architecture** in a single .NET 10 solution: four
architecture-layer projects (Domain → Application → Infrastructure → Api) plus two .NET Aspire support
projects (AppHost, ServiceDefaults).

Clean Architecture defines the layer boundary (Domain → Application → Infrastructure → Api) and ensures inner layers know nothing about outer layers. Vertical Slice Architecture determines the internal organization of `HikingLog.Application`: each feature (Route, Stage, HikeLog) is a standalone vertical slice with its own commands, queries, and validators. Slices share no logic with each other — shared contracts live in `Data/Contracts/`.

```
HikingLog.slnx
├── src/
│   ├── HikingLog.Domain          → Entities, enums, constants
│   ├── HikingLog.Application     → Commands, queries, validators, IDataContext interface
│   ├── HikingLog.Infrastructure  → DbContext, Fluent API configs, migrations, seed data
│   ├── HikingLog.Api             → Controllers, API models, Program.cs
│   ├── HikingLog.AppHost         → .NET Aspire orchestration host
│   └── HikingLog.ServiceDefaults → Shared Aspire service defaults (telemetry, health, resilience)
└── tests/
    ├── HikingLog.Application.Tests
    ├── HikingLog.Api.Tests
    └── HikingLog.IntegrationTests  → Testcontainers + Respawn, Tier 0 & Tier 3
```

## Branch workflow

- Never work directly on `master` — always create a feature branch before starting.
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

Only deliver code when all four pass. Note: CI builds and tests in **Release**
(`--configuration Release --no-build`); run the sequence with `--configuration Release` if you need to
reproduce a Release-only failure locally.

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
- Manual static extension methods for API model mapping (no AutoMapper, no source-gen mapper)
- OneOf (discriminated unions for success/failure)
- FluentValidation · Swashbuckle (Swagger UI)
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
│       ├── GetRoute.cs        ← record + DTO + handler in one file (queries have no validator)
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

- File structure: namespace declaration first (file-scoped, no braces), then `using` directives — never the other way around.
- Validators are `internal sealed class` in the same file as the command (commands only — queries have no validator).
- Use static extension methods for API model mapping — never AutoMapper or a source-gen mapper.
- Always use FluentValidation for input validation.
- Use C# 13 features where applicable (primary constructors, collection expressions).
- Add an XML doc comment to all type and member declarations: classes, records, enums, interfaces, and all methods (regardless of access modifier). Minimum a `<summary>`; add `<param>`, `<returns>`, and `<exception>` where relevant.
- Write inline body comments only when the reason is not apparent from the code itself.
- Never remove existing inline comments when modifying code.
- Write all comments in English.
- Register DI via `ServiceCollectionExtensions` per layer — not directly in `Program.cs`.
- Register handlers manually in `ServiceCollectionExtensions` — no automatic assembly scanning.

### OneOf patterns

- Add (top-level entity): `OneOf<TResult, ValidationFailed>` — no `NotFound`
- Add (child with parent FK) and Update: `OneOf<TResult, ValidationFailed, NotFound>`
- Delete: `OneOf<Success, NotFound>` — no validator
- Get single: `OneOf<TResult, NotFound>`; Get collection: `IReadOnlyList<TDto>` (never fails)
- Never use exceptions for expected error paths — always use OneOf.

## Test conventions

- `HikingLog.Application.Tests` — pure unit tests; handlers tested with NSubstitute mocks of `IHikingLogDataContext` and Bogus for test data.
- `HikingLog.Api.Tests` — controller/integration tests; tests HTTP status code, response body, and routing.
- `HikingLog.IntegrationTests` — integration tests with real SQL Server via Testcontainers and Respawn; Tier 0 (HTTP contract) and Tier 3 (handler with database).
- No shared state between tests; use `[Fact]` for single cases, `[Theory]` + `[InlineData]` for multiple inputs.
- File structure in `tests/` mirrors `src/`: test class ↔ handler/controller is one-to-one.

See @.claude/integration-testing.md for the full conventions, structure, and code patterns.

## HTTP status codes

- 200 OK — successful GET, PUT
- 201 Created — successful POST (with `CreatedAtAction`)
- 204 No Content — successful DELETE
- 400 Bad Request — validation failure (via `ValidationProblem`)
- 404 Not Found — resource does not exist

## Skills

Each skill has a single responsibility; the session loads only the one(s) a request needs.
Build a full feature by composing several (typically `domain-entity` → `dotnet-ef-migration` →
`add-command`/`add-query` → `api-endpoint` → `register-di` → `integration-test`).

- `domain-entity` — entity in Domain + Fluent API config + DbSet on context and interface
- `add-command` — one CQRS command (record + validator + handler) in `Application/<Feature>/Commands/`
- `add-query` — one CQRS query (record + DTO + handler) in `Application/<Feature>/Queries/`
- `api-endpoint` — controller + request/response models + mapping in `Api/<Feature>/`
- `register-di` — register handlers and validators in `AddApplication()`
- `dotnet-ef-migration` — EF Core migrations (add, apply, undo, list)
- `integration-test` — write Tier 0 (HTTP contract) and Tier 3 (handler + database) integration tests

## Agents

Spawn via the Agent tool (`subagent_type`). Agents run in their own context window.

- `slice-builder` — orchestrates the task-skills to build a whole feature end to end
  (brief → `domain-entity` → `dotnet-ef-migration` → commands/queries → `api-endpoint` →
  `register-di` → `integration-test` → verify → push). Use for a full slice; use the individual skills
  for one layer.
- `skill-reviewer` — read-only reviewer of the skills, agents, and instruction files (correctness vs
  `src/`, skill/agent design, instruction consistency). Spawn after changing any `SKILL.md`, agent
  definition file (`.claude/agents/*.md`), or instruction file.

## Pre-merge gate

CI (`.github/workflows/ci.yml`) builds, runs the format check, and runs the unit and integration test
suites on every push and PR. The intended hard, unbypassable gate is a GitHub branch-protection
required status check on `master` (future action) — that is the authoritative enforcement. Run the
local verification sequence (see **Verification**) before opening a PR.

## Functional plan

See @.claude/functional-plan.md for the domain model, API endpoints, business rules, and seed data.

## Scope

- Work exclusively within this repository (`C:\github\hiking-log`).
- Do not add packages without an explicit request from the user.
