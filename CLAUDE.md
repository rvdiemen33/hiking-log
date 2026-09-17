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
├── tests/
│   ├── HikingLog.Application.Tests
│   ├── HikingLog.Api.Tests
│   └── HikingLog.IntegrationTests  → Testcontainers + Respawn, Tier 0 & Tier 3
└── docs/
    ├── specs/                      → feature specs (see Spec-driven development); archive/ holds closed ones
    └── adr/                        → architecture decision records, created on demand by `spec-close`
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
- Outside the `ship-slice` skill, a standalone `slice-builder` run, and the spec-flow skills that own a
  specific commit (`spec-create` the draft; `spec-review` the gate result; `spec-implement` the status
  flips; `spec-close` the retirement, after the user confirms), commit or push only when the user asks.
  `docs/specs/README.md` owns the spec-flow commit conventions: the `docs(spec)` scope, what each
  commit stages, and the `Reopens-Spec` trailer for a status that runs backwards.

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

## Running locally

**.NET Aspire AppHost — the primary dev loop.** Orchestrates the SQL Server container and the API, with a
dashboard showing structured logs, traces and health:

```powershell
dotnet run --project src/HikingLog.AppHost --launch-profile https
```

The dashboard URL is printed at startup. The SQL Server container is persistent: it survives AppHost
restarts and keeps running after it stops (`docker rm -f <name>` to remove it). The API is published on
`http://localhost:5000` and `https://localhost:5001`. Never run the AppHost and the compose stack at the
same time; they compete for the same host ports.

The Aspire CLI reads `aspire.config.json` in the repo root to locate the AppHost, so `aspire run` works too.
`AspireUseCliBundle` stays at its default of `false` so the dashboard and DCP resolve from NuGet and the
build needs no Aspire CLI; the AppHost suppresses the resulting `ASPIRE010` with that reasoning inline.

**The API on its own**, against a SQL Server you provide via the connection string:

```powershell
dotnet run --project src/HikingLog.Api
```

**Docker Compose**, to reproduce what the container build produces:

```powershell
docker compose up -d
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
- .NET Aspire 13.5.4 (AppHost orchestration + ServiceDefaults: OpenTelemetry, health checks, service
  discovery, HTTP resilience). Keep the `Aspire.AppHost.Sdk` version in the AppHost csproj and the
  `Aspire.Hosting.*` versions in `Directory.Packages.props` on the same release.
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

Each rule below is the source of truth for its layer and loads automatically when you read or edit a
matching file. When you orchestrate from the main loop before touching any file, read the relevant one
explicitly.

- **CQRS** — `backend-cqrs.md` (`src/HikingLog.Application/**`): slice layout, the one-file command/query
  shape, `OneOf` result contracts per operation, validators, manual DI registration.
- **Controllers** — `backend-controllers.md` (`src/HikingLog.Api/**`): typed handler injection, API models
  and mapping extensions, the status code each `OneOf` arm maps to.
- **Persistence** — `backend-persistence.md` (`src/HikingLog.Domain/**`, `src/HikingLog.Infrastructure/**`):
  entity shape, Fluent API conventions, relationship ownership, DbSet style (expression-bodied on the
  context, plain `DbSet<T> X { get; }` on the interface), migrations, seed data.
- **Unit tests** — `backend-unit-testing.md` (`tests/HikingLog.Application.Tests/**`,
  `tests/HikingLog.Api.Tests/**`): xUnit and NSubstitute only, naming, stubbing shapes, and what cannot be
  unit-tested here (collection handlers, and validators, which are `internal`).
- **Integration tests** — `backend-integration-testing.md` (`tests/HikingLog.IntegrationTests/**`):
  Tier 0 and Tier 3, required status coverage, behavioural filter tests, Bogus fakers.

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
├── skills/<name>/SKILL.md      ← task-skills, orchestrators, review entry points (task-skills carry evals/)
├── agents/*.md                 ← slice-builder + read-only review lenses (spawned via the Agent tool)
├── commands/check.md           ← `/check` — the verification sequence
├── functional-plan.md          ← domain spec, always loaded (see Functional plan)
├── archive/                    ← superseded skills/agents/rules, kept for reference (inert — not loaded)
└── settings.json               ← team permission allow/deny list; personal overrides go in the gitignored
                                   settings.local.json
reviews/                        ← generated review reports (gitignored)
docs/specs/                     ← feature specs, committed (see Spec-driven development)
docs/adr/                       ← architecture decision records, written on demand by `spec-close`
```

- **Never delete a superseded skill, agent, rule or instruction file** — move it to `.claude/archive/<kind>/`
  (`agents/`, `skills/<name>/`, `rules/`; instruction files go at the archive root) and add a row to
  `.claude/archive/README.md` naming its replacement.
- Eval workspaces (`.claude/skills/*-workspace/`) and skill-creator artifacts are gitignored.
- Use Grep/Glob for discovery; read a file whole only when you are about to edit it.
- After changing any skill, agent, rule, slash command, or instruction file, run `/review-claude-setup` (the `ship-slice`
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
  commit) → `backend-review` (+ `spec-verify` when a spec drives the slice) over the uncommitted working
  tree (apply confirmed fixes, re-verify, loop to convergence; a `spec-verify` design finding reopens the
  spec and stops) → conditional `review-claude-setup` (only if a skill/agent/rule/instruction file changed) →
  completeness check vs `functional-plan.md` → commits the reviewed slice → docs sync → reports the
  ready-to-run `gh pr create`. Use for "build, review and ship X"; use `slice-builder` for a plain build, or a
  single task-skill for one layer.
- `backend-review` — parallel five-lens code review (architecture, data-performance, correctness,
  code-quality, tests) of the working tree, the branch diff vs `master`, a file list, a PR, or the whole
  solution; verified, de-duplicated report in `reviews/`. Read-only.
- `review-claude-setup` — parallel six-lens review of `.claude/**` and `CLAUDE.md` against the live Claude Code
  docs and against `src/` (code-example drift); report in `reviews/`. Read-only.
- `/check` (command) — runs the verification sequence and stops at the first failure.

Spec-driven development (main-loop skills — see **Spec-driven development**; like the orchestrators they
carry no `evals/`, because they produce a spec and a delegation, not generated code):

- `spec-create` — interviews, explores the solution read-only, writes `docs/specs/<name>.md` as `draft`,
  auto-refines it against `spec-reviewer` in advisory mode, then commits the draft on `feature/<slug>`.
- `spec-review` — the `draft → reviewed` gate; dispatches `spec-reviewer` in gate mode and commits the
  gate result.
- `spec-implement` — turns an **approved** spec into a brief and delegates to `ship-slice` (default) or
  `slice-builder`; maintains the spec's status and checkboxes (and reopens the spec on a design finding
  on its `slice-builder`-only route). Writes no slice code.
- `spec-verify` — lays the slice diff against its spec via the `spec-verifier` agent: missing or
  contradicted `R` requirements, untested `AC` scenarios, unspecified changes — each classified
  **mechanical** (fix the code) or **design** (reopen the spec). Runs inside `ship-slice`'s review loop
  when a spec drives the slice, and standalone. Read-only.
- `spec-close` — retires an **implemented** spec: harvests into `functional-plan.md` (and `docs/adr/` when
  a lasting decision exists), archives the spec under `docs/specs/archive/`.

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
- `spec-reviewer` — model-pinned (`opus`) reviewer of **one** spec in `docs/specs/`, across eight
  dimensions (the eighth: impact on existing data, behaviour and clients). Gate mode appends
  `## Review Notes` and flips `status` to `reviewed`; advisory mode reports only. Dispatched by
  `spec-review` and by `spec-create`'s refine pass. Reviews specs, never source code.
  It deliberately carries no `-<lens>` suffix: unlike `backend-review` and `review-claude-setup`, the spec
  gate is a **single-agent** family — one spec is small enough that fanning out would cost more than it
  buys, and the eight dimensions stay in one file.
- `spec-verifier` — model-pinned (`opus`) verifier of the **code diff against one spec** (`Read, Grep,
  Glob, Bash` — Bash for read-only `git status`/`git diff` only). Returns coverage per `R`/`AC`
  id plus findings classified mechanical or design; dispatched by `spec-verify`. It verifies code against
  a spec — `backend-review` judges code against the rules, `spec-reviewer` judges the spec itself.
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

`.claude/functional-plan.md` holds the domain model, API endpoints, business rules, and seed data. Read it
whenever you build, review, or reason about a feature. It is a plain pointer rather than an `@` include on
purpose: the spec is situational, and the skills and agents that need it already read it themselves.

## Spec-driven development

Non-trivial features are specified in `docs/specs/` before they are built:
`spec-create` → `draft` → `spec-review` → `reviewed` → **(you set `approved` by hand)** → `spec-implement`
(whose `ship-slice` review loop runs `spec-verify`) → `implemented` → `spec-close`. Specs are ephemeral working artifacts; what lasts is harvested into
`.claude/functional-plan.md` (and `docs/adr/`) when the spec is closed.

A spec normally starts from a GitHub issue filed with the **User story** form
(`.github/ISSUE_TEMPLATE/user-story.yml`, the Definition of Ready), and its status can run backwards
when a later phase finds a design gap. `docs/specs/README.md` is the source of truth for the statuses,
the reopening table, filename and frontmatter conventions and the `TO CONFIRM:` marker — read it when
you touch a spec. Two things matter
session-wide: **`approved` is the one transition no skill makes for you**, and **a small, obvious change
needs no spec** — go straight to `ship-slice` or a single task-skill.

## Scope

- Work exclusively within this repository (`C:\github\hiking-log`).
- Do not add packages without an explicit request from the user.
