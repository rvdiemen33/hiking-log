---
name: spec-close
description: >
  Spec-driven development close-out for HikingLog. Retires an implemented spec: confirms the code was
  reviewed and merged, harvests durable facts out of the spec (domain model and endpoints into
  .claude/functional-plan.md, lasting technical decisions into docs/adr/), then archives the spec under
  docs/specs/archive/ and commits the retirement. Use when a spec-driven feature has shipped and the
  spec should be cleaned up: "close the spec", "retire the spec for X", "we're done with this spec",
  "/spec-close". Refuses to run unless the spec's status is implemented.
  Do NOT use to create, review, or implement a spec — those are spec-create, spec-review and
  spec-implement.
---

# spec-close — retire a delivered spec

A spec is an **ephemeral working artifact**. Once the feature ships, a spec left in `docs/specs/`
becomes stale documentation that quietly contradicts the code. This skill harvests whatever is worth
keeping, then moves the spec out of the active folder and commits that.

---

## Step 1 — Locate and validate the spec

If the user gave a file path, use it. Otherwise:

```bash
ls -t docs/specs/*.md | grep -v README
```

Pick the most recently modified file with `status: implemented`.

**Guards — stop immediately if any fails:**

- No spec found → say so and stop.
- `status` is not `implemented`:
  - `draft` / `reviewed` / `approved` → "This spec has not been implemented — there is nothing to
    close. Run `spec-implement` first." Stop.
  - `implementing` → "This spec is mid-implementation. Finish `spec-implement` before closing." Stop.
- Read the full spec and note the `issue` number when present.

---

## Step 2 — Confirm the code shipped, and decide on harvesting

The spec stays alive until the code it produced has been reviewed and merged — if review bounces the
implementation, the spec is the reference for the fixes.

Before asking, scan the spec for material worth preserving:

- `## Business rules` and `## Domain` — facts that now describe the shipped system and belong in
  `.claude/functional-plan.md` (the living domain spec), if they are not already there.
- `## Open questions` resolved with a non-obvious answer, and `## Review Notes` warnings that led to a
  deliberate design choice.
- Any deviation from the canonical patterns — an unusual `OneOf` contract, a denormalisation, a
  deliberate departure from the CQRS or persistence rules — and **why**.

Judge honestly: **most feature specs yield no architectural decision.** A plain CRUD slice that
followed every canonical pattern has nothing durable to record beyond the plan update.

Then ask once (AskUserQuestion, both questions in a single call):

1. "Has `{slug}`{ (issue #{issue})} been reviewed and merged?" — **Yes, close it** / **Not yet, keep the spec**.
2. Only when step 2's scan found something: "The spec records {one-line summary}. Promote it to an ADR
   before archiving?" — **Yes, write the ADR** / **No, the functional plan and git history are enough**.

On "not yet" → stop cleanly, leaving the spec at `status: implemented`.

---

## Step 3 — Harvest

**Always: sync the functional plan.** `.claude/functional-plan.md` is the living domain spec and must
describe the shipped system. Fold in what the feature changed — new or changed entity properties in
the `## Domain model` tables, new endpoints under `## API endpoints`, new entries under
`## Business rules` and `## Seed data`. Leave the `## Delivery status` line that `ship-slice` already
added; do not duplicate it. If the feature changed nothing the plan describes, say so and skip.

**Conditionally: write an ADR.** Only when the user chose that in step 2:

1. Create `docs/adr/` if it does not exist.
2. Number it from the existing files — highest prefix plus one, three digits, matching the folder's
   padding. Filename: `{number}-{kebab-slug}.md`.
3. Follow the format already used in that folder; when the folder is new, use
   **Context / Decision / Consequences / Alternatives considered**, with frontmatter carrying `date`
   and `status: accepted`.
4. Record the decision itself and the reasoning — never transcribe the whole spec.

---

## Step 4 — Archive the spec and commit

A closed spec is **moved, not deleted** — this skill's own convention, mirroring in spirit how
`CLAUDE.md` treats superseded `.claude/` artifacts, but with no connection to the
`.claude/archive/README.md` mechanism: that table tracks replaced skills, agents, rules and
instruction files, and a retired spec has no replacement to record there. Set `status: closed` in the
frontmatter, then:

```bash
git mv docs/specs/{name}.md docs/specs/archive/{name}.md
```

Create `docs/specs/archive/` if needed. Stage the functional-plan edit and the ADR when there is one,
then commit with a Conventional Commit:

- with an ADR: `docs(spec): retire {slug} and record ADR {number}`
- without: `docs(spec): retire {slug} after implementation`

Do not push and do not open a PR unless the user asks.

**If the user explicitly prefers a clean working tree** over an archive folder, `git rm` the spec
instead — git history retains the full copy either way (`git log --all -- docs/specs/{name}.md`).
This is a spec-only exception the user asks for; it does not loosen `CLAUDE.md`'s rule that a
superseded skill, agent, rule or instruction file is always archived under `.claude/archive/`.

---

## Step 5 — Report

> "Closed `{slug}`{ (issue #{issue})}.
> - Functional plan: {what was folded in, or 'no changes needed'}
> - ADR: {`docs/adr/{number}-{slug}.md`, or 'none — routine slice, no lasting decision'}
> - Spec archived at `docs/specs/archive/{name}.md` (`status: closed`), committed but not pushed."

---

## Hard rules

- **Never close a spec that is not `implemented`** — an earlier status means the work is unfinished
  and the spec is still the reference for it.
- **Confirm the code shipped before retiring** — the spec is the map for review-driven fixes.
- **Harvest before archiving, but do not over-harvest** — the functional plan always; an ADR only for
  a genuine, lasting decision.
- **Use `git mv` (or `git rm` on explicit request), never a filesystem delete** — the commit and the
  retained history are the point.
- This skill commits the retirement it was invoked to perform, after the user's confirmation in
  step 2; it commits nothing else.
