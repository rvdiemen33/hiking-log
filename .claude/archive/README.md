# Archive — superseded Claude Code artifacts

Skills, agents, rules and instruction files that were **replaced** live here instead of being deleted, so the
previous version stays readable without digging through git history. Claude Code does not load anything
under `.claude/archive/` — it only discovers `.claude/skills/*/SKILL.md`, `.claude/agents/*.md`,
`.claude/rules/**/*.md` and `.claude/commands/*.md` — so these files are inert. The `review-claude-setup`
skill excludes this folder as well.

When you supersede an artifact: move (or copy, when it was split) the old file into the matching
subfolder here (`agents/`, `skills/<name>/`, `rules/`, or the root for instruction files), add a row to the
table below, and delete the live copy.

| Archived file | Was | Replaced by | Date |
|---|---|---|---|
| `agents/skill-reviewer.md` | Single read-only agent reviewing skills, agents and instruction files (correctness vs `src/`, skill/agent design, instruction consistency) with a BLOCKER/MAJOR/MINOR/NIT list output | The `review-claude-setup` skill orchestrating six `claude-setup-reviewer-*` lens agents; its "correctness vs real code" lens became `claude-setup-reviewer-code-examples`, the design and consistency checks became the `skills`, `agents`, `consistency` and `placement` lenses | 2026-09-17 |
| `integration-testing.md` | Always-loaded instruction file (`@`-included from `CLAUDE.md`) with the integration-test conventions | Path-scoped rule `.claude/rules/backend/backend-integration-testing.md` (loads only for `tests/HikingLog.IntegrationTests/**`), extended with behavioural filter coverage, test naming and child-faker guidance | 2026-09-17 |
