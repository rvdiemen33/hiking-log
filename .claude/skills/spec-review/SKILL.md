---
name: spec-review
description: >
  Spec-driven development review gate for HikingLog. Locates a spec in docs/specs/ and dispatches the
  model-pinned spec-reviewer agent in gate mode: the agent critiques the spec across seven dimensions
  (completeness, domain/persistence consistency, Application-to-Api consistency, edge cases, validation
  gaps, test coverage, unresolved TO CONFIRM markers), appends Review Notes to the spec, and flips
  status draft to reviewed when no blockers remain. Use when the user says "review my spec", "check the
  spec for X", "is this spec ready", "/spec-review", or right after spec-create once they have edited
  the draft. Takes an optional file path; defaults to the most recent draft in docs/specs/.
  Do NOT use to review backend code — that is backend-review — and do NOT use to review the Claude Code
  setup (review-claude-setup).
---

# spec-review — the draft-to-reviewed gate

Locate the spec, dispatch the `spec-reviewer` agent in **gate mode**, relay the outcome. All review
judgment lives in the agent — its model is pinned in `.claude/agents/spec-reviewer.md`, so every spec
passes the same reviewer regardless of the session model. **Do not review the spec yourself** and do
not edit it; the agent owns both the Review Notes and the status flip.

## Step 1 — Locate the spec

If the user gave a file path, use it. Otherwise list the specs newest first:

```bash
ls -t docs/specs/*.md | grep -v README
```

Pick the most recently modified file whose frontmatter says `status: draft`. If no draft exists, say
so and stop — a `reviewed` spec is already through this gate, and an `approved` one is waiting for
`spec-implement`.

## Step 2 — Dispatch the reviewer

Spawn the agent (`Agent` tool, `subagent_type: spec-reviewer`) with:

> Review the spec at `{path}` in **gate mode**.

The agent reads the spec, evaluates the seven dimensions against `.claude/functional-plan.md` and the
rules in `.claude/rules/backend/`, appends `## Review Notes`, and flips `status: draft` → `reviewed`
when it finds no blockers.

**Fallback if the agent is not in the registry** (which happens when its definition was created in
this same session): spawn a read-only `Explore` agent instead, inline the seven dimensions and the
severity scale from `.claude/agents/spec-reviewer.md` into its prompt, and apply its Review Notes and
status flip yourself with `Edit`. Say in your report that the fallback ran.

## Step 3 — Report

Relay the agent's findings verbatim, then the next step. No blockers:

> "Spec reviewed — no blockers. Status set to `reviewed`.
> {N} warning(s) and {N} suggestion(s) are recorded in `## Review Notes`.
>
> **Next step:** address the warnings you care about, then set `status: approved` in the frontmatter
> to unlock `spec-implement`."

Blockers:

> "Spec review found {N} blocker(s) — status stays `draft`. Resolve these before approving:
>
> {numbered blockers}
>
> Run `spec-review` again once they are resolved."

Do not commit the spec — per `CLAUDE.md`, commit only when the user asks.
