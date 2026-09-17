---
name: spec-verify
description: >
  Spec-driven development verification for HikingLog. Lays the delivered code against its approved
  spec in docs/specs/ by dispatching the model-pinned spec-verifier agent over the working tree, the
  branch diff vs master, or an explicit file list: which R{n} requirements are missing or contradicted,
  which AC{n}.{m} acceptance scenarios have no test, and what changed that the spec never mentions —
  every finding classified mechanical (fix the code) or design (reopen the spec). Verifies the
  CRITICAL/SIGNIFICANT findings, writes a paired markdown + JSON report to reviews/, and returns the
  findings inline. Read-only; never edits or commits. Use when the user says "verify the slice against
  the spec", "does the code match the spec", "spec coverage", "/spec-verify", and it is the spec step of
  ship-slice's review loop. Do NOT use to review code quality (backend-review), to review the spec
  itself (spec-review), or to review the Claude Code setup (review-claude-setup).
---

# spec-verify — code against spec

Locate the spec, settle the scope, dispatch the `spec-verifier` agent, verify what it returns, write
the report, relay. All verification judgment lives in the agent — its model is pinned in
`.claude/agents/spec-verifier.md`, so every slice meets the same verifier regardless of the session
model. **Do not verify the code yourself** and do not edit anything; the caller (the user, or
`ship-slice`) applies fixes.

## Step 1 — Locate the spec

If the caller gave a file path, use it. Otherwise list the specs newest first:

```bash
ls -t docs/specs/*.md | grep -v README
```

Pick the most recently modified file whose frontmatter status is `implementing`, `implemented` or
`approved` — in that order of preference. If none exists, say so and stop: a `draft` or `reviewed` spec
has nothing to verify against yet, and a `closed` one lives in `docs/specs/archive/`.

## Step 2 — Settle the scope

- The caller stated it (`ship-slice` always passes **working tree**; `spec-implement`'s Route B passes
  **branch**; a scoped re-run passes an explicit file list) → use it.
- The user said "my changes" / "what I have uncommitted" → **working tree**.
- The user said "this branch" / "the slice" on a `feature/<...>` branch → **branch**.
- Otherwise: **working tree** when `git status --short` shows changes, **branch** when it is clean.

## Step 3 — Dispatch the verifier

Spawn the agent (`Agent` tool, `subagent_type: spec-verifier`) with:

> Verify the code against the spec at `{path}`. Scope: **{working tree | branch | the following
> files: …}**. {round: {label} — only when the caller passed one}

**Fallback if the agent is not in the registry** (which happens when its definition was created in
this same session): spawn a read-only `Explore` agent instead, inline the read order, the three
questions, the classification rules, the severity rubric and the output schema from
`.claude/agents/spec-verifier.md` into its prompt, and restrict its Bash use to `git status` and
`git diff`. Say in the report that the fallback ran.

If the agent returns malformed output, retry once with "Return ONLY the JSON object, nothing else."
If still malformed, report the failure and stop — do not invent findings.

## Step 4 — Verify the findings

For every `critical`/`significant` finding with a real `line`: `Read` the cited file at
`[line-5, line+15]` and confirm the content matches the claim; for a `missing-requirement`, `Glob` the
location the agent says is empty. If the claim does not hold, **drop** the finding (do not downgrade)
and count the drop for the header. Leave `minor` findings as advisory. Never change a finding's
`class` — mechanical vs design is the agent's call; if you disagree, say so in the report, do not edit
it silently.

## Step 5 — Render and deliver

Sort by severity, then file, then line; assign `SV-001…`. Create `reviews/` if missing (gitignored).
Basename `<yyyy-MM-dd-HHmm>_spec_verification` (`Get-Date -Format "yyyy-MM-dd-HHmm"` in PowerShell,
`date +"%Y-%m-%d-%H%M"` in bash). Write `reviews/<basename>.md` and `reviews/<basename>.json` from the
same finding list.

Header: timestamp, spec path and status, scope, `Round:` (the caller's label, or `—`), `Mechanism:`
(`spec-verifier` | `Explore fallback`),
`Ids:` (`spec` | `derived`), `Coverage:` `R {implemented}/{total} implemented · AC {tested}/{total}
tested`, `Findings:` `<C> CRITICAL · <S> SIGNIFICANT · <M> MINOR · <D> dropped · {n} design`.

Per finding:

```markdown
### SV-001 — <title>
**File:** `<file>:<line>` · **Requirement:** `<R/AC id or —>` · **Kind:** <kind> · **Class:** <mechanical | design>

**Why it matters:** <why>

**Fix:** <fix>

---
```

Then an **Index by requirement** table (`| Id | Status | Findings |` — one row per `R` and `AC`,
status `implemented` / `tested` / `missing` / `untested` / `contradicted`) and an **Action checklist**
split in two lists: **Mechanical — fix the code** (`- [ ] **SV-001** CRITICAL — <short title> in
<basename>:<line>`) and **Design — reopen the spec** (same shape, plus the pointer
`docs/specs/README.md → Reopening a spec`).

JSON sidecar: `version` 1, `generated_at` (ISO 8601 + tz), `spec`, `spec_status`, `scope`, `round`,
`mechanism`,
`ids`, `requirements`, `scenarios`, `summary` (counts incl. `design`), `findings[]` (id, severity,
class, kind, requirement, title, file, line, excerpt_lines, why, fix, evidence).

Print the two paths, the coverage line, and both checklists verbatim. When invoked by `ship-slice`,
also return the CRITICAL and SIGNIFICANT findings inline with their `class` — that skill applies the
mechanical ones and stops on a design one. Do not edit code, do not touch the spec, do not commit.

## Hard rules

- Read-only, no commit — the spec's status and the code are the caller's to change
  (`docs/specs/README.md → Reopening a spec` names who reopens on a design finding).
- One spec per run. A slice that spans two specs is two runs.
- Relay the agent's `ids: derived` note when it appears: the spec predates requirement ids, so the
  coverage numbers are the verifier's numbering, not the spec's.
