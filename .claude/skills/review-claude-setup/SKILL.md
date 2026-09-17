---
name: review-claude-setup
description: Review this repo's Claude Code setup (.claude/ skills, agents, rules, commands, settings, and CLAUDE.md) against the current Claude Code docs AND against the real code in src/ and tests/. Orchestrates 6 read-only lens subagents (skills, agents, config, consistency, placement, code-examples) in parallel and writes a paired markdown + JSON report to reviews/. Use whenever the user wants to review, audit, or sanity-check the Claude Code configuration — "review the Claude Code setup", "review our skills/agents", "check the rules", "do the skill examples still match the code" — and it is the conditional step 4 of the ship-slice skill. Do NOT use for reviewing application code (use backend-review).
---

# Review Claude Code setup — HikingLog

**You — the main conversation — are the orchestrator.** This skill's instructions run in the main loop, which holds the `Agent` tool, so *you* spawn the lens subagents and synthesize their findings. Do **not** delegate this orchestration to a subagent: *subagents cannot spawn other subagents*, so an orchestrator subagent would collapse into a single context and lose all parallelism. Keep the hierarchy flat: you (orchestrator) → one layer of lens subagents.

You delegate to six focused lens subagents (each is `Read, Grep, Glob` only):
- `claude-setup-reviewer-skills` — skill frontmatter, trigger quality and exclusivity, interview-mode discipline, progressive disclosure, referenced-file integrity
- `claude-setup-reviewer-agents` — agent frontmatter, least-privilege tools, model fitness, non-interactive robustness, orchestration correctness
- `claude-setup-reviewer-config` — settings/permissions/MCP/hooks schema, shared-vs-local placement, reference resolution, secrets
- `claude-setup-reviewer-consistency` — rules ↔ CLAUDE.md ↔ skills alignment, naming scheme, cross-references, machine-specific values, duplication
- `claude-setup-reviewer-placement` — artifact-type fitness: is each piece the right primitive in the right place
- `claude-setup-reviewer-code-examples` — every code example and code claim verified against `src/` and `tests/` (the project-specific lens; it does not need `<DOCS>`)

Eligible files (scope filter applied to every mode): under `.claude/` (excluding `.claude/skills/*-workspace/`, `.claude/worktrees/` and `.claude/archive/` — the archive holds superseded, inert artifacts), OR `.mcp.json`, OR the repo-root `CLAUDE.md`. Within `.claude/`, keep `*.md` and `*.json`. Output always goes to `reviews/` (gitignored). This skill **never edits**; the caller applies fixes.

**If a lens agent is missing from the registry** — the usual cause is that its definition was created in *this* session and the agent registry has not picked it up yet — do not abandon the review. Use the **fallback**: spawn a read-only `Explore` agent per lens instead, inlining that lens's scan checklist, severity rubric and output schema (copy them from `.claude/agents/claude-setup-reviewer-<lens>.md`) into the prompt, and add a `"lens"` field to each finding object so you can still attribute them. You may merge several lenses into one fallback agent to save round trips. Say in the report header which mechanism ran (`lens agents` | `Explore fallback`). The registry normally catches up within the same session, so a later round can use the real lenses.

---

## Phase 0 — Settle scope (in this conversation)

Resolve scope here, in the main loop, before spawning anything. From the caller's message:
- "what changed on this branch" / "my changes" → **branch diff vs master**
- "what I have uncommitted" / invoked by `ship-slice` → **working tree** (`git status --short`, includes untracked files)
- a GitHub PR number → that **PR** (`gh pr diff <n> --name-only`; prompts — `gh` is not in the allow-list; fall back to branch diff if declined)
- "audit everything" / "the whole setup" / an unqualified "review the Claude Code setup" → **full setup audit**
- genuinely ambiguous → ask **one** `AskUserQuestion` (working tree vs branch diff vs full audit). Resolve it here — never push the question into a subagent (it cannot round-trip to the user).

## Phase 1 — Resolve file list

- **Working tree:** `git status --short` → path column (for renames take the new path) → apply the eligible-file filter.
- **Branch diff:** `git diff master...HEAD --name-only` → apply the filter.
- **PR:** `gh pr diff <n> --name-only` → apply the filter.
- **Full audit:** `Glob` `.claude/**/*.md`, `.claude/**/*.json`, `.mcp.json`, `CLAUDE.md`, then drop `*-workspace/`, `worktrees/` and `archive/` paths.

Empty list → exit cleanly ("No Claude Code setup files in scope — nothing to review."), write nothing.

## Phase 2 — Fetch current docs (anchor to the live spec)

The point of this reviewer is to judge against **current** conventions. Build a `<DOCS>` block once and pass it verbatim to every lens except `code-examples`.

**Primary:** spawn the `claude-code-guide` subagent (single `Agent` call) asking for current, cited authoring rules for: skill `SKILL.md` frontmatter + description/triggering + progressive disclosure; subagent frontmatter + least-privilege tools + model selection; `settings.json`/`settings.local.json` keys + permission rule syntax + shared-vs-local split; `.mcp.json` + `enabledMcpjsonServers`/`enabledPlugins`; hooks; slash commands under `.claude/commands/`; `.claude/rules/*.md` with `paths:` frontmatter (including subfolders); and the decision criteria for **when to use** a skill vs subagent vs slash command vs hook vs rule vs `CLAUDE.md`. Spawning this as a subagent keeps the raw docs out of your main context — only the distilled answer returns.

**Fallback:** if `claude-code-guide` is unavailable or errors, `WebFetch` the canonical pages (`https://code.claude.com/docs/en/{skills,sub-agents,settings,slash-commands,hooks,mcp,memory}`), using `WebSearch` to relocate any moved page. Distill into the `<DOCS>` block yourself.

Record the source used (`claude-code-guide` | `WebFetch-fallback`) for the header. If **both** fail (e.g. no network), still run `code-examples` (it needs no docs) and **skip** the other five lenses, stating why in the header — never review against stale assumptions.

## Phase 3 — Prepare per-lens inputs

Default to **filter-and-read mode** so you do not pull every file into the main context: give each lens a `<FILES>` list of the paths in its domain and let the lens `Read` them itself.
- skills → in-scope `.claude/skills/**`
- agents → in-scope `.claude/agents/*.md`
- config → in-scope `settings*.json` + `.mcp.json` + any hook scripts (+ `.gitignore` for the local-file check)
- consistency, placement → the whole eligible set **plus** `CLAUDE.md` and every rule file even when unchanged (they reason across artifacts)
- code-examples → in-scope `.claude/skills/**`, `.claude/agents/*.md`, `.claude/rules/**`, `CLAUDE.md`; it reads `src/` and `tests/` itself

(Only if the entire scope is tiny — a handful of small files — may you instead build a shared line-numbered `<CONTEXT mode="embed">` block for the five docs-driven lenses. Filter-and-read is the default here.)

## Phase 4 — Decide which lenses run

Run a lens only if its artifacts are in scope; **consistency, placement and code-examples always run** (a change anywhere can break a cross-reference or a code claim). Track run/skip with whichever task-list tool the session offers (`TodoWrite`, or `TaskCreate`/`TaskUpdate`) — one item per lens, skipped ones noted with reason. If the session has no such tool, state the run/skip list in your reply instead; do not skip the bookkeeping.
- skills → if any `.claude/skills/**` in scope
- agents → if any `.claude/agents/*.md` in scope
- config → if any `settings*.json` / `.mcp.json` in scope

## Phase 5 — Fan out (single message, parallel)

In **one message**, issue one `Agent` call per surviving lens (`subagent_type` = the lens name) so they run concurrently. Each prompt has this exact structure (the `<PREAMBLE>` and `<DOCS>` blocks must be byte-identical across calls for prompt-cache hits; only `<TASK>` and `<FILES>` vary; `code-examples` gets no `<DOCS>`):

```
<PREAMBLE>
[the shared preamble below, verbatim]
</PREAMBLE>

<DOCS>
[the distilled docs block from Phase 2]
</DOCS>

<FILES>
[this lens's file list from Phase 3]
</FILES>

<TASK>
You are the <lens> reviewer. Apply your scan checklist to the files listed, judging against <DOCS> (or against src/ and tests/ for code-examples), and return a JSON array of findings per the schema.
</TASK>
```

**Shared preamble (verbatim):**

```
Mode: filter-and-read

## Severity rubric
- CRITICAL — breaks Claude Code or generates wrong code: invalid JSON / unknown settings key, a skill/agent/orchestrator referencing a non-existent skill, agent or file, an invalid tool name, a committed secret, a skill/agent missing required name/description, or a code example that would not compile / fails the dotnet format gate / is factually wrong about src/.
- SIGNIFICANT — a description too weak to trigger reliably or colliding with a sibling, an over-broad tool grant, a task-skill that assumes inputs, a contradiction between rule / CLAUDE.md / skill on a convention code is generated from, config in the wrong file, a stale path reference, a code example that diverges from src/ conventions, or a clear artifact-type misplacement.
- MINOR — verbosity, style, missing eval, link nits, borderline primitive choices, cosmetic example divergence.

## Output
Return ONLY a single fenced JSON code block — an array of finding objects. No prose around it.
[
  {
    "severity": "critical" | "significant" | "minor",
    "title": "one-sentence problem statement",
    "file": ".claude/relative/path or CLAUDE.md",
    "line": <int>,
    "excerpt_lines": [<startLine>, <endLine>],
    "why": "what is wrong, what breaks, under what conditions",
    "fix": "concrete fix in 1–2 sentences",
    "evidence": "optional — the src/ or tests/ path a code-example finding was checked against"
  }
]
If nothing found, return [].

## Hard rules
- Only report findings about files under .claude/, CLAUDE.md, and .mcp.json.
- Judge against the <DOCS> block (or, for code examples, against the real code), not memory. If <DOCS> is silent on a point, do not invent a rule.
- Read the files in your <FILES> list; cite a real file:line for every finding.
- No soft language ("might", "could", "consider"). State what is wrong.
- Stay in your lane — do not report topics owned by another lens.
```

If a lens returns malformed output, retry it once with "Return ONLY the JSON array, nothing else." If still malformed, note the failure in the header and continue.

## Phase 6 — Verification pass

For every `critical`/`significant` finding: `Read` the cited file at `[line-5, line+15]` and confirm the content matches the finding's claim. If it does not relate, **drop** the finding (do not downgrade). For consistency/placement "referenced but missing" / "should be elsewhere" claims, also confirm the target's existence or absence with `Glob`. For code-examples findings, open the `evidence` path and confirm the claimed divergence. Count drops per lens for the header. Leave `minor` findings as advisory.

## Phase 7 — De-duplicate

Group by `(file, line ± 3, fingerprint of title)` (fingerprint = lowercased title, punctuation stripped, first 6 distinct content words). Per group: severity = max; `caught_by` = sorted unique lenses; `why` = longest, with unique sentences from others appended; `fix` = most specific.

## Phase 8 — Assign IDs and render

Sort by severity (CRITICAL→SIGNIFICANT→MINOR), then file, then line. Assign `CCR-001…`. Build the header: timestamp (`Get-Date -Format "yyyy-MM-dd HH:mm"` in PowerShell, `date +"%Y-%m-%d %H:%M"` in bash), scope description, `Docs source:`, `Lenses run:`, `Lenses skipped:` (with reason or `—`), `Findings:` `<C> CRITICAL · <S> SIGNIFICANT · <M> MINOR · <D> dropped`. Then per finding:

```markdown
### CCR-001 — <title>
**File:** `<file>:<line>`
**Caught by:** <Lens A>(, <Lens B> — merged)
**Evidence:** `<src path>` (code-examples findings only)

**Why it matters:** <why>

**Fix:** <fix>

---
```

Then an **Index by file** table (`| File | C | S | M | Findings |`) and an **Action checklist** (`- [ ] **CCR-001** CRITICAL — <short title> in <basename>:<line>`).

JSON sidecar: `version` 1, `generated_at` (ISO 8601 + tz), `scope`, `docs_source`, `lenses_run`, `lenses_skipped`, `summary` (counts), `findings[]` (id, severity, title, file, line, excerpt_lines, why, fix, caught_by, evidence).

## Phase 9 — Deliver

Create `reviews/` if missing (it is gitignored). Basename `<yyyy-MM-dd-HHmm>_claude_setup_review`. `Write` `reviews/<basename>.md` and `reviews/<basename>.json` from the same finding list. Print the two paths, the executive summary line, and the action checklist verbatim. When invoked by `ship-slice`, also return the CRITICAL and SIGNIFICANT findings inline — that skill applies them as "confirmed" fixes. Do not edit the setup files and do not commit.
