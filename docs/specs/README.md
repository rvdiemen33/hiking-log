# Feature specs

Working specs for spec-driven development. A spec is written **before** the code, reviewed against
this repo's own conventions, approved by a human, and then implemented by the existing build
orchestrators. It is an **ephemeral artifact**: once the feature ships, whatever is durable moves to
`.claude/functional-plan.md` (domain model, endpoints, business rules) or to `docs/adr/` (lasting
technical decisions), and the spec is archived under `docs/specs/archive/`.

## Lifecycle

```
GitHub issue (User story form = Definition of Ready)
      │
      ▼
spec-create  →  draft  →  spec-review  →  reviewed  →  (human sets approved)
                                                              │
                                                              ▼
                                                      spec-implement
                                                     (ship-slice builds,
                                                      reviews, commits)
                                                              │
                                              implementing → implemented
                                                              │
                                                              ▼
                                                        spec-close
                                                 (harvest → archive → closed)
```

| Status | Meaning | Set by |
|---|---|---|
| `draft` | Written, not yet through the review gate — or reopened (see below) | `spec-create`, or whoever reopens |
| `reviewed` | Reviewed with no blockers left | `spec-reviewer` (gate mode) |
| `approved` | The human accepted the review notes; implementation is unlocked | **you**, by hand |
| `implementing` | Build in progress | `spec-implement` |
| `implemented` | Delivered and verified | `spec-implement` |
| `closed` | Harvested and archived | `spec-close` |

`approved` is deliberately manual — it is the one transition no skill makes for you. The status also
runs **backwards**; see *Reopening a spec*.

## Where a spec starts

A spec normally starts from a GitHub issue created with the **User story** form
(`.github/ISSUE_TEMPLATE/user-story.yml`). The form is the Definition of Ready: business value, SMART
acceptance criteria, sized to one feature branch, no blocking dependencies. The issue number is the
thread through the spec (`{issue}-{slug}.md`), the branch, the commits and the PR. An issue is
recommended, not required — a small feature can go straight to `spec-create` without one.

## Conventions

- **Filename**: `{issue}-{slug}.md` when there is a GitHub issue (`17-stage-notes.md`), otherwise
  `{slug}.md`. The slug is kebab-case, derived from the entity or feature name.
- **Frontmatter**: `status`, optional `issue`, `created` (ISO date).
- **`TO CONFIRM: {question}`** marks any value that was guessed or needs a human decision. The marker
  is grep-able and `spec-reviewer` escalates every surviving one to a blocker, so an unconfirmed value
  cannot reach `approved` disguised as prose. Append `(owner: {who})` when someone other than the
  person running the flow has to answer — the reviewer lists the owner with the blocker.
- **Every spec has an `## Impact / Affected areas` section.** It is what `spec-reviewer`'s eighth
  dimension judges: what happens to existing data, existing behaviour and existing clients. A
  greenfield slice writes "none identified"; a change to an existing table, handler or endpoint spells
  out the data strategy, the behaviour delta and any breaking change.
- **Specs are committed, and committed early.** `spec-create` commits the draft the moment it is
  written (`docs(spec): add {slug} draft`), before any code exists, so the history proves the design
  predates the implementation. `spec-review` commits the gate result, `spec-implement` the status
  flips, `spec-close` the retirement. The one edit you make by hand — `approved` — rides along with
  `spec-implement`'s first commit. Every spec-flow commit touches only the spec file (plus what the
  skill explicitly owns) and uses the `docs(spec)` scope.

## Reopening a spec

Work flows back when a later phase learns something an earlier phase should have caught. The status
runs backwards, the spec is edited, and that edit is committed on its own with a `Reopens-Spec`
trailer so reopenings stay countable:

```
docs(spec): reopen {slug} — {one-line reason}

Reopens-Spec: docs/specs/{name}.md
```

| Situation | Status | What happens |
|---|---|---|
| Implementation finds the spec is wrong or incomplete | `implementing → draft` | `spec-implement` stops; fix the spec, then `spec-review` again |
| Code review or the PR turns up a **design** finding (wrong abstraction, missed edge case) | `implemented → draft` | Fix the spec, not the code — a code patch for a design finding hides the gap |
| The PR is rejected on **mechanics** (failing test, naming, null check) | stays `implemented` | Fix the code on the branch; nothing is reopened |
| The reviewer rejects the approach, not a detail | stays `draft` | Rewrite via `spec-create`, keep the filename |
| The story turns out not to meet the Definition of Ready | spec dropped | `git rm` the spec, commit `docs(spec): drop {slug} — story not ready` |

A reopened spec goes through the full gate again: `spec-review` → `reviewed` → you set `approved`.
`git log --oneline --grep="^Reopens-Spec:"` counts the reopenings (`impl_corrections`); a rising count
says the spec phase is letting things through, not that the build phase is weak.

## Skills

`spec-create` · `spec-review` · `spec-implement` · `spec-close` — see `.claude/skills/` and the
**Spec-driven development** section in `CLAUDE.md`.
