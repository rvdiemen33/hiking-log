---
name: spec-create
description: >
  Spec-driven development intake for HikingLog. Interviews the user about a new feature, grounds it in
  a read-only exploration of the solution, writes a structured spec to docs/specs/ with status: draft,
  then auto-refines that draft by folding the model-pinned spec-reviewer agent's advisory findings back
  into the spec, so the human opens an already-cleaned draft. When that draft comes out clean — no
  surviving TO CONFIRM markers — it also runs the spec-review gate in the same run, so the human opens
  a spec that has already been through review. Writes no production code and scaffolds
  nothing. Use when the user wants to plan or design a feature before building it: "create a spec for
  X", "let's design X first", "plan the implementation of X", "I have an idea for a new feature",
  "/spec-create". Do NOT use when the user wants to start building right away (use ship-slice or the
  task-skills), and do NOT use to review an existing spec (use spec-review) or to implement one
  (use spec-implement).
---

# spec-create — HikingLog feature spec intake

Interview the user about a feature and write a structured spec to `docs/specs/`, grounded in a
**read-only** exploration of the solution so the spec mirrors the conventions already in `src/`,
avoids collisions, and records what the change touches. No code is generated and nothing is
scaffolded. The skill closes by running `spec-reviewer` in **advisory** mode and folding its findings
back into the spec automatically, and then — **only when no `TO CONFIRM:` marker survives** — invokes
the `spec-review` skill so the draft goes through the gate in the same run. The gate may still return
blockers of its own; what the chain saves is the extra turn, not the verdict. A draft with surviving markers
stops at `draft`: the gate would fail on those markers by design, and only the user can resolve them.
The gate itself still belongs to `spec-review`; this skill invokes it, it never flips `status` by hand.

Read `.claude/functional-plan.md` before you start — the domain model, endpoints and business rules
there are the baseline every spec extends or refines. Read `.claude/rules/backend/backend-cqrs.md` and
`backend-controllers.md` too: their tables are the ground truth for the `OneOf` result contract per
operation kind and the status codes per verb, which Phase 3 derives without asking. The mirrored
reference slice only confirms them, and it may not contain every kind the new feature needs.

---

## How this skill works

The user knows **what they want**; the codebase knows **how it must look**. So do not interrogate
them field by field:

1. **Understand** the request in plain language (Phase 1).
2. **Explore** the solution to see what exists and what the change touches (Phase 2).
3. **Derive** as much of the spec as the codebase allows — mirror the nearest existing slice (Phase 3).
4. **Ask only the genuine unclarities** (Phase 4). Anything you had to guess, and anything only the
   user can decide, gets the canonical marker **`TO CONFIRM: {question}`** — it is grep-able, and
   `spec-reviewer` escalates every surviving marker to a blocker, so an unconfirmed value can never
   slip through the gate as prose. When someone other than the user has to answer (an external
   party, a data owner), append `(owner: {who})` so the reviewer can name them with the blocker.
5. **Verify** the assembled understanding with the user before writing (Phase 5).
6. **Commit the draft** the moment it is written and refined (Phase 8) — before any code exists.
7. **Run the gate when the draft is clean** (Phase 9) — invoke `spec-review` only when no
   `TO CONFIRM:` marker survives, so a clean draft reaches `reviewed` in one run.

The phases list what a complete spec needs. Treat them as **what to populate, derived-first** — a
checklist for you, not a questionnaire to read out.

---

## Phase 1 — Understand the request

One message, plain language, no stack vocabulary:

> "Let's capture the spec. In your own words:
>
> 1. **What do you want to happen, and why?** What is the feature, which problem does it solve, and
>    which data does it involve?
> 2. **Issue number** — the GitHub issue, if there is one (e.g. `17`), ideally created with the
>    *User story* form so it already passed the Definition of Ready. Optional; say 'none'.
> 3. **Rough scope** — a new entity, or an addition to Routes / Stages / HikeLogs / Statistics?
>    A rough answer is fine; the exploration confirms it."

Wait for the answer and confirm the intent you understood before exploring.

---

## Phase 2 — Explore the solution (read-only)

Dispatch read-only exploration **agents in parallel — one message, two `Agent` calls,
`subagent_type: Explore`**. They only read; they write no code and run no skills.

**Domain and persistence explorer** — ask it to report, with exact file paths:

- Does an entity named `{Entity}` already exist in `src/HikingLog.Domain/Entities/`? (**collision**)
- Does a feature folder `{Feature}` already exist under `src/HikingLog.Application/`? (**collision**)
- The nearest existing entity to mirror, its Fluent configuration in
  `src/HikingLog.Infrastructure/Data/Configurations/`, and the conventions it uses (max lengths,
  decimal precision, which side owns the relationship, delete behaviour).
- The `DbSet` style on `HikingLogDbContext` and on `IHikingLogDataContext`.
- Existing migrations, and what `DataSeeder` currently seeds.
- Enums in `src/HikingLog.Domain/Enums/` the feature could reuse.

**Application, Api and tests explorer** — ask it to report, with exact file paths:

- The nearest existing slice to mirror: the commands and queries in
  `src/HikingLog.Application/<Feature>/`, their `OneOf` result contracts, their validators.
- The matching controller in `src/HikingLog.Api/<Feature>/`: route templates, verbs, status codes,
  request/response models and mapping extensions.
- The registrations in `src/HikingLog.Application/Extensions/ServiceCollectionExtensions.cs`.
- Which unit tests, Tier 0 and Tier 3 tests the mirrored feature has, and which fakers exist in
  `tests/HikingLog.IntegrationTests`.
- Anything the new feature would **affect**: shared DTOs, statistics aggregates, existing endpoints
  whose behaviour changes.

Summarise the findings to the user — **collisions first, as warnings** — then the reference slice and
the defaults you can now pre-fill.

If a collision is found (the entity or feature folder already exists), **stop and ask** how to
proceed — extend the existing feature, rename, or abort — before continuing. Record the outcome in
the spec's Impact section; `spec-implement` reads it to decide new-slice versus extend, and passes it
on as the skip-list for layers that already exist.

---

## Phase 3 — Derive the proposed spec

Using the request, the exploration findings and `.claude/functional-plan.md`, assemble a proposed
spec **before asking anything further**. Mirror the nearest reference slice for structure and naming:

- **Domain**: entity + feature folder, a plausible property set with C# types, nullability, string
  max lengths and decimal precision, relationships (FK + navigation; the **parent** configuration
  owns the relationship, exactly once on the "one" side), migration name (`Add{Feature}` /
  `Add{Property}To{Entity}`).
- **Application**: the command and query set the reference slice uses, each with the `OneOf` contract
  the CQRS rule prescribes for its kind, validation rules per command, DTO fields per query.
- **Api**: route templates, verbs and the status codes the controller rule prescribes per verb.
- **Tests**: the unit, Tier 0 and Tier 3 tests the slice owes.

**Number the requirements.** Every requirement gets an `R{n}` in spec order — the entity and its
persistence (when there is a `## Domain` section), then each command, each query, each business rule;
endpoints share the id of their handler. Every acceptance scenario gets `AC{n}.{m}` under its
requirement's `n`. `spec-verifier` traces the delivered code and tests back to these ids; they are
stable once approved — `docs/specs/README.md → Conventions` owns the renumbering rule.

Tag every value you had to guess with `TO CONFIRM: {question}`. Those become the targeted questions in
Phase 4; everything else is presented for confirmation, not asked cold.

---

## Phase 4 — Confirm and fill the gaps

Present the derived spec per section and ask only what the codebase cannot answer. Use
AskUserQuestion (one call, up to 4 questions) for the discrete choices, free text for the rest.

**Domain** (skip when the feature adds no entity and changes no schema):

> "Entity: name (singular PascalCase), feature folder (plural), properties (name, C# type,
> required/optional, max length for strings, precision for decimals), relationships, extra indexes,
> seed data, migration name — here is what I derived; correct what is wrong."

**Commands** — per command: name (verb + entity), kind (Add / Update / Delete), carried properties,
validation rules, and **1–3 acceptance scenarios** (Given/When/Then with real values) you derived for
confirmation. The `OneOf` contract follows from the kind — derive it, never ask.

**Queries** — per query: name, kind (single / collection / by-parent / aggregate), filters, DTO
fields, and **1–2 acceptance scenarios** (what a filter returns and excludes, what the empty result
looks like). Collection queries return `IReadOnlyList<TDto>` and never fail — derive that too.

**Api** — route template and verb per operation; status codes follow from the verb.

**Business rules** — anything beyond field validation (uniqueness, cross-entity constraints,
ordering), and where it is enforced.

**Open questions** — "Any unresolved decisions or external dependencies to flag? Say 'none' if not."

---

## Phase 5 — Verify with the user (confirmation gate)

Before writing, present the assembled spec back as a concise summary: intent, scope, derived domain /
application / api shape, impact and affected areas, and **every surviving `TO CONFIRM:` marker** (each
one becomes a blocker at review time — resolve now what the user can resolve). Ask:

> "This is what I understood — correct? Anything to change before I write the spec?"

**Wait for explicit confirmation.** Fold in corrections and re-confirm if they are material. Only
write once the user agrees.

---

## Phase 6 — Confirm the filename and write

Derive a kebab-case slug from the entity or feature name. The filename is
`docs/specs/{issue}-{slug}.md` when the user gave an issue number (e.g. `17-stage-notes.md`) and
`docs/specs/{slug}.md` when there is none. Suggest it and ask the user to confirm or adjust.

Create `docs/specs/` if it does not exist yet. Then write the file in exactly this format:

```markdown
---
status: draft
issue: {number, or omit the line entirely when there is none}
created: {TODAY_ISO_DATE}
---

# Feature: {Name}

## Overview
{Feature description from Phase 1 — verbatim, not paraphrased}

## Impact / Affected areas
_From codebase exploration on {TODAY_ISO_DATE}._
- **Reference slice mirrored**: {feature — path}
- **Existing code this feature touches**: {e.g. HikingLogDbContext, DataSeeder, RoutesController, shared DTOs — or "none identified"}
- **Layers that already exist** (spec-implement passes these on as the skip-list): {entity | Fluent config | DbSet | migration | Application slice | controller | DI — or "none"}
- **Collisions / conflicts**: {none | the collision and the resolution chosen}

## Domain
- **Entity**: {EntityName} (feature folder `{Feature}`) — `R1`
- **Properties**:
  - {Name}: {CSharpType} — {required|optional}{, max {N} chars}{, precision {p},{s}}
  _(repeat per property)_
- **Relationships**: {e.g. Route (1) → Stage (n): Stage carries FK RouteId + navigation Route; RouteConfiguration owns the relationship (HasMany/WithOne, cascade delete) | none}
- **Enums**: {new or reused enum values | none}
- **Indexes**: {beyond the PK | none}
- **Seed data**: {rows added to DataSeeder | none}
- **Migration name**: {MigrationName}

## Application

### Commands
- **{CommandName}** ({Add|Update|Delete}) — `R{n}`
  - Properties: {list with C# types}
  - Result: `{OneOf<...> contract}`
  - Validation: {rule per property, or "none — Delete has no validator"}
  - Acceptance:
    - `AC{n}.1` Given {precondition}, when {action}, then {observable outcome}
    _(1–3 scenarios per command, numbered AC{n}.1, AC{n}.2, …)_
_(repeat per command)_

### Queries
- **{QueryName}** ({Single|Collection|By-parent|Aggregate}) — `R{n}`
  - Filters: {list | none}
  - Result: `{contract}`
  - DTO fields: {list}
  - Acceptance:
    - `AC{n}.1` Given {rows on both sides of the filter}, when {GET …}, then {which rows, and which not}
    _(1–2 scenarios per query)_
_(repeat per query)_

## Api
- **Controller**: `{Feature}Controller` in `src/HikingLog.Api/{Feature}/`
- **Endpoints**:
  - `{VERB} {route template}` → {HandlerName} (`R{n}`) — {status codes}
  _(repeat per endpoint; the id is the handler's)_
- **Models**: {Create{Entity}Request, Update{Entity}Request, {Entity}Response}

## Business rules
1. `R{n}` {rule} — enforced in {layer}
_(repeat per rule, or "None beyond field validation.")_

## Tests
- **Unit**: {per handler — happy path, validation-failed, NotFound where applicable} — covers {AC ids}
- **Tier 0**: {per endpoint — one test per status code; behavioural tests per filter/by-parent route} — covers {AC ids}
- **Tier 3**: {per handler with database — at minimum the Add handler} — covers {AC ids}

## Open questions
{User-provided items, or "None."}

## Implementation tasks
- [ ] Domain entity + Fluent config + DbSet: {EntityName}
- [ ] Migration: {MigrationName}
{- [ ] Command: {CommandName} — per command}
{- [ ] Query: {QueryName} — per query}
- [ ] Api: {Feature}Controller + models + mapping
- [ ] DI registration
- [ ] Tests: unit + Tier 0 + Tier 3

## Review Notes
_Not yet reviewed. Run `spec-review` to review this spec._
```

Omit sections the feature genuinely does not touch (a spec that only adds a query has no `## Domain`
section and no migration task — its first requirement is then `R1` on the first command or query; ids
stay contiguous across the sections that remain). Keep `## Impact / Affected areas` regardless, and
write "none identified" where the feature is greenfield.

---

## Phase 7 — Automated review-and-refine pass

After writing, run `spec-reviewer` against the spec and **fold its findings back into the spec**.

1. Dispatch the agent (`Agent` tool, `subagent_type: spec-reviewer`) on the just-written file in
   **advisory** mode:

   > Review the spec at `docs/specs/{name}.md` in **advisory mode**.

   Advisory mode reports findings and changes nothing — no Review Notes, no `status` change. The
   `draft → reviewed` transition belongs exclusively to `spec-review`, which Phase 9 invokes when the
   refined draft carries no surviving marker. Never flip `status` yourself, in any phase.

   **Fallback if `spec-reviewer` is not in the registry** (which happens when its definition was
   created in this same session): spawn a read-only `Explore` agent instead, inlining the eight
   dimensions and the severity scale from `.claude/agents/spec-reviewer.md`, and say in your report
   that the fallback ran. Never skip the refine pass because the agent is missing.

2. **Process every finding into the spec body**, editing the section it belongs to. Do **not** append
   a findings list and do **not** touch the `## Review Notes` placeholder. Resolve each finding one
   of two ways:
   - **Derivable** — fold the concrete fix into the right section: add the missing validation rule
     (`NotEmpty`, `MaximumLength`, a range), the missing acceptance scenario, the missing test, the
     correct `OneOf` contract or status code, the missing `NotFound` arm for a child whose parent may
     be absent, the missing max length or decimal precision. Ground every value in the reference
     slice and the Phase 2 exploration — the same grounding the rest of the spec uses.
   - **Not derivable** — if the fix needs a decision only the user can make, **do not fabricate it**.
     Convert the finding into a precise `TO CONFIRM: {question}` marker in the exact section it
     concerns, with `(owner: {who})` when the answer has to come from someone other than the user.
     The finding still lands *in the spec*, and the surviving marker becomes a blocker at the gate —
     by design.

3. **Re-run the advisory reviewer once** to confirm the auto-fixable findings are gone, and process
   any new derivable finding the same way. Stop after at most two refine passes; whatever is still
   open is a genuine decision for the user, not noise to auto-clear.

4. **Grep the spec for `TO CONFIRM:`** and list every hit to the user as its own bullet.

---

## Phase 8 — Commit the draft

The spec is committed **before any code exists**, so the history proves the design predates the
implementation (see `docs/specs/README.md`). This is one of the spec-flow commits `CLAUDE.md`
exempts from "commit only when the user asks"; it never needs a confirmation.

1. `git branch --show-current` and pick the branch the spec lives on — the slice later shares it, so
   spec and implementation end up in one branch and one PR:
   - already on `feature/{slug}`: stay;
   - on `master`: `git checkout -b feature/{slug}`, or `git switch feature/{slug}` when
     `git branch --list feature/{slug}` shows the branch already exists (both are in the permission
     allow-list; plain `git checkout <branch>` and `git rev-parse` are not);
   - on any other branch (another feature, a fix branch): the draft never lands there — `spec-implement`
     and `ship-slice` only know `master` and `feature/{slug}`. Ask once (AskUserQuestion): create
     `feature/{slug}` from `master` now (`git checkout -b feature/{slug} master`; the untracked spec
     travels along, and so do any uncommitted changes, still uncommitted), or **stop** and leave the
     draft uncommitted so the user can switch branches and rerun Phase 8. Never commit on the other
     branch.
2. Stage **only the spec**: `git add docs/specs/{name}.md`. Never `git add -A` — the user may have
   unrelated work in the tree.
3. Commit: `docs(spec): add {slug} draft`. Do not push; the user decides when the branch goes up.

Do not report yet — Phase 9 decides what the report says.

---

## Phase 9 — Chain the gate when the draft is clean

A refined draft with no surviving `TO CONFIRM:` marker has nothing left that only the user can answer,
so the gate can run now instead of waiting a turn. A draft that still carries a marker must not go to
the gate: `spec-reviewer` escalates every marker to a blocker, so the run would spend a full review to
tell you what Phase 7 step 4 already listed.

1. **Did Phase 8 actually commit?** If step 1 there ended in *stop* — the draft is still uncommitted on
   a branch it must not land on — skip the gate entirely and go to route A. `spec-review` stages and
   commits on whatever branch is checked out, so chaining it now would put the spec exactly where
   Phase 8 just refused to put it.
2. Use the Phase 7 step 4 grep result — do not re-derive it.
3. **Markers survive** → skip the gate. `status` stays `draft`; report route A below.
4. **No markers** → invoke the `spec-review` skill (`Skill` tool, `skill: spec-review`) with the spec
   path as its argument. That skill owns the gate end to end: it dispatches `spec-reviewer` in gate
   mode, the agent appends `## Review Notes` and flips `status`, and the skill commits the result. Do
   not dispatch `spec-reviewer` yourself, do not edit the spec afterwards, and do not commit again —
   a second commit here would duplicate what `spec-review` step 4 already made.
5. Relay the gate's own verdict in the report. The gate can still find blockers the advisory passes
   did not (it judges the same eight dimensions but decides rather than advises); when it does,
   `status` stays `draft` and its blockers are the user's next action — report route C.

---

## Phase 10 — Report

Pick the route that matches what happened. When the user chose to stop in Phase 8 step 1, replace the
"committed on" clause with "**not committed** — switch to `master` or `feature/{slug}` and rerun
Phase 8", and use route A regardless of markers.

**Route A — markers survived, gate skipped:**

> "Spec written to `docs/specs/{name}.md` (`status: draft`), auto-refined against the spec-reviewer
> ({N} finding(s) resolved directly in the spec) and committed on `feature/{slug}`.
>
> **{N} item(s) need your decision** (surviving `TO CONFIRM` markers):
> - [{section}] {question}
>
> The review gate has **not** run — it would report these markers back as blockers.
>
> **Next steps:**
> 1. Resolve the `TO CONFIRM` items — edit the spec and replace each marker with the confirmed value.
> 2. Run `spec-review` — the gate that appends Review Notes and flips `status` to `reviewed` once no
>    blockers remain.
> 3. Set `status: approved` in the frontmatter once you are satisfied.
> 4. Run `spec-implement` to build the feature from the spec."

**Route B — gate ran, no blockers:**

> "Spec written to `docs/specs/{name}.md`, auto-refined against the spec-reviewer ({N} finding(s)
> resolved directly in the spec), then put through the review gate — **`status: reviewed`**, no
> blockers. Both commits are on `feature/{slug}`.
>
> {N} warning(s) and {N} suggestion(s) are recorded in `## Review Notes`:
> - [{severity}] {finding}
>
> No `TO CONFIRM` items are left.
>
> **Next steps:**
> 1. Read the spec and address the warnings you care about.
> 2. Set `status: approved` in the frontmatter — the one transition no skill makes for you.
> 3. Run `spec-implement` to build the feature from the spec."

**Route C — gate ran and found blockers:**

> "Spec written to `docs/specs/{name}.md`, auto-refined against the spec-reviewer ({N} finding(s)
> resolved directly in the spec) and committed on `feature/{slug}`. The review gate then found
> **{N} blocker(s)** — `status` stays `draft`:
>
> {numbered blockers}
>
> **Next steps:**
> 1. Resolve the blockers in the spec.
> 2. Run `spec-review` again.
> 3. Set `status: approved` once it comes back clean, then run `spec-implement`."

---

## Hard rules

- **Write nothing but the spec.** No entity, no handler, no controller, no test — the spec precedes
  the code, and `spec-implement` builds it.
- **Explore read-only.** The exploration agents are `Explore`; they never edit.
- **Never invent a value silently** — derive it from the codebase, or mark it `TO CONFIRM:`.
- **Commit only the spec, and only in Phase 8.** The draft commit is the one commit this skill owns;
  it stages nothing else and never pushes. The gate commit that may follow belongs to `spec-review` —
  let that skill make it, never repeat it here.
- **Never flip `status` yourself.** `draft → reviewed` is the `spec-review` gate's, and `approved`
  is the user's alone.
- Stay within this repository (`C:\github\hiking-log`) and add no packages.
