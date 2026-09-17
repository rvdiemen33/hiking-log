---
status: implementing
issue: 8
created: 2026-09-17
---

# Feature: Stage Notes

## Overview
From GitHub issue #8 ("Add note field"), verbatim:

> As a hiker I want the possibility to add a Note bij een stage
>
> Business value: Dit geeft de gebruiker extra functionaliteit

Confirmed interpretation (the issue's Scope field says "Routes", the story says stage; the user chose
**Stage**): an optional free-text `Notes` field on the existing `Stage` entity, mirroring `HikeLog.Notes`.
It is set through the existing `POST /stages` and `PUT /stages/{id}` and returned by the existing
`GET /stages/{id}` and `GET /routes/{routeId}/stages`. No new entity, no new endpoints.

## Impact / Affected areas
_From codebase exploration on 2026-09-17._
- **Reference slice mirrored**: HikeLogs — `src/HikingLog.Application/HikeLogs/`, `src/HikingLog.Api/HikeLogs/`;
  specifically `HikeLog.Notes` (`src/HikingLog.Domain/Entities/HikeLog.cs`, `HasMaxLength(2000)` in
  `src/HikingLog.Infrastructure/Data/Configurations/HikeLogConfiguration.cs`, `RuleFor(x => x.Notes).MaximumLength(…)`
  in `AddHikeLog.cs` / `UpdateHikeLog.cs`, `string? Notes` in `Api/HikeLogs/Models.cs`, `null` in `HikeLogFaker`).
- **Existing code this feature touches** (this is an **extension of the existing Stages slice**):
  - `src/HikingLog.Domain/Entities/Stage.cs` — new property.
  - `src/HikingLog.Infrastructure/Data/Configurations/StageConfiguration.cs` — new `Property(...)` call.
  - `src/HikingLog.Infrastructure/Migrations/` — one new migration (`AddNotesToStage`) plus the regenerated
    `HikingLogDbContextModelSnapshot.cs`.
  - `src/HikingLog.Application/Stages/Commands/AddStage.cs`, `UpdateStage.cs` — record, validator, handler copy.
  - `src/HikingLog.Application/Stages/Queries/GetStage.cs` (declares `StageDto`), `GetStagesByRoute.cs` (projection).
  - `src/HikingLog.Api/Stages/Models.cs`, `MappingExtensions.cs`, `StagesController.cs` (the two inline
    `StageResponse` constructions in `Create` and `Update`).
  - Tests: `tests/HikingLog.Application.Tests/Stages/*` (positional `AddStage`/`UpdateStage` constructions),
    `tests/HikingLog.IntegrationTests/Stages/Fakers/StageFaker.cs` (builds `CreateStageRequest`; used by the
    HikeLogs endpoint tests too), `tests/HikingLog.IntegrationTests/Stages/Endpoints/*`,
    `tests/HikingLog.IntegrationTests/Stages/Consumers/AddStageHandlerTests.cs`, and
    `tests/HikingLog.IntegrationTests/HikeLogs/Consumers/AddHikeLogHandlerTests.cs` (line 37 constructs
    `AddStage` positionally to seed a parent stage — compile fix only; `Notes` gets no default value, matching
    `AddHikeLog`, so the call site passes `null`).
  - **Untouched**: `DeleteStage.cs`, `HikingLogDbContext`, `IHikingLogDataContext`, `ServiceCollectionExtensions.cs`
    (generic signatures do not change), `SeedData.cs` / `DataSeeder.cs` (nullable property compiles as-is,
    no seed notes), `DataSeederTests.cs`, HikeLogs handlers (`db.Stages.FindAsync` existence checks only).
- **Cross-cutting — XML docs**: every new `Notes` member carries documentation. The entity property gets the
  `<summary>` given under `## Domain`; the positional records in Application (`AddStage`, `UpdateStage`,
  `StageDto`) and Api (`CreateStageRequest`, `UpdateStageRequest`, `StageResponse`) each get a
  `<param name="Notes">Optional personal note about the stage.</param>` line, as the HikeLog records do.
  `Directory.Build.props` has `GenerateDocumentationFile`, so a missing line is a build warning.
- **Harvest at `spec-close`**: `.claude/functional-plan.md` → `### Stage` table gains the row
  `| Notes | string? | Personal note, optional |`. No endpoint or business-rule change to harvest.
- **Layers that already exist** (spec-implement passes these on as the skip-list): entity | Fluent config | DbSet |
  Application slice | controller | DI — every layer exists and is **modified in place**; only the migration is new.
- **Data strategy**: additive nullable column `Notes nvarchar(2000) NULL` on `Stages`; existing rows read back
  as `null`. No backfill, no data migration, reversible by dropping the column.
- **Behaviour delta**: `PUT /stages/{id}` is full-replace, so a request that omits `notes` (or sends `null`)
  clears an existing note. No other endpoint changes behaviour.
- **Client impact**: additive optional JSON property `notes` on `CreateStageRequest`, `UpdateStageRequest` and
  `StageResponse`. Existing clients that omit it keep working (deserialises to `null`); responses gain one
  field. Non-breaking. The C# positional records change shape, which is internal to this solution.
- **Collisions / conflicts**: none — `Stage` has no `Notes`/`Note` property. Known inconsistency in the mirror:
  `HikeLog.Notes` is `HasMaxLength(2000)` in the database but `MaximumLength(1000)` in its validators; this spec
  uses **2000 on both sides** for `Stage.Notes` (user decision) and leaves the HikeLog mismatch out of scope.

## Domain
- **Entity**: Stage (feature folder `Stages`) — existing; extended — `R1`
- **Properties** (added):
  - Notes: `string?` — optional, max 2000 chars; `get; set;`, no initializer; placed after `Difficulty`, before
    the navigations (mirrors `HikeLog.Notes`); XML doc `Gets or sets an optional personal note about the stage.`
- **Fluent API**: `builder.Property(s => s.Notes).HasMaxLength(2000);` — no `IsRequired()`; optionality comes
  from the nullable reference type, exactly as `HikeLogConfiguration` and `RouteConfiguration.Description`.
- **Relationships**: unchanged (Route (1) → Stage (n) owned by `RouteConfiguration`; Stage (1) → HikeLog (n)
  owned by `StageConfiguration`).
- **Enums**: none
- **Indexes**: none
- **Seed data**: none — all 50 seeded stages keep `Notes = null` (user decision); `SeedData.cs` unchanged.
- **Migration name**: `AddNotesToStage`
- **Acceptance**:
  - `AC1.1` Given the `AddNotesToStage` migration is applied, when a `Stage` is saved with a 2000-character note
    and read back with `FindAsync`, then the note round-trips unchanged; when saved with `Notes == null`, the
    column reads back as `null`. (Verified by the Tier 3 tests for `AC2.1`, `AC2.2` and `AC2.4` — the schema has
    no separate test.)
- **XML docs**: see the cross-cutting bullet under `## Impact / Affected areas`.

## Application

### Commands
- **AddStage** (Add, existing — extended) — `R2`
  - Properties: existing eight, plus `string? Notes` appended as the **last** positional parameter
    (`AddStage(int RouteId, int Number, string Name, string StartPoint, string EndPoint, decimal DistanceKm,
    decimal ElevationDifferenceM, Difficulty Difficulty, string? Notes)`).
  - Result: `OneOf<AddStageResult, ValidationFailed, NotFound>` — unchanged
  - Validation: add `RuleFor(x => x.Notes).MaximumLength(2000);` (a `null` value passes; no `NotEmpty`,
    no `.When(...)`). All existing rules unchanged.
  - Handler: object initializer gains `Notes = command.Notes` — stored verbatim, no trimming.
  - Acceptance:
    - `AC2.1` Given route 1 exists, when `AddStage(1, 3, "Vorden - Zutphen", "Vorden", "Zutphen", 17.5m, 40m,
      Difficulty.Easy, "Mooie etappe langs de Berkel")` is handled, then a stage is persisted with that note
      and `AddStageResult(Id)` is returned.
    - `AC2.2` Given route 1 exists, when the same command is handled with `Notes = null`, then the stage is
      persisted with `Notes == null` and the result is `AddStageResult(Id)`.
    - `AC2.3` Given route 1 exists, when `POST /stages` carries a 2001-character `notes`, then the response is
      `400` problem+json with an error on `Notes` and no stage is created. (Validators are `internal` and unit
      tests stub `IValidator<T>`, so this is a Tier 0 scenario only — see `## Tests`.)
    - `AC2.4` Given route 1 exists, when the command carries a note of exactly 2000 characters, then the stage
      is persisted and the note reads back from the database unchanged (accepted boundary — guards against the
      validator/column drift the mirror has).

- **UpdateStage** (Update, existing — extended) — `R3`
  - Properties: existing nine, plus `string? Notes` appended as the **last** positional parameter.
  - Result: `OneOf<UpdateStageResult, ValidationFailed, NotFound>` — unchanged
  - Validation: add `RuleFor(x => x.Notes).MaximumLength(2000);`. All existing rules unchanged.
  - Handler: assignment block gains `stage.Notes = command.Notes;` — unconditional, so `null` clears an
    existing note (full-replace semantics, see `R6`).
  - Acceptance:
    - `AC3.1` Given stage 5 exists with `Notes == null`, when `UpdateStage` for id 5 carries
      `Notes = "Herberg onderweg gesloten op maandag"`, then the stage is saved with that note and
      `UpdateStageResult(5)` is returned.
    - `AC3.2` Given stage 5 exists with `Notes == "oud"`, when `UpdateStage` for id 5 carries `Notes = null`,
      then the stage is saved with `Notes == null`.
    - `AC3.3` Given stage 5 exists, when `PUT /stages/5` carries a 2001-character `notes`, then the response is
      `400` problem+json with an error on `Notes` and the stage is not modified. (Tier 0 only, same reason as
      `AC2.3`.)

### Queries
- **GetStage** (Single, existing — extended) — `R4`
  - Filters: `Id` — unchanged
  - Result: `OneOf<StageDto, NotFound>` — unchanged
  - DTO fields: `StageDto` (declared in `GetStage.cs`, shared by the feature) gains `string? Notes` appended
    as the **last** positional parameter; `GetStageHandler` passes `stage.Notes`.
  - Acceptance:
    - `AC4.1` Given stage 7 exists with `Notes == "Let op: veerpont vaart niet in de winter"`, when
      `GetStage(7)` is handled, then the DTO carries that note.
    - `AC4.2` Given stage 8 exists with `Notes == null`, when `GetStage(8)` is handled, then the DTO carries
      `Notes == null`.

- **GetStagesByRoute** (By-parent, existing — extended) — `R5`
  - Filters: `RouteId` — unchanged
  - Result: `IReadOnlyList<StageDto>` — never fails; unchanged
  - DTO fields: same `StageDto`; the in-query projection `Select(s => new StageDto(..., s.Notes))` gains the
    argument. Ordering by `Number` unchanged.
  - Acceptance:
    - `AC5.1` Given route 1 has stage A with a note and stage B without, when `GET /routes/1/stages` is
      called, then both stages are returned in `Number` order, A with its `notes` string and B with
      `notes: null`.

## Api
- **Controller**: `StagesController` in `src/HikingLog.Api/Stages/` — existing; no new actions, no route changes
- **Endpoints** (existing; payload extended):
  - `POST stages` → AddStageHandler (`R2`) — 201 / 400 / 404 (unchanged); the 201 body echoes `notes` from
    the request (the handler stores it verbatim, so echoing is correct per the controller rule).
  - `PUT stages/{id:int}` → UpdateStageHandler (`R3`) — 200 / 400 / 404 (unchanged); 200 body echoes `notes`.
  - `GET stages/{id:int}` → GetStageHandler (`R4`) — 200 / 404 (unchanged); body includes `notes`.
  - `GET routes/{routeId:int}/stages` → GetStagesByRouteHandler (`R5`) — 200 (unchanged); each item includes `notes`.
  - `DELETE stages/{id:int}` — unchanged, not part of this spec.
- **Models** (`Api/Stages/Models.cs`): `CreateStageRequest`, `UpdateStageRequest`, `StageResponse` each gain
  `string? Notes` appended last. Over-long `notes` surfaces as `400` problem+json via
  `ValidationProblem(v.ToModelStateDictionary())` — no new error shape.
- **Mapping** (`Api/Stages/MappingExtensions.cs`): `ToCommand()`, `ToCommand(int id)`, `ToResponse()` pass
  `Notes` through. The two inline `new StageResponse(...)` constructions in `StagesController.Create` and
  `Update` pass `request.Notes`.

## Business rules
1. `R6` `Notes` is stored **verbatim** (no trim, no normalisation, empty string is kept as empty string, not
   coerced to `null`) and `PUT` **replaces** it: an omitted or `null` `notes` clears an existing note — enforced in
   the Application handlers (`AddStageHandler`, `UpdateStageHandler`); the Api layer echoes the request, which
   is only valid because nothing is transformed.
   - `AC6.1` Given stage 5 has `Notes == "x"`, when `PUT /stages/5` is sent with a valid body that omits
     `notes`, then the response is 200 with `notes: null` and a subsequent `GET /stages/5` returns `notes: null`.
   - `AC6.2` Given stage 5 has `Notes == "x"`, when `PUT /stages/5` is sent with `"notes": ""`, then the
     response is 200 with `notes: ""` and a subsequent `GET /stages/5` returns `notes: ""` — the empty string is
     kept, not coerced to `null`.

## Tests
- **Unit** (`tests/HikingLog.Application.Tests/Stages/`, xUnit + NSubstitute):
  - All existing `AddStage` / `UpdateStage` constructions updated for the new positional parameter (compile fix).
  - `AddStageHandlerTests`: `Handle_WhenValid_CallsSaveChangesAndReturnsResult` asserts the added entity carries
    the note (`AC2.1`); new `Handle_WhenNotesIsNull_PersistsNullNotes` (`AC2.2`).
  - `UpdateStageHandlerTests`: `Handle_WhenValid_UpdatesStageAndReturnsResult` asserts `stage.Notes` updated
    (`AC3.1`); new `Handle_WhenNotesIsNull_ClearsNotes` (`AC3.2`).
  - `GetStageHandlerTests`: `Handle_WhenStageExists_ReturnsStageDto` asserts `Notes` mapped (`AC4.1`); new
    `Handle_WhenStageHasNoNotes_ReturnsNullNotes` (`AC4.2`).
  - **No unit test for the length rule.** Handler tests stub `IValidator<T>` and validators are `internal`
    without `InternalsVisibleTo`, so a "too long" unit test would only re-prove the existing
    `Handle_WhenValidationFails_ReturnsValidationFailed`. `AC2.3` and `AC3.3` are covered at Tier 0 only.
  - `GetStagesByRouteHandler` cannot be unit-tested (`ToListAsync` on a substituted `DbSet`); `AC5.1` is Tier 0.
- **Tier 0** (`tests/HikingLog.IntegrationTests/Stages/Endpoints/`):
  - `StageFaker` gains `Notes` (pass `null`, mirroring `HikeLogFaker`; tests that need a note set it with
    `with { Notes = "…" }`). Its `CreateStageRequest` signature change is absorbed by the HikeLogs endpoint tests
    that call `new StageFaker(routeId)` — no behavioural change there.
  - `PostStageTests`: new `PostStage_WhenNotesProvided_Returns201AndEchoesNotes` (`AC2.1`); new
    `PostStage_WhenNotesTooLong_Returns400` (`AC2.3`); existing `PostStage_WhenValid_Returns201` covers `AC2.2`
    (faker sends `null`) — assert `notes` is `null` in the body.
  - `PutStageTests`: new `PutStage_WhenNotesProvided_Returns200AndEchoesNotes` (`AC3.1`); new
    `PutStage_WhenNotesOmitted_ClearsNotes` (`AC3.2`, `AC6.1`); new `PutStage_WhenNotesIsEmptyString_KeepsEmptyString`
    (`AC6.2`); new `PutStage_WhenNotesTooLong_Returns400` (`AC3.3`).
  - `GetStageTests`: new `GetStage_WhenStageHasNotes_ReturnsNotes` (`AC4.1`); existing
    `GetStage_WhenExists_Returns200` asserts `notes` is `null` (`AC4.2`).
  - `GetStagesByRouteTests`: new `GetStagesByRoute_WhenStagesHaveMixedNotes_ReturnsNotesPerStage` — one stage
    with a note and one with `null`, both returned in `Number` order with the right `notes` value (`AC5.1`).
    The existing ordering test stays as it is.
- **Tier 3** (`tests/HikingLog.IntegrationTests/Stages/Consumers/AddStageHandlerTests.cs`):
  - New `Handle_WhenNotesProvided_PersistsNotes` reads the row back with `FindAsync` and asserts the note
    round-trips (`AC2.1`); new `Handle_WhenNotesIsNull_PersistsNull` asserts the column stays `NULL` (`AC2.2`);
    new `Handle_WhenNotesIs2000Chars_PersistsNotes` saves a note of exactly 2000 characters and reads it back
    unchanged (`AC2.4`), exercising the real validator (resolved from DI) and the real column width. The
    existing `Handle_WhenValid_PersistsStage` is left unchanged.
  - Together these three are the verification of `R1` / `AC1.1`.

## Open questions
None for this spec.

**Follow-up outside this spec** (owner: the user files it as a GitHub issue before `spec-close` archives this
spec, so the observation survives): `HikeLog.Notes` is
`HasMaxLength(2000)` in `HikeLogConfiguration` but `MaximumLength(1000)` in `AddHikeLogValidator` and
`UpdateHikeLogValidator`, contradicting the persistence rule that the two numbers stay in sync. Decide 1000 or
2000 for HikeLog and align both sides.

## Implementation tasks
- [ ] Domain: add `Stage.Notes` (`string?`) + `StageConfiguration` `HasMaxLength(2000)`
- [ ] Migration: `AddNotesToStage`
- [ ] Command: AddStage — record, validator rule, handler copy
- [ ] Command: UpdateStage — record, validator rule, handler assignment
- [ ] Query: GetStage — `StageDto` + handler
- [ ] Query: GetStagesByRoute — projection
- [ ] Api: `Models.cs`, `MappingExtensions.cs`, the two inline `StageResponse` constructions in `StagesController`
- [ ] DI registration — no change (verify build)
- [ ] Tests: unit updates + new scenarios, `StageFaker`, Tier 0, Tier 3

## Review Notes

_Reviewed on 2026-09-17._

### Blockers
None.

### Warnings
1. `## Business rules` / `## Tests` — `R6` names `AddStageHandler` and `UpdateStageHandler` as the enforcement
   point for verbatim storage (empty string kept, not coerced to `null`), but `AC6.1` and `AC6.2` are covered at
   Tier 0 only. No Application-layer test pins the behaviour where the spec says it lives, so a later handler
   change that trims or null-coerces would be caught only by the HTTP tests. Consider an assertion in
   `UpdateStageHandlerTests` for the empty-string case.
2. `## Impact / Affected areas` / `## Open questions` — the spec sets `Notes` to 2000 on both sides while the
   mirrored `HikeLog.Notes` stays at `HasMaxLength(2000)` / `MaximumLength(1000)` (confirmed in
   `src/HikingLog.Infrastructure/Data/Configurations/HikeLogConfiguration.cs` vs `AddHikeLog.cs` /
   `UpdateHikeLog.cs`). `## Open questions` says "None for this spec" and the follow-up depends on a GitHub
   issue that does not exist yet, so the divergence can survive `spec-close` unrecorded. Record the issue number
   in the spec once filed, or have `spec-close` harvest the observation explicitly.

### Suggestions
1. `## Tests` (Tier 0) — `PostStage_WhenNotesProvided_Returns201AndEchoesNotes` and
   `PutStage_WhenNotesProvided_Returns200AndEchoesNotes` mix the two naming forms in
   `backend-integration-testing.md` (`…_Returns<StatusCode>` for status tests,
   `…_When<Condition>_<Outcome>` for behavioural ones). Pick one form per test.
2. `## Tests` (Tier 0) — state that `PutStage_WhenNotesOmitted_ClearsNotes` sends a body that literally omits the
   `notes` property (an anonymous object), since a `StageFaker`-derived request would send `"notes": null`. The
   two are equivalent under the planned model binding, but only the former tests what the name claims.
3. `## Tests` (Tier 3) — a round-trip/clear test on `UpdateStageHandler` would exercise the real column on the
   update path, which is otherwise only covered through HTTP. Above the rule's minimum (Add handler only), hence
   optional.
