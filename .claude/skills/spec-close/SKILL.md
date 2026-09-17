---
name: spec-close
description: >
  Spec-driven development close-out for HikingLog. Retires an implemented spec: confirms the code was
  reviewed and merged, answers three close-out questions explicitly - is there a lasting decision (ADR),
  did the feature make existing documentation wrong (correct it), is documentation now missing (create
  it) - where "none" is an answer and silence is not, harvests the domain model and endpoints into
  .claude/functional-plan.md, then archives the spec under docs/specs/archive/ and commits the
  retirement. Establishes the merge itself from the git history rather than asking about it, so a
  routine slice closes without a single question. Use when a spec-driven feature has shipped and the
  spec should be cleaned up: "close the spec", "retire the spec for X", "we're done with this spec",
  "/spec-close", or once the pull request for a spec-driven feature has been merged. Refuses to run
  unless the spec's status is implemented.
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

Pick the most recently modified file with `status: implemented`. When several qualify, run step 2's
`git log` check **once per candidate slug** and prefer a candidate whose merge it proves — an
`implemented` spec whose branch is still open is not ready to retire. Fall back to the most recently
modified one when the check proves none of them.

**Guards — stop immediately if any fails:**

- No spec found → say so and stop.
- `status` is not `implemented`:
  - `draft` / `reviewed` / `approved` → "This spec has not been implemented — there is nothing to
    close. Run `spec-implement` first." Stop.
  - `implementing` → "This spec is mid-implementation. Finish `spec-implement` before closing." Stop.
- Read the full spec and note the `issue` number when present.

---

## Step 2 — Confirm the code shipped, then answer the three close-out questions

The spec stays alive until the code it produced has been reviewed and merged — if review bounces the
implementation, the spec is the reference for the fixes.

**Establish the merge yourself before asking about it.** The spec-flow commits carry the slug, so a
merged branch leaves its trail on `master`:

```bash
git log master --oneline --grep="{slug}" | head -5
git log origin/master --oneline --grep="{slug}" | head -5
```

`--grep` matches anywhere in a commit message, so a bare hit proves nothing — a revert, a follow-up or
a longer word containing the slug all match. **Only two exact subjects count as proof:**

- `Merge feature/{slug} into master`
- `docs(spec): mark {slug} implemented`

Read the matched subjects and accept only those two; anything else falls through to the question. The
slice's own `feat(...)` commit is **not** proof — its scope is the feature area, not the slug
(`feat(stages): …` for slug `stage-notes`), so it never matches. `git log` is in the permission
allow-list; `gh` deliberately is not, so do not reach for it here.

On proof, treat the merge as confirmed and **skip question 1 below** — asking a question you have just
answered from the history is friction, not a gate. Say in the report which commit and which ref carried
the proof.

When neither ref shows the slug the branch is not merged **or** the local refs are stale, and those two
are indistinguishable without a fetch. Ask question 1 as written.

Before asking anything, answer **three questions** from the spec and the shipped code. Each gets an
explicit answer — **"none" is an answer, silence is not** — and every answer lands in the report
(step 5):

1. **Is there a lasting decision in here?** Scan `## Business rules` and `## Domain` for facts that
   now describe the shipped system; `## Open questions` resolved with a non-obvious answer and
   `## Review Notes` warnings that led to a deliberate design choice; any deviation from the canonical
   patterns — an unusual `OneOf` contract, a denormalisation, a deliberate departure from the CQRS or
   persistence rules — and **why**. Judge honestly: **most feature specs yield no architectural
   decision.** A plain CRUD slice that followed every canonical pattern has nothing durable to record
   beyond the plan update. Answer: the one-line decision, or "none".
2. **Does the shipped feature make existing documentation wrong?** Check `.claude/functional-plan.md`
   (a rule, endpoint or property it changed), `docs/adr/*.md` (an ADR the feature supersedes),
   `CLAUDE.md` where it describes the stack, local setup or running the API (a new configuration
   value, a changed port or connection string), and the code examples in `.claude/rules/backend/*.md`
   and `.claude/skills/*/SKILL.md` (a pattern the feature changed). Answer: the files and what is now
   wrong in each, or "none".
3. **Is documentation missing that should now exist?** A new endpoint family or business rule with no
   home in the functional plan; a configuration value or run-time dependency `CLAUDE.md`'s setup
   sections do not mention; a convention the slice introduced that no rule captures. Answer: what to
   create and where, or "none".

Then ask once (AskUserQuestion — every applicable question in a single call, at most four). **When no
question applies, ask nothing and go straight to step 3** — that is the normal outcome for a routine
slice whose merge is provable and which yields no decision:

1. Only when the merge could **not** be established above: "Has `{slug}`{ (issue #{issue})} been
   reviewed and merged?" — **Yes, close it** / **Not yet, keep the spec**.
2. Only when close-out question 1 (lasting decision) found one: "The spec records {one-line decision}.
   Promote it to an ADR before archiving?" — **Yes, write the ADR** / **No, the functional plan and git
   history are enough**.
3. Only when close-out question 2 (documentation made wrong) found something: "These documents are now
   wrong: {list}. Correct the ones under `docs/` and in the functional plan in this close-out? (Items
   in `CLAUDE.md` and `.claude/**` stay follow-up.)" — **Yes, correct them** / **No, I will handle it**.
4. Only when close-out question 3 (documentation missing) found something: "This documentation should
   now exist: {list}. Create it in this close-out? (A missing rule or instruction stays follow-up.)" —
   **Yes, create it** / **No, I will handle it**.

On "not yet" → stop cleanly, leaving the spec at `status: implemented`. A "none" answer is never asked
about — it is reported.

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

**Conditionally: correct documentation the feature made wrong.** Only when the user chose that in
step 2, and only in files under `docs/` and in `.claude/functional-plan.md`: make the minimal edit that
makes the text true again; set a superseded ADR's `status` to `superseded by {number}` when the new
ADR replaces it (the file stays — nothing under `docs/adr/` is ever deleted). **Never edit
`CLAUDE.md`, `.claude/rules/**` or `.claude/skills/**` here** — an instruction, rule or skill change
needs `/review-claude-setup` afterwards and is its own piece of work; list those as follow-up in the
report instead.

**Conditionally: create documentation that should now exist.** Only when the user chose that in
step 2, under the same file restriction: add the missing section to the functional plan, or the
missing ADR when close-out question 1 also produced one. A missing *rule* or *instruction* is a
follow-up, not something this skill writes.

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

Create `docs/specs/archive/` if needed. Stage the functional-plan edit, the ADR and the documentation
edits the user approved in step 2 when there are any, then commit with a Conventional Commit:

- with an ADR: `docs(spec): retire {slug} and record ADR {number}`
- without: `docs(spec): retire {slug} after implementation`

Do not push and do not open a PR unless the user asks.

**If the user explicitly prefers a clean working tree** over an archive folder, `git rm` the spec
instead — git history retains the full copy either way (`git log --all -- docs/specs/{name}.md`).
`git mv` is in the permission allow-list; `git rm` deliberately is not — expect a prompt on that path.
This is a spec-only exception the user asks for; it does not loosen `CLAUDE.md`'s rule that a
superseded skill, agent, rule or instruction file is always archived under `.claude/archive/`.

---

## Step 5 — Report

> "Closed `{slug}`{ (issue #{issue})}.
> - Merge: {the commit that proves it, and the ref it was found on — or 'confirmed by you'}
> - Lasting decision: {ADR `docs/adr/{number}-{slug}.md`, or 'none — routine slice'}
> - Existing documentation corrected: {files and what changed, or 'none needed'}{; follow-up in
>   `CLAUDE.md` / `.claude/**`: {list}}
> - Documentation created: {files, or 'none needed'}{; follow-up rule or instruction needed in
>   `.claude/**`: {what}}
> - Functional plan: {what was folded in, or 'no changes needed'}
> - Spec archived at `docs/specs/archive/{name}.md` (`status: closed`), committed but not pushed."

All three close-out answers appear, every time — a line that says "none" is the evidence that the
question was asked.

---

## Hard rules

- **Never close a spec that is not `implemented`** — an earlier status means the work is unfinished
  and the spec is still the reference for it.
- **Confirm the code shipped before retiring** — the spec is the map for review-driven fixes.
- **Answer all three close-out questions explicitly** — a lasting decision, documentation made wrong,
  documentation now missing. "None" is an answer; silence is not, and the report carries all three.
- **Harvest before archiving, but do not over-harvest** — the functional plan always; an ADR only for
  a genuine, lasting decision; documentation edits only where the user said yes, and never in
  `CLAUDE.md`, `.claude/rules/**` or `.claude/skills/**`.
- **Use `git mv` (or `git rm` on explicit request), never a filesystem delete** — the commit and the
  retained history are the point.
- This skill commits the retirement it was invoked to perform, and commits nothing else. It waits for
  the user's confirmation only on the questions step 2 actually asked; a routine slice with a provable
  merge and three "none" answers asks nothing and commits without one.
