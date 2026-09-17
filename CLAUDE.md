# Hiking Log

## Persona

Act as a senior .NET developer. Use a technical tone: precise, dense, assumes deep expertise. Respond in the
same language the user writes in; code, comments, and repository documentation are always in English.

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

## Git conventions

- Conventional Commits in English, imperative mood: `feat(<scope>): …`, `fix(<scope>): …`,
  `docs(<scope>): …`, `test(<scope>): …`, `chore(<scope>): …` — the scope is the feature or area
  (`hikelogs`, `skills`, `plan`).
- One feature = one `feature/<...>` branch = one reviewed `feat(...)` commit (plus a `docs(plan)` commit for
  the delivery status), merged into `master` with a merge commit (`Merge feature/<x> into master`).
- Source control is GitHub (`gh`). Opening a PR is always the user's call — never run `gh pr create`
  unprompted; `gh` is deliberately absent from the permission allow-list.
- Outside the `ship-slice` skill and a standalone `slice-builder` run, commit or push only when the user asks.

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

## Key patterns (details in `.claude/rules/backend/`)

Path-scoped rules load automatically when you read or edit a matching file; when you orchestrate from the
main loop before touching any file, read them explicitly. One line each here — the rule file is the source
of truth.

- **CQRS** — `backend-cqrs.md` (`src/HikingLog.Application/**`). Vertical slice per feature; one file holds
  record + validator + handler (commands) or record + DTO + handler (queries); handlers inject
  `IHikingLogDataContext` and run their validator explicitly; `OneOf` results — top-level Add
  `OneOf<T, ValidationFailed>`, child Add / Update `OneOf<T, ValidationFailed, NotFound>`, Delete
  `OneOf<Success, NotFound>` (no validator), Get single `OneOf<TDto, NotFound>`, Get collection
  `IReadOnlyList<TDto>`; never exceptions for expected failures; handlers and validators registered
  manually in `AddApplication()`.
- **Controllers** — `backend-controllers.md` (`src/HikingLog.Api/**`). Typed handler injection via the primary
  constructor (never IMediator); request/response records plus hand-written mapping extensions per feature;
  200 GET/PUT · 201 POST (`CreatedAtAction`) · 204 DELETE · 400 validation (`ValidationProblem`) · 404 not
  found, each mapped from a `OneOf` arm; collection GETs never 404.
- **Persistence** — `backend-persistence.md` (`src/HikingLog.Domain/**`, `src/HikingLog.Infrastructure/**`).
  Plain entities with navigations on both sides; one `IEntityTypeConfiguration<T>` per entity
  (`HasMaxLength` on every string, `HasPrecision` on every decimal, enums as strings, each relationship
  configured once on the parent side); expression-bodied `DbSet`s on context and interface; migrations only
  via `dotnet ef`.
- **Unit tests** — `backend-unit-testing.md` (`tests/HikingLog.Application.Tests/**`,
  `tests/HikingLog.Api.Tests/**`). xUnit + NSubstitute + Bogus; substitute `IHikingLogDataContext`; assert the
  `OneOf` arm and the side effect; collection handlers (`ToListAsync`) are not unit-testable against a
  substituted `DbSet` — cover them with Tier 0/Tier 3.
- **Integration tests** — `backend-integration-testing.md` (`tests/HikingLog.IntegrationTests/**`).
  Testcontainers SQL Server + Respawn; Tier 0 (HTTP contract, every status code per verb, behavioural filter
  tests) and Tier 3 (handler resolved from DI against the real database); Bogus fakers with
  `CustomInstantiator`. Test projects mirror `src/` one-to-one.

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

## Claude Code setup

Everything is committed; nothing needs installing beyond the CLI.

```
.claude/
├── rules/backend/*.md          ← path-scoped conventions (`paths:` frontmatter), loaded on demand
├── skills/<name>/SKILL.md      ← task-skills, orchestrators, review entry points (evals/ next to each)
├── agents/*.md                 ← slice-builder + read-only review lenses (spawned via the Agent tool)
├── commands/check.md           ← `/check` — the verification sequence
├── functional-plan.md          ← domain spec, always loaded (see Functional plan)
├── archive/                    ← superseded skills/agents/rules, kept for reference (inert — not loaded)
└── settings.json               ← team permission allow/deny list; personal overrides go in the gitignored
                                   settings.local.json
reviews/                        ← generated review reports (gitignored)
```

- **Never delete a superseded skill, agent, rule or instruction file** — move it to `.claude/archive/<kind>/`
  and add a row to `.claude/archive/README.md` naming its replacement.
- Eval workspaces (`.claude/skills/*-workspace/`) and skill-creator artifacts are gitignored.
- Use Grep/Glob for discovery; read a file whole only when you are about to edit it.
- After changing any skill, agent, rule, or instruction file, run `/review-claude-setup` (the `ship-slice`
  skill does this in its step 4).

## Skills

Each skill has a single responsibility; the session loads only the one(s) a request needs.
Build a full feature by composing several (typically `domain-entity` → `dotnet-ef-migration` →
`add-command`/`add-query` → `api-endpoint` → `register-di` → `integration-test`).

Task-skills (one layer each):

- `domain-entity` — entity in Domain + Fluent API config + DbSet on context and interface
- `add-command` — one CQRS command (record + validator + handler) in `Application/<Feature>/Commands/`
- `add-query` — one CQRS query (record + DTO + handler) in `Application/<Feature>/Queries/`
- `api-endpoint` — controller + request/response models + mapping in `Api/<Feature>/`
- `register-di` — register handlers and validators in `AddApplication()`
- `dotnet-ef-migration` — EF Core migrations (add, apply, undo, list)
- `integration-test` — write Tier 0 (HTTP contract) and Tier 3 (handler + database) integration tests

Orchestrators and reviews (main-loop skills — a subagent cannot spawn agents):

- `ship-slice` — delivers a feature end to end **with the quality gate**: spawns `slice-builder` (build only, no
  commit) → `backend-review` over the uncommitted working tree (apply confirmed fixes, re-verify, loop to
  convergence) → conditional `review-claude-setup` (only if a skill/agent/rule/instruction file changed) →
  completeness check vs `functional-plan.md` → commits the reviewed slice → docs sync → reports the
  ready-to-run `gh pr create`. Use for "build, review and ship X"; use `slice-builder` for a plain build, or a
  single task-skill for one layer.
- `backend-review` — parallel five-lens code review (architecture, data-performance, correctness,
  code-quality, tests) of the working tree, the branch diff vs `master`, a file list, a PR, or the whole
  solution; verified, de-duplicated report in `reviews/`. Read-only.
- `review-claude-setup` — parallel six-lens review of `.claude/**` and `CLAUDE.md` against the live Claude Code
  docs and against `src/` (code-example drift); report in `reviews/`. Read-only.
- `/check` (command) — runs the verification sequence and stops at the first failure.

## Agents

Spawn via the Agent tool (`subagent_type`). Agents run in their own context window and cannot spawn agents.

- `slice-builder` — orchestrates the task-skills to build a whole feature end to end
  (brief → `domain-entity` → `dotnet-ef-migration` → commands/queries → `api-endpoint` →
  `register-di` → `integration-test` → verify → commit & push, or leave uncommitted in composed mode).
  Use for a full slice; use the individual skills for one layer. When wrapped by the `ship-slice` skill
  it runs in **composed mode** — build and verify only, leaving changes uncommitted so `ship-slice` can
  review before committing (it takes over the commit/push).
- `backend-reviewer-architecture` · `backend-reviewer-data-performance` · `backend-reviewer-correctness` ·
  `backend-reviewer-code-quality` · `backend-reviewer-tests` — read-only code-review lenses (`Read, Grep,
  Glob`) that return a JSON findings array; orchestrated by `backend-review`, usable standalone for one angle.
- `claude-setup-reviewer-skills` · `claude-setup-reviewer-agents` · `claude-setup-reviewer-config` ·
  `claude-setup-reviewer-consistency` · `claude-setup-reviewer-placement` ·
  `claude-setup-reviewer-code-examples` — read-only lenses over the Claude Code setup; orchestrated by
  `review-claude-setup`. The `code-examples` lens verifies every code snippet and code claim in skills,
  agents and rules against `src/` and `tests/`.

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
