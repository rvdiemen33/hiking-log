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
      └── runs spec-review itself when          │
          the draft has no TO CONFIRM markers ──┘
                                                              │
                                                              ▼
                                                      spec-implement
                                                  (ship-slice builds, reviews,
                                                   spec-verify checks the code
                                                   against the spec, commits)
                                                              │
                                              implementing → implemented
                                                              │
                                                              ▼
                                                        spec-close
                                            (three close-out questions → harvest
                                                    → archive → closed)
```

| Status | Meaning | Set by |
|---|---|---|
| `draft` | Written, not yet through the review gate — or reopened (see below) | `spec-create`, or whoever reopens |
| `reviewed` | Reviewed with no blockers left | `spec-reviewer` (gate mode) |
| `approved` | The human accepted the review notes; implementation is unlocked | **you**, by hand |
| `implementing` | Build in progress — the branch is the state; an interrupted run is re-derived from it, never resumed from the checkboxes | `spec-implement` |
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
- **Requirements carry ids.** Every requirement is an `R{n}` — the entity and its persistence, each
  command, each query, each business rule; endpoints share their handler's id — and every acceptance
  scenario an `AC{n}.{m}` under its requirement. `## Tests` names the `AC` ids each test covers.
  `spec-verifier` traces the delivered code and tests back to these ids, so they are **stable**: a
  reopened spec appends new ids and strikes dropped ones (`~~R4~~ — dropped, see Review Notes`), it
  never renumbers. A spec written before ids existed is verified with derived ids; the report says so.
- **Every spec has an `## Impact / Affected areas` section.** It is what `spec-reviewer`'s eighth
  dimension judges: what happens to existing data, existing behaviour and existing clients. A
  greenfield slice writes "none identified"; a change to an existing table, handler or endpoint spells
  out the data strategy, the behaviour delta and any breaking change.
- **A clean draft goes through the gate in the same run.** `spec-create` invokes `spec-review` itself
  once its refine pass leaves no `TO CONFIRM:` marker, so such a spec arrives at `reviewed` without a
  second command. A draft that still carries a marker stops at `draft` — the gate would only report
  those markers back as blockers. Either way the gate stays `spec-review`'s: it makes the status flip
  and its own commit, and `spec-create` never touches `status`. Running `spec-review` by hand is
  unchanged and always allowed, which is what a reopened or hand-edited spec needs.
- **Specs are committed, and committed early.** `spec-create` commits the draft the moment it is
  written (`docs(spec): add {slug} draft`), before any code exists, so the history proves the design
  predates the implementation. `spec-review` commits the gate result — so a clean `spec-create` run
  produces two commits, the draft and the gate. `spec-implement` the status
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
| `spec-verify` (in `ship-slice`'s review loop) classifies a finding as **design** — an unspecified change, a contract the spec got wrong, a missed edge case | `implementing → draft` | The skill that observed it (`ship-slice`, or `spec-implement` on its `slice-builder`-only route) commits the reopen and stops; the slice stays as evidence |
| Code review or the PR turns up a **design** finding (wrong abstraction, missed edge case) | `implemented → draft` | Fix the spec, not the code — a code patch for a design finding hides the gap |
| The PR is rejected on **mechanics** (failing test, naming, null check) | stays `implemented` | Fix the code on the branch; nothing is reopened |
| `ship-slice`'s review loop is still not clean after **two fix rounds** | stays `implementing` | `ship-slice` stops and escalates with the open findings; the human decides — never a third automated round |
| The reviewer rejects the approach, not a detail | stays `draft` | Rewrite via `spec-create`, keep the filename |
| The story turns out not to meet the Definition of Ready | spec dropped | `git rm` the spec, commit `docs(spec): drop {slug} — story not ready` |

A reopened spec goes through the full gate again: `spec-review` → `reviewed` → you set `approved`.
`git log --oneline --grep="^Reopens-Spec:"` counts the reopenings (`impl_corrections`); a rising count
says the spec phase is letting things through, not that the build phase is weak.

## Skills

`spec-create` · `spec-review` · `spec-implement` · `spec-verify` · `spec-close` — see `.claude/skills/`
and the **Spec-driven development** section in `CLAUDE.md`. Two agents carry the judgment:
`spec-reviewer` judges the spec (before code), `spec-verifier` judges the code against the spec (after
the build). Both are model-pinned.
