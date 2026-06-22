---
name: ship-slice
description: >
  This skill should be used to deliver a HikingLog feature end to end WITH the full quality gate —
  build, then review-before-commit, then ship. Activate on an explicit `/ship-slice`, or when the user
  asks to "build, review and ship feature X", "run the full quality gate for X", "deliver X with code
  review", or otherwise wants the slice built AND reviewed AND committed in one orchestrated pass. It
  composes the existing pieces — the `slice-builder` agent, the `code-review` skill, the `skill-reviewer`
  agent — and runs the chain at the main-loop level (a subagent cannot spawn agents, so this cannot live
  inside `slice-builder`). Do NOT use this for a plain "add feature X" with no review/ship intent (that
  stays `slice-builder`), nor for a single layer (use the individual task-skills).
---

# Ship slice — HikingLog build → review → ship orchestrator

You deliver one complete feature **with quality gates**, by composing existing pieces — you do not
re-implement what they own. The chain is: **build (no commit) → review the working tree → (conditional)
skill review → completeness check → commit & push → docs sync → report.**

**Why this is a main-loop skill, not an agent or part of `slice-builder`:** a subagent cannot spawn
agents. `slice-builder` has no `Agent` tool, so it cannot run `skill-reviewer` or the `code-review` skill
(which itself fans out agents). Only the main loop has `Agent` + `Skill`, so the orchestration must run
here.

Read `CLAUDE.md`, `.claude/functional-plan.md`, and `.claude/integration-testing.md` first for the
architecture rules, domain model, and test conventions.

## Core principle: review BEFORE the commit

`slice-builder` builds and self-verifies but — when invoked by this skill — **does not commit**. The
review, the conditional skill review, and the completeness check all run on the **uncommitted
working-tree diff** (`git diff` against `master`). Only once every gate is green do you commit the
reviewed slice as a single clean `feat(...)` commit. This is the whole point: nothing known-bad ever
reaches a commit.

## Honesty rules (non-negotiable)

- **"Unverified" is never "passed."** Integration tests need Docker (Testcontainers). If Docker is
  unavailable and you could not run them, say so explicitly — do not claim they passed.
- **Never commit or push known-red work** (failing build, format, or tests — Docker exception aside).
- **No hard iteration cap on the review loop.** Loop until it *converges* (a round produces no new
  confirmed findings). The runaway guard is *non-progress*: if the same finding keeps coming back
  without a fix resolving it, stop and report it — do not loop forever and do not invent a fixed count.

## Steps

### 0. Validate the `code-review` tool (once, before the loop)
The review mechanism is a **session-level capability, not a skill defined in this repo** — do not expect
to find it under `.claude/skills/`. Two different things are named "code-review" and may both be present
in the session:
- a **PR-based plugin** `/code-review` — needs an open PR and comments via `gh`. **Not usable before a
  commit** (and `gh` is not in this repo's allow-list).
- the **`code-review` skill** — reviews "the current diff" locally and can apply fixes to the working
  tree with `--fix`. **This is the one to use.**

Confirm `Skill("code-review")` resolves to the working-tree `--fix` variant and reviews *uncommitted*
changes (not the PR plugin). **Fallback if it is unavailable or resolves to the PR variant:** do the
review inline from the main loop — spawn a few parallel read-only review agents over the diff (one for
`CLAUDE.md` adherence, one for bugs, one for comment/spec adherence), then confirm each finding with an
independent skeptic agent before applying. This fallback is the contract; never block the pipeline just
because the packaged skill is missing.

### 1. Pre-flight
- **Confirm scope first.** Establish the feature name and what to build from the user's request,
  reconciled with `.claude/functional-plan.md`. If neither the request nor the plan resolves what to
  ship, **stop and ask the user** — you run at the main loop and can ask interactively (unlike
  `slice-builder`). Never invent scope.
- Be on a `feature/<short-description>` branch. If still on `master`, create one.
- **Require a clean working tree** (`git status` clean). The review diff must be *only* this slice — if
  the tree is dirty, stop and report (or have the user stash) rather than reviewing stray changes.
- Confirm no `HikingLog.AppHost` / `HikingLog.Api` process is running — a running app holds file locks
  on the build output and the build will fail.

### 2. Build (no commit)
Spawn `slice-builder` with the feature brief. The brief **must contain the exact phrase
`composed mode — do NOT commit`** — that phrase is the agreed trigger that puts `slice-builder` into
composed mode (build + self-verify only, leave changes uncommitted, do not create/switch branches). Also
restate it in plain words: build and self-verify but do not commit or push, leaving the changes in the
working tree for review. Do not duplicate its verification here.

If `slice-builder` returns **red or unverified** (build or unit tests not green — the Docker exception
for integration tests aside), **stop and report. Do not review a broken tree.**

### 3. Review loop (the core — runs before any commit)
Operate on the working-tree diff (`git diff master`, uncommitted). Each round:
1. Run the **`code-review` skill** (working-tree variant, with `--fix`; otherwise the fallback from
   step 0). Apply only **confirmed** findings.
2. **Re-verify after every round** at the chosen safety level: the `/check` sequence (`dotnet build`,
   `dotnet format --verify-no-changes`, `dotnet test tests/HikingLog.Application.Tests`,
   `dotnet test tests/HikingLog.Api.Tests`) **plus the integration tests, which `/check` does NOT
   include** — run `dotnet test tests/HikingLog.IntegrationTests` (Docker) separately each round. This
   is full verification each round, not a cheap subset. If Docker is unavailable, the integration tests
   are *unverified*, not passed (see Honesty rules).

Repeat **until convergence** — stop as soon as a round yields no new confirmed findings. Non-progress
guard: if a finding keeps reappearing without a fix resolving it, stop and report it rather than looping.

### 4. Conditional skill review (before commit)
Only if the work changed a `.claude/skills/**`, `.claude/agents/**`, or instruction file (`CLAUDE.md`,
`.claude/*.md`) — detect via `git diff --name-only` on the working tree. A normal slice touches none of
these, so skip it then.

If it did: spawn `skill-reviewer`, apply confirmed fixes, re-verify (`/check`), and repeat until clean
(same convergence rule as step 3). Otherwise skip entirely.

### 5. Completeness check (before commit)
Spawn a read-only agent — the built-in `Explore` agent type, or `general-purpose` if unavailable — that
compares **only the just-built feature** against its corresponding section in
`.claude/functional-plan.md`, not the whole plan (otherwise it will always report the features that are
deliberately not built yet). Ask: are all endpoints of *this* feature present? Are all business rules of
*this* feature covered? Report any gaps as follow-up — do not build them unprompted.

### 6. Commit & push
Only now, with every gate green. Commit the reviewed slice as a single clean `feat(...)` commit (the
review fixes are already folded in — no separate `fix` commits). Then `git push` on the feature branch.
`git` commit/push are in the allow-list.

### 7. Docs sync
Update the status in `.claude/functional-plan.md` (mark the feature / its endpoints as done) — only if
the completeness check found **this feature** fully covered (all of its endpoints and business rules
present). Gaps in *other*, not-yet-built features are expected and do not block this sync; an incomplete
*this* feature does — report it and leave the plan unchanged. Commit separately as `docs(plan): ...`.

### 8. Final report (PR stays the user's call)
Do not run `gh pr create` yourself — `gh` is intentionally not in the allow-list and opening a PR is the
user's outward-facing call. Report:
- what was built (entity, commands/queries, endpoints, tests);
- verification results (and explicitly flag integration tests as unverified if Docker was unavailable);
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
