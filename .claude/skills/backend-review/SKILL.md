---
name: backend-review
description: Comprehensive, project-aware code review of the HikingLog .NET solution. Orchestrates 5 read-only lens subagents (architecture, data-performance, correctness, code-quality, tests) in parallel over the uncommitted working tree, the branch diff vs master, an explicit file list, a GitHub PR, or the whole solution; verifies and de-duplicates their findings and writes a paired markdown + JSON report to reviews/. Use whenever the user wants the HikingLog code reviewed — "review the slice", "review my changes against the project rules", "review this branch", "audit the backend" — and it is the review step of the ship-slice skill. Do NOT use for reviewing the Claude Code setup (use review-claude-setup). It is distinct from the session-level code-review skill: this one applies the repo's own rules and lenses and never edits.
---

# Backend code review — HikingLog

**You — the main conversation — are the orchestrator.** This skill runs in the main loop, which holds the `Agent` tool, so *you* spawn the lens subagents and synthesize their findings. Do **not** delegate this orchestration to a subagent: *subagents cannot spawn other subagents*, so an "orchestrator subagent" would collapse into a single context and lose all parallelism. Keep the hierarchy flat: you (orchestrator) → one layer of lens subagents.

HikingLog follows Clean Architecture + Vertical Slice Architecture with a hand-rolled CQRS, `OneOf` results, FluentValidation and EF Core accessed directly through `IHikingLogDataContext`. The conventions the lenses judge against are `CLAUDE.md` and `.claude/rules/backend/*.md`. You delegate to five lens subagents (each is `Read, Grep, Glob` only):
- `backend-reviewer-architecture` — layer boundaries, slice integrity, OneOf contracts, manual DI, folder drift
- `backend-reviewer-data-performance` — EF Core projection/N+1/`CancellationToken`, aggregates in the DB, sync I/O, LINQ
- `backend-reviewer-correctness` — validation/existence checks, OneOf misuse, null handling, async pitfalls, boundary bugs
- `backend-reviewer-code-quality` — naming, complexity, duplication, XML docs, `.editorconfig`/`dotnet format` gate, C# idioms
- `backend-reviewer-tests` — structure, assertions, unit/Tier 0/Tier 3 coverage, mocking discipline, fakers

Scope restriction: only `.cs` and `.csproj` files under `src/` and `tests/`. Output always goes to `reviews/` (gitignored — reports are local artifacts). This skill **never edits code**; the caller applies fixes.

---

## Phase 0 — Scope

Resolve scope here, in the main loop. If the caller already stated it (the `ship-slice` skill always passes **working tree**, or an explicit file list for a scoped re-review), use it. Otherwise pick from the user's words, and only when genuinely ambiguous ask **one** `AskUserQuestion`:

```
Question: "Which scope should be reviewed?"
Header: "Scope"
Options:
- (A) Working tree — everything uncommitted, including new files (default when the tree is dirty)
- (B) Branch diff vs master — files this branch changed
- (C) Full audit — every .cs/.csproj under src/ and tests/
- (D) GitHub PR — provide the number; uses `gh pr diff` (prompts for permission — gh is not in the allow-list)
```

An explicit file list from the caller is scope **(E)** and skips the question.

## Phase 1 — Resolve file list

- **(A) Working tree:** `git status --short` → take the path column (for renames `old -> new`, take `new`); include `M`, `A`, `R`, and `??` (untracked). `git diff --name-only` alone misses untracked files, and a new slice is mostly untracked.
- **(B) Branch diff:** `git diff master...HEAD --name-only`.
- **(C) Full audit:** `Glob` `src/**/*.cs`, `tests/**/*.cs`, `src/**/*.csproj`, `tests/**/*.csproj`.
- **(D) PR:** `gh pr diff <number> --name-only`. If `gh` is unavailable or the user declines the prompt, fall back to (B) and say so in the header.
- **(E) Explicit list:** as given.

Then filter: keep only `.cs`/`.csproj` under `src/` or `tests/`; drop `bin/`, `obj/`, and `Migrations/*.Designer.cs` / `*ModelSnapshot.cs` (generated). If the list is empty, exit cleanly ("No `.cs`/`.csproj` files in scope — nothing to review.") and write nothing.

## Phase 2 — Choose read mode

Approximate **4 bytes per token** over the in-scope files.
- `total_tokens ≤ 80_000` → **embed mode** (default for a slice): `Read` each file once and build a single line-numbered `<CONTEXT mode="embed">` block — the same string for all lenses, so prompt caching applies:
  ```
  <CONTEXT mode="embed">
  === file: src/HikingLog.Application/Stages/Commands/AddStage.cs ===
  1: namespace HikingLog.Application.Stages.Commands;
  2: ...
  === file: src/HikingLog.Api/Stages/StagesController.cs ===
  1: ...
  </CONTEXT>
  ```
  Line numbers MUST match real file lines (reformat `Read`'s `N→` prefix to `N: `).
- Else → **filter-and-read mode**: give each lens a `<FILES>` list it reads itself —
  - architecture → all `.cs`/`.csproj`
  - data-performance → files with EF or async signals (Phase 3)
  - correctness → all `.cs` under `src/`
  - code-quality → all `.cs`/`.csproj`
  - tests → files under `tests/`, plus the `src/` handlers/validators/controllers they cover

Record `mode`, `files_count`, `total_tokens` for the header.

## Phase 3 — Relevance classification

Cheap `Grep` passes on the resolved list:

| Signal | Pattern |
|---|---|
| EF Core | `DbSet<\|IQueryable<\|ToListAsync\|FindAsync\|SaveChangesAsync\|\.Include\(\|AsNoTracking` |
| Tests | path starts with `tests/` OR filename matches `Tests\.cs$` |
| Async I/O | `\basync\s\|\bawait\s\|\bTask<` |
| Handlers/CQRS | `ICommandHandler<\|IQueryHandler<\|AbstractValidator<` |

Skip rules: always run architecture, correctness and code-quality. Skip data-performance only if BOTH EF and async signals are absent. Skip tests if no test files are in scope **and** no handler/validator/controller is in scope (a new handler without tests is exactly what the tests lens must catch — when production handlers are in scope, also hand the lens the matching `tests/` paths via `Glob` so it can judge coverage). Record run/skip with `TodoWrite` — one item per lens, skipped ones with reason. If the session offers no task-list tool, state the run/skip list in your reply instead.

## Phase 4 — Fan out (single message, parallel)

In **one message**, issue one `Agent` call per surviving lens (`subagent_type` = lens name) so they run concurrently. Each prompt — order matters for the prompt cache:

```
<PREAMBLE>               ← byte-identical across all calls
<CONTEXT mode="embed">   ← embed mode only, byte-identical across all calls
<FILES>                  ← filter-and-read mode only, per lens
<TASK>
You are the <topic> reviewer. Read CLAUDE.md and .claude/rules/backend/*.md first, apply your scan checklist to the files above, and return a JSON array of findings per the schema.
</TASK>
```

**Shared preamble (verbatim):**

```
Mode: <embed | filter-and-read>

## Severity rubric
- CRITICAL — runtime error, data loss, a failing build/format/test gate, or a broken architectural invariant from CLAUDE.md / .claude/rules/backend.
- SIGNIFICANT — wrong behaviour under realistic conditions, a clear performance regression, or a violation of a documented project rule.
- MINOR — readability, style, light duplication, missing-but-non-critical test.

## Output
Return ONLY a single fenced JSON code block — an array of finding objects. No prose around it.
[
  {
    "severity": "critical" | "significant" | "minor",
    "title": "one-sentence problem statement",
    "file": "src/or/tests/relative/path.cs",
    "line": <int>,
    "excerpt_lines": [<startLine>, <endLine>],
    "why": "what is wrong, what breaks, under what conditions",
    "fix": "concrete fix in 1–2 sentences"
  }
]
If nothing found, return [].

## Hard rules
- Only analyze .cs / .csproj files under src/ and tests/.
- In embed mode, treat the <CONTEXT> block as the source of truth — do not Read files already present in it.
- Every finding must cite a real file:line that exists in the context provided.
- No soft language ("might", "could", "consider"). State what is wrong.
- Stay in your lane — do not report topics owned by another lens.
```

`subagent_type`: `backend-reviewer-architecture`, `backend-reviewer-data-performance`, `backend-reviewer-correctness`, `backend-reviewer-code-quality`, `backend-reviewer-tests`.

If a lens returns malformed output, retry once with "Return ONLY the JSON array, nothing else." If still malformed, log it in the header and continue.

**If a lens agent is missing from the registry** — the usual cause is that its definition was created in *this* session and the registry has not picked it up yet — do not abandon the review. Spawn a read-only `Explore` agent in its place, inlining that lens's scan checklist, severity rubric and output schema (copy them from `.claude/agents/backend-reviewer-<lens>.md`) into the prompt, and add a `"lens"` field to each finding so you can still attribute it. Say in the report header which mechanism ran (`lens agents` | `Explore fallback`). The registry normally catches up within the same session, so a later round can use the real lenses.

## Phase 5 — Verification pass

For every `critical`/`significant` finding: `Read` the cited file at `[line-5, line+15]` and confirm the content matches the finding's claim; if unrelated or already handled elsewhere in the file, **drop** it (not downgrade). Count drops per lens for the header. Leave `minor` findings as advisory.

## Phase 6 — De-duplicate

Group by `(file, line ± 3, fingerprint of title)` (fingerprint = lowercased title, punctuation stripped, first 6 distinct content words). Per group: severity = max; `caught_by` = sorted unique lenses; `why` = longest, with unique sentences from the others appended; `fix` = most specific.

## Phase 7 — Assign IDs and render

Sort by severity (CRITICAL→SIGNIFICANT→MINOR), then file, then line. Assign `BR-001…`. Header: timestamp (`Get-Date -Format "yyyy-MM-dd HH:mm"` in PowerShell, `date +"%Y-%m-%d %H:%M"` in bash), scope description (e.g. `working tree on feature/stages-slice (14 files, ~22k tokens)`), `Mode:`, `Lenses run:`, `Lenses skipped:` (reason or `—`), `Findings:` `<C> CRITICAL · <S> SIGNIFICANT · <M> MINOR · <D> dropped`. Then per finding:

```markdown
### BR-001 — <title>
**File:** `<file>:<line>`
**Caught by:** <Lens A>(, <Lens B> — merged)

```csharp
// :<startLine>–:<endLine>
<excerpt — only lines startLine..endLine, with `<n>: ` prefix>
```

**Why it matters:** <why>

**Fix:** <fix>

---
```

Then an **Index by file** table (`| File | C | S | M | Findings |`) and an **Action checklist** (`- [ ] **BR-001** CRITICAL — <short title> in <basename>:<line>`).

JSON sidecar: `version` 1, `generated_at` (ISO 8601 + tz), `scope`, `mode`, `lenses_run`, `lenses_skipped`, `summary` (counts), `findings[]` (id, severity, title, file, line, excerpt_lines, why, fix, caught_by).

## Phase 8 — Deliver

Create `reviews/` if missing (it is gitignored). Basename `<yyyy-MM-dd-HHmm>_backend_review`. `Write` `reviews/<basename>.md` and `reviews/<basename>.json` from the same finding list. Print the two paths, the executive summary line, and the action checklist verbatim. When invoked by `ship-slice`, also return the CRITICAL and SIGNIFICANT findings inline — that skill applies them as "confirmed" fixes. Do not edit code and do not commit.

## Hard rules

- Never analyze code yourself. Delegate to the lens subagents. Phase-5 verification reads are the only exception.
- Never invent findings. Drop findings whose citations don't verify (a miss = drop, not downgrade).
- Never analyze files outside `src/` and `tests/` or non-`.cs`/`.csproj` files.
- Markdown and JSON must be rendered from the same internal finding list in a single pass.
- All lens calls in Phase 4 go in a **single message** so they run in parallel and share the prompt cache.
- This skill is read-only: no `Edit`/`Write` outside `reviews/`, no `git add`, no commit.
