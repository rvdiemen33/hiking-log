---
name: ship-slice
description: >
  This skill should be used to deliver a HikingLog feature end to end WITH the full quality gate —
  build, then review-before-commit, then ship. Activate on an explicit `/ship-slice`, or when the user
  asks to "build, review and ship feature X", "run the full quality gate for X", "deliver X with code
  review", or otherwise wants the slice built AND reviewed AND committed in one orchestrated pass. It
  composes the existing pieces — the `slice-builder` agent (which it drives in a no-commit "composed
  mode"), the `backend-review` skill (five read-only lens agents), the `spec-verify` skill (code against
  the approved spec, when one drives the slice), the `review-claude-setup` skill — and
  runs the chain at the main-loop level (a subagent cannot spawn agents, so this cannot live inside
  `slice-builder`). Do NOT use this for
  a plain "add feature X" with no review/ship intent (that stays `slice-builder`), nor for a single
  layer (use the individual task-skills).
---

# Ship slice — HikingLog build → review → ship orchestrator

You deliver one complete feature **with quality gates**, by composing existing pieces — you do not
re-implement what they own. The chain is: **build (no commit) → review the working tree → (conditional)
skill review → completeness check → commit & push → docs sync → report.**

**Why this is a main-loop skill, not an agent or part of `slice-builder`:** a subagent cannot spawn
agents. `slice-builder` has no `Agent` tool, so it cannot run `backend-review` or `review-claude-setup`
(both fan out lens agents). Only the main loop has `Agent` + `Skill`, so the orchestration must run
here.

Read `CLAUDE.md`, `.claude/functional-plan.md`, and the path-scoped rules in `.claude/rules/backend/`
first for the architecture rules, domain model, and test conventions (the rules load automatically once
you touch matching files, but you orchestrate before that happens).

## Core principle: review BEFORE the commit

`slice-builder` builds and self-verifies but — when invoked by this skill — **does not commit**. The
review, the conditional Claude-setup review, and the completeness check all run on the **uncommitted slice**
(everything the working tree adds since the branch point — see "Determining the slice diff" below). Only
once every gate is green do you commit the reviewed slice as a single clean `feat(...)` commit. This is
the whole point: nothing known-bad ever reaches a commit.

## Honesty rules (non-negotiable)

- **"Unverified" is never "passed."** Integration tests need Docker (Testcontainers). If Docker is
  unavailable and you could not run them, say so explicitly — do not claim they passed.
- **Never commit or push known-red work** (failing build, format, or tests — Docker exception aside).
- **No hard iteration cap on the review loop.** Loop until it *converges* (a round produces no new
  confirmed findings). The runaway guard is *non-progress*: if the same finding keeps coming back
  without a fix resolving it, stop and report it — do not loop forever and do not invent a fixed count.

## Determining the slice diff (used by steps 3–5)

In composed mode `slice-builder` leaves the changes **uncommitted**, and a new slice is *mostly new
(untracked) files* — so pick the right git command:

- The baseline is `HEAD`. For a fresh run `HEAD` equals the branch's merge-base with `master` (clean tree,
  nothing committed yet in composed mode). In the resume path (step 1) `HEAD` may already carry prior
  commits — either way `git status --short` correctly shows the current working-tree additions, which are
  the slice.
- **List the touched files with `git status --short`** — it reports modified (`M`) *and* new/untracked
  (`??`) files. `git diff --name-only` alone omits untracked files, and a new slice is mostly new files,
  so never rely on it for the file list.
- **Do not stage file *content* during review.** Review fixes are written to the working tree with
  `Edit`/`Write`, and the review tools/agents read those files directly from the working tree. Never
  `git add <file>` actual content during the loop — a staged snapshot taken before a fix would be out of
  date the moment the fix lands. You stage content exactly once, at commit (step 6: `git add -A`), which
  captures every reviewed file including new ones.
- **`backend-review` needs no staging tricks.** Its working-tree scope resolves the file list from
  `git status --short` and its lenses read the files directly, so untracked slice files are reviewed
  as-is and every later fix is picked up on the next round.
- **Only if you additionally run the session-level `code-review` skill** (optional extra pass — see step 0):
  it gathers its scope with `git diff HEAD`, which **omits untracked files**. Run
  `git add -N <slice paths>` (intent-to-add) first: this is **not** staging content — it records a
  zero-length placeholder so `git diff` renders the new files' full content as additions, while the
  content stays in the working tree. It composes cleanly with the final `git add -A` at commit. Caveat:
  intent-to-add changes how `git stash` treats those files — prefer `Edit`/`Write` rollback regardless;
  if you must fully abort with stash (see the step-3 non-progress guard), plain `git stash` already
  covers paths given `git add -N`.

## What "confirmed" means and who applies fixes

You (the main loop) own the working tree — the review tools do not write to it. For each round:
- A finding is **confirmed** when it survived the review skill's own verification pass and carries
  severity **CRITICAL or SIGNIFICANT** — `backend-review`, `review-claude-setup` and `spec-verify` all
  drop unverifiable findings before reporting. MINOR findings are advisory: apply one only when the fix is
  trivial and obviously correct; otherwise list it as follow-up.
- Apply each confirmed finding yourself with `Edit`/`Write`, then re-verify (below).
- A `spec-verify` finding additionally carries a **`class`**. `mechanical` findings are confirmed and
  applied like any other; a `design` finding is **never patched in code** — it stops the review loop
  (step 3), because the spec, not the slice, is what needs changing.
- If a finding's correct fix is genuinely unclear and no user is reachable, **stop and report it** —
  never guess at a fix you don't understand.

## Progress tracking

This is a long, multi-step orchestration. **Create a task list with `TodoWrite` at the start** — one
task per step (pre-flight, build, review loop, conditional Claude-setup review, completeness check, commit &
push, docs sync, report) — and mark each `in_progress` when you start it and `completed` when it passes.
If the session offers no task-list tool, report step transitions in your replies instead; do not drop the
bookkeeping. The review loop may take several rounds; reflecting that in the task gives the user
live visibility into where the gate is. Keep it lightweight — it is for the user's visibility, not a
substitute for the per-step reporting below.

## Steps

### 0. Review mechanism (read once before the review loop)
The review steps use two **repo-owned** skills, both read-only and both defined under `.claude/skills/`:
- **`backend-review`** (step 3) — fans out the five `backend-reviewer-*` lens agents over the working tree,
  verifies and de-duplicates their findings against `CLAUDE.md` and `.claude/rules/backend/*.md`, and
  returns CRITICAL/SIGNIFICANT findings inline plus a report in `reviews/` (gitignored).
- **`review-claude-setup`** (step 4) — same shape over `.claude/**` and `CLAUDE.md`, with the six
  `claude-setup-reviewer-*` lenses.
- **`spec-verify`** (step 3, **only when the brief names a spec** — `spec-implement` always does) —
  dispatches the `spec-verifier` agent over the working tree against `docs/specs/<name>.md` and answers
  three questions: which `R` requirements are missing or contradicted, which `AC` scenarios have no
  test, what changed that the spec never mentions. Every finding is classified **mechanical** (fix the
  code) or **design** (reopen the spec); report in `reviews/`.

Invoke each with `Skill(...)`, stating the scope explicitly ("scope: working tree", or an explicit file
list for a scoped re-review). Neither skill edits anything — you apply the fixes.

Do **not** confuse them with the session-level `code-review` skill or the PR-based `/code-review` plugin:
the plugin needs an open PR and `gh` (not usable before a commit, and `gh` is not in the allow-list);
the session skill is a generic reviewer that knows nothing about this repo's rules. You may run the
session-level `code-review` skill as an **optional extra pass** when the user asked for maximum
thoroughness — it needs `git add -N` on untracked files first (see "Determining the slice diff").

Both skills carry their own fallback for a lens agent that is missing from the registry (which happens
when the definition was created in the same session) — they substitute a read-only `Explore` agent with
the lens checklist inlined, so a missing lens never blocks the gate.

**Fallback (only if a review skill itself cannot run at all):** review inline from the main loop — spawn
a few parallel read-only agents (`Explore`, or `general-purpose`) over the paths from
`git status --short`, one each for `CLAUDE.md`/rules adherence, bugs, and spec adherence; confirm each
finding with one independent skeptic agent before applying. Never block the pipeline because a review
tool is unavailable — but say in the report which mechanism ran.

### 1. Pre-flight
- **Confirm scope first.** Establish the feature name and what to build from the user's request,
  reconciled with `.claude/functional-plan.md`. If neither the request nor the plan resolves what to
  ship, **stop and ask the user** — you run at the main loop and can ask interactively (unlike
  `slice-builder`). Never invent scope. **If the caller supplied an approved spec** (`spec-implement`
  does), that spec *is* the scope and the skip-list: take it as authoritative, do not re-derive it, and
  do not ask the user to restate what the spec already settled.
- **Inventory which layers already exist, and trim the brief accordingly.** A feature is often
  partially built — e.g. the entity, its Fluent config, the `DbSet`, and the migration may already be in
  place while the Application/Api layers are not. Before spawning `slice-builder`, check the repo for
  each layer of *this* feature: entity in `HikingLog.Domain/Entities/`, config in
  `Infrastructure/Data/Configurations/`, `DbSet` on `HikingLogDbContext` + `IHikingLogDataContext`, an
  existing migration, the `Application/<Feature>/` slice, the `Api/<Feature>/` controller, and the DI
  registrations. **Explicitly tell `slice-builder` in the brief which already-built layers to SKIP** (by
  name — e.g. "the entity, EF config, DbSet and migration already exist; skip `domain-entity` and
  `dotnet-ef-migration`"). This prevents it from re-running `domain-entity` (which clobbers the entity)
  or `dotnet-ef-migration` (which creates a spurious empty migration).
- **Detect a prior interrupted run.** If the working tree is *not* clean, inspect it: if it already
  holds this feature's files (a previous run got partway), ask the user whether to **resume from step 3**
  (skip the build) or **discard and restart**. If `HEAD` already has a `feat(...)` commit for this
  feature, confirm with the user before continuing. Do not silently overwrite prior work.
- **Require a clean working tree** for a fresh run (`git status` clean) so the slice diff is *only* this
  slice. If dirty for unrelated reasons, stop and report (or have the user stash).
- File locks: a running `HikingLog.AppHost` / `HikingLog.Api` holds locks on the build output and makes
  the build fail. There is no process-listing command in the allow-list, so handle this **reactively**:
  if the build fails with a file-lock / "being used by another process" error, stop and ask the user to
  close the running app, then retry — do not assume a real compile error.

### 2. Build (no commit)
**Hard branch gate first:** run `git branch --show-current`. You MUST be on a `feature/<...>` branch
before spawning `slice-builder`. If still on `master`, create the branch (`git checkout -b
feature/<short-description>`) and re-confirm — composed mode tells `slice-builder` *not* to create a
branch, so an un-branched run would write straight to `master`.

Then spawn `slice-builder` with the feature brief. The brief **must contain the exact phrase
`composed mode — do NOT commit`** — the agreed trigger that puts `slice-builder` into composed mode
(build + self-verify only, leave changes uncommitted, do not create/switch branches). Also restate it in
plain words. Do not duplicate its verification here.

Handle its result:
- **Green** → proceed to step 3.
- **Red or unverified** (build or unit tests not green — Docker exception for integration tests aside) →
  **do not review a broken tree.** Read its report, name the failing layer, and stop and report. (You
  may, if the fix is obvious and isolated, re-invoke the responsible task-skill to complete that one
  layer and re-verify — but never proceed to review until fully green.)

### 3. Review loop (the core — runs before any commit)
Operate on the slice diff (see "Determining the slice diff"). Each round:
1. Run the **`backend-review` skill** with scope **working tree** (first round) — or, on a scoped
   re-review, with the explicit list of files the previous round's fixes touched. Apply only
   **confirmed** findings (see "What 'confirmed' means") with `Edit`/`Write`. **Scale the extra effort to
   the slice's size and novelty:** a small slice that mirrors an existing one (e.g. another CRUD feature
   built from the same task-skills as Routes/Stages) needs nothing beyond `backend-review`; for a large,
   novel, or architecturally unusual slice — or when the user asked to be thorough / "audit" — add the
   optional session-level `code-review` pass described in step 0.
   **When the caller supplied a spec**, run the **`spec-verify` skill** in the same round (scope
   working tree, the spec path from the brief, `round: {n}`). Apply its confirmed **mechanical**
   findings: a missing requirement by re-invoking the responsible task-skill (`domain-entity`,
   `dotnet-ef-migration`, `add-command`, `add-query`, `api-endpoint`, `register-di`,
   `integration-test`) or, when the gap is isolated, with `Edit`/`Write`; an untested scenario by
   writing the test. A confirmed **design** finding **stops the loop** — the fix belongs in the spec,
   not the slice, and the decision belongs to the human. You observed it and hold the spec path, so
   **reopen the spec yourself**: set `status: draft`, append the finding verbatim with its `SV-` id
   under `## Open questions`, and commit **only the spec** — `docs(spec): reopen {slug} — {one-line
   reason}` with the trailer `Reopens-Spec: docs/specs/{name}.md` (`docs/specs/README.md → Reopening a
   spec`). Leave the slice uncommitted in the working tree as the evidence, do not patch around the
   finding, then stop and report. The human resolves the spec, runs `spec-review`, sets `approved`,
   and the build resumes on the same branch.
2. **Re-verify after every round** at the chosen safety level: the `/check` sequence (`dotnet build`,
   `dotnet format --verify-no-changes`, `dotnet test tests/HikingLog.Application.Tests`,
   `dotnet test tests/HikingLog.Api.Tests`) **plus the integration tests, which `/check` does NOT
   include** — run `dotnet test tests/HikingLog.IntegrationTests` (Docker) separately each round. This
   is full verification each round, not a cheap subset. If Docker is unavailable, the integration tests
   are *unverified*, not passed (see Honesty rules).

Repeat **until convergence** — stop as soon as a round yields no new confirmed findings from either
skill (and, with a spec, `spec-verify` reports full `R`/`AC` coverage).

**Scope of the re-review vs. the re-verify — they differ.** The re-*verify* in point 2 is always full
(build + format + unit + integration), never a subset — a fix anywhere can break anything. The
re-*review* in point 1 may be **scoped**: when the prior round's confirmed fixes were isolated to a few
files (e.g. only test files, with the production code already reported clean), the next round may review
just those changed files — a focused pass confirming the fixes introduced no new defect — rather than
re-running the full multi-angle fan-out over already-clean code. Use the full fan-out again only when a
fix touched production code broadly or you have reason to think it shifted behaviour elsewhere. A scoped
round that yields no new findings still counts as convergence. **Guard: scoping is only allowed after at
least one full fan-out pass has covered the whole slice** — never scope from the first round, or a
production-code defect the first pass would have caught could go unreviewed.

Non-progress
guard: if a finding keeps reappearing without a fix resolving it, **revert the last failed fix** so the
tree is back in its last verified-green state. Do this with `Edit`/`Write`: for a tracked file
`git diff HEAD <file>` shows what changed, but the new (untracked) files that make up most of a slice
never existed at `HEAD`, so `git diff` cannot recover their prior state — `Read` a file before you edit
it if you may need to roll it back. **Prefer `Edit`/`Write` rollback over stash** — it is precise and
unaffected by index state. `git restore`/`git reset` are not in the allow-list. Stash is only for a
*full* abort and its exact form depends on whether you ran `git add -N` (only needed for the optional
session-level pass): once intent-to-add is applied, those paths are tracked-as-empty, so plain `git stash`
already covers them; otherwise use `git stash --include-untracked` so the untracked slice files are
included. Either way stash discards everything, so
reserve it for a full abort. Then stop and report —
including the finding text, the file/line it references, the fixes you attempted, and why they failed.

### 4. Conditional Claude-setup review (before commit)
Only if the slice changed a `.claude/skills/**`, `.claude/agents/**`, `.claude/rules/**`,
`.claude/commands/**`, or instruction file (`CLAUDE.md`, `.claude/*.md`, `.claude/settings.json`) — detect
via `git status --short` (includes untracked files). A normal slice touches none of these, so skip it then.

If it did: run the **`review-claude-setup` skill** with scope **working tree**, apply confirmed fixes
(CRITICAL/SIGNIFICANT), and repeat until clean (same convergence rule as step 3). Re-verify with the
`/check` sequence — these changes are to skill/doc files that do not affect compiled code, so the
integration tests are not required here; run them only if a fix also touched `src/` or `tests/`.

### 5. Completeness check (before commit)
Spawn a read-only agent (`Explore`, or `general-purpose`) that compares **only the just-built feature**
against its corresponding section in `.claude/functional-plan.md`, not the whole plan (otherwise it will
always report the features that are deliberately not built yet). Ask: are all endpoints of *this* feature
present? Are all business rules of *this* feature covered? Report any gaps as follow-up — do not build
them unprompted.

**When the caller supplied a spec** (`spec-implement` passes `docs/specs/<name>.md` as the authoritative
scope document), the last clean `spec-verify` round of step 3 **is** the completeness check against the
spec — its coverage line (`R x/y implemented · AC x/y tested`) is the evidence; do not spawn a second
agent to repeat it. Still compare against the feature's plan section **if one exists**; a spec-driven
feature may be new to the plan, and an absent plan section is expected, not a gap.

### 6. Commit & push
Only now, with every gate green. **Stage the full reviewed slice first — `git add -A`** — so every new
file *and* every review fix lands in the commit (the working tree, not the index, holds the fixes until
now). Then commit as a single clean `feat(...)` commit — the review fixes are already folded in, so there
are no separate `fix` commits; a single commit keeps the feature's history atomic and ships exactly the
reviewed state. Then push, choosing by upstream state (check `git status`: "no upstream branch" → new;
"up to date with"/"ahead of origin/…" → existing):
- new branch (no upstream yet): `git push -u origin HEAD`;
- existing upstream: `git push`.

### 7. Docs sync
`.claude/functional-plan.md` is a static spec with **no status field** — do **not** edit its spec tables
or invent per-row "done" markup. Instead, record delivery additively in a **`## Delivery status`**
section (create it once, just above `## Possible extensions`, if absent) with one checklist line per
delivered feature.

Reference the **feature branch**, not a PR number — this step runs *before* step 8, and the PR is the
user's to open afterwards, so no PR number exists yet. Do not write a `(PR #N)` placeholder you cannot
fill. Example:
`- [x] HikeLogs — entity (pre-existing), CRUD, ?year filter, by-stage query (branch \`feature/hikelogs-slice\`)`.
The user can backfill the PR number when they open the PR if they wish.

The completeness check (step 5) decides the branch — gaps in *other*, not-yet-built features are expected
and do not block this sync; only an incomplete *this* feature does:
- **This feature fully covered** → add the checklist line, then `git commit` it as `docs(plan): mark
  <feature> delivered`, and **`git push`** so the docs commit reaches the same remote branch as the
  feature commit (step 6's push does not include this later commit).
- **Gaps in this feature** → report them and leave the plan unchanged (no docs commit).

### 8. Final report (PR stays the user's call)
Do not run `gh pr create` yourself — `gh` is intentionally not in the allow-list and opening a PR is the
user's outward-facing call. Report:
- what was built (entity, commands/queries, endpoints, tests);
- verification results (and explicitly flag integration tests as unverified if Docker was unavailable);
- with a spec: the final `spec-verify` coverage line and the number of mechanical findings applied;
- review fixes applied;
- any completeness gaps as follow-up;
- the ready-to-run command:

```
gh pr create --title "<slice>" --body "<which layers and operations the slice covers>"
```

## Guardrails
- Stay within this repository (`C:\github\hiking-log`). Do not add NuGet packages without an explicit
  user request.
- This skill orchestrates; it does not hand-write slice code — `slice-builder` and the task-skills own
  that. Your edits here are limited to applying confirmed review fixes and the docs sync.
- If a gate cannot be evaluated (e.g. Docker down) or a step returns ambiguous output, stop and report
  rather than guessing or claiming success.
