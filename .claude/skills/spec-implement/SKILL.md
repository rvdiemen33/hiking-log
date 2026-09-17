---
name: spec-implement
description: >
  Spec-driven development implementation orchestrator for HikingLog. Reads an approved spec from
  docs/specs/, turns it into a complete feature brief, and hands that brief to the ship-slice skill
  (build → review → commit) or, when the user declines the quality gate, to the slice-builder agent.
  Refuses to run unless the spec's status is approved. Use when the user says "implement the spec",
  "build the feature from the spec", "the spec is approved, go", or "/spec-implement". Takes an
  optional file path; defaults to the most recent approved spec in docs/specs/.
  Do NOT use to write a spec (spec-create), to review one (spec-review), or to retire one after
  delivery (spec-close); for a feature that has no spec, use ship-slice directly.
---

# spec-implement — from approved spec to delivered slice

Read an **approved** spec and orchestrate its implementation. This skill is a thin coordination
layer: it validates the spec, translates it into a brief, delegates, and maintains the spec's status
and checkboxes. It **writes no production code** — `ship-slice` and the task-skills own that.

---

## Step 1 — Locate and validate the spec

If the user gave a file path, use it. Otherwise:

```bash
ls -t docs/specs/*.md | grep -v README
```

Pick the most recently modified file with `status: approved`.

**Guards — stop immediately if any fails:**

- No spec found → say so and stop.
- `status` is not `approved`:
  - `draft` → "This spec has not passed the review gate. Run `spec-review` first, then set
    `status: approved`."
  - `reviewed` → "This spec is reviewed but not approved. Set `status: approved` in the frontmatter
    once you have accepted the Review Notes, then re-run `spec-implement`."
  - `implementing` / `implemented` → "This spec is already `{status}`. Re-running may duplicate
    scaffolding." Use AskUserQuestion to confirm before continuing; on `implementing`, resume from the
    first unticked item in `## Implementation tasks`.
- Read the full spec. `grep -n "TO CONFIRM:"` it — **an approved spec must contain none**. If markers
  survived, stop and list them; the spec was approved prematurely.

Note the `issue` number from the frontmatter when present; it goes in the PR, not in the commit
subject (`CLAUDE.md` prescribes Conventional Commits).

---

## Step 2 — Choose the delivery route

Default to the **full quality gate**. Ask once with AskUserQuestion, and skip the question when the
user already stated which route they want:

- **`ship-slice` (recommended)** — build (no commit) → five-lens `backend-review` over the working
  tree → completeness check → single reviewed `feat(...)` commit → push → docs sync. This is the
  route spec-driven development is for: nothing known-bad reaches a commit.
- **`slice-builder` only** — build, self-verify, commit and push without the review gate. Faster, no
  quality gate. Choose it only when the user explicitly asks.

---

## Step 3 — Prepare the branch and mark the spec in flight

`ship-slice` requires a **clean working tree** and a `feature/<...>` branch, so do both before
spawning it:

1. `git branch --show-current`. If you are on `master`, switch to the spec's branch: `spec-create`
   normally created it when it committed the draft, so `git checkout feature/{slug}` when
   `git rev-parse --verify --quiet feature/{slug}` finds it, else `git checkout -b feature/{slug}`.
   If you are already on a suitable `feature/<...>` branch, stay.
2. If the working tree is dirty for unrelated reasons, stop and report — do not stash the user's work.
3. Set `status: implementing` in the spec frontmatter and commit **only that file**:
   `docs(spec): mark {slug} implementing`. This keeps the tree clean for `ship-slice` and makes the
   run resumable after an interruption. This is the single commit this skill makes; every code commit
   belongs to `ship-slice`.

---

## Step 4 — Build the brief and delegate

Translate the spec into one brief. The mapping is mechanical:

| Brief input | Spec source |
|---|---|
| Feature name and intent | `# Feature:` + `## Overview` (verbatim) |
| Entity, properties, relationships, indexes, seed data, migration name | `## Domain` |
| Commands: names, kinds, properties, `OneOf` contracts, validation rules | `## Application → Commands` |
| Queries: names, kinds, filters, DTO fields | `## Application → Queries` |
| Controller, route templates, verbs, status codes, request/response models | `## Api` |
| Business rules and the layer that enforces them | `## Business rules` |
| Required unit / Tier 0 / Tier 3 coverage, acceptance scenarios | `## Tests` + the per-command Acceptance blocks |
| Layers to **skip** because they already exist | `## Impact / Affected areas → Layers that already exist` |

Then delegate.

**Route A — `ship-slice` (default).** Invoke it with the `Skill` tool. State in the invocation:

- the full brief from the table above;
- that **the spec is the authoritative scope document**: its pre-flight must take the scope from
  `docs/specs/{name}.md` rather than re-deriving it, and must not ask the user to restate it;
- the **skip-list** verbatim, so its pre-flight does not re-run `domain-entity` or
  `dotnet-ef-migration` over layers that already exist;
- that the **completeness check (its step 5) compares the delivered slice against the spec**, plus the
  feature's section in `.claude/functional-plan.md` where one exists — a spec-driven feature may be
  new to the plan, and an absent plan section is not a gap;
- that its **docs sync (its step 7)** still applies: add the delivery line to `## Delivery status` in
  `.claude/functional-plan.md`, referencing the feature branch.

`ship-slice` owns the branch gate, the build, the review loop, the verification, the commit and the
push. Do not duplicate any of it, and do not suppress its gates.

**Route B — `slice-builder` only.** Spawn the agent (`Agent` tool, `subagent_type: slice-builder`)
with the same brief. Do **not** include the phrase `composed mode — do NOT commit`: standalone mode
is exactly what this route wants — the agent commits and pushes itself. It will not create a branch
when one is already checked out, which step 3 guarantees.

**If either reports a red build, red tests, or an unresolved review finding** — stop. Leave the spec
at `status: implementing`, relay the failure, and let the user decide. Never mark a spec implemented
on top of a failing verification, and never claim integration tests passed when Docker was
unavailable (they are *unverified*, per `CLAUDE.md`).

---

## Step 5 — Close the loop on the spec

Once the delegated route reports green:

1. Tick every delivered item in `## Implementation tasks`; leave deliberately skipped items unticked
   with a one-line note.
2. Set `status: implemented` in the frontmatter.
3. Commit the spec update: `docs(spec): mark {slug} implemented`, and push it to the same branch.
4. Report:

> "Implemented `{slug}`{ (issue #{issue})}.
>
> - Built: {entity, commands, queries, endpoints, tests — from the delegate's report}
> - Verification: {build / format / unit / Tier 0 / Tier 3 results, with Docker caveats stated}
> - Review: {confirmed findings applied, or 'no review gate — slice-builder route'}
> - Completeness gaps: {list, or 'none'}
>
> Spec marked `implemented`. Next: open the PR yourself — `gh` is deliberately not in the allow-list:
>
> ```
> gh pr create --title \"{feature}\" --body \"{layers and operations the slice covers}{, closes #{issue}}\"
> ```
>
> Once the PR is merged and the code is stable, run `spec-close` to retire the spec."

---

## Hard rules

- **Never write slice code directly.** Delegate to `ship-slice` or `slice-builder`; this skill only
  reads the spec, maps the brief, runs guards, and maintains spec status and checkboxes.
- **Never run `gh pr create`** — opening a PR stays the user's call (`CLAUDE.md`).
- **Only spec files are committed here.** The feature commit belongs to the delegated route.
- **Stop on red.** A failing build or test leaves the spec `implementing` and ends the run.
- Stay within this repository (`C:\github\hiking-log`) and add no packages without an explicit request.
