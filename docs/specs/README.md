# Feature specs

Working specs for spec-driven development. A spec is written **before** the code, reviewed against
this repo's own conventions, approved by a human, and then implemented by the existing build
orchestrators. It is an **ephemeral artifact**: once the feature ships, whatever is durable moves to
`.claude/functional-plan.md` (domain model, endpoints, business rules) or to `docs/adr/` (lasting
technical decisions), and the spec is archived under `docs/specs/archive/`.

## Lifecycle

```
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
| `draft` | Written, not yet through the review gate | `spec-create` |
| `reviewed` | Reviewed with no blockers left | `spec-reviewer` (gate mode) |
| `approved` | The human accepted the review notes; implementation is unlocked | **you**, by hand |
| `implementing` | Build in progress | `spec-implement` |
| `implemented` | Delivered and verified | `spec-implement` |
| `closed` | Harvested and archived | `spec-close` |

`approved` is deliberately manual — it is the one transition no skill makes for you.

## Conventions

- **Filename**: `{issue}-{slug}.md` when there is a GitHub issue (`17-stage-notes.md`), otherwise
  `{slug}.md`. The slug is kebab-case, derived from the entity or feature name.
- **Frontmatter**: `status`, optional `issue`, `created` (ISO date).
- **`TO CONFIRM: {question}`** marks any value that was guessed or needs a human decision. The marker
  is grep-able and `spec-reviewer` escalates every surviving one to a blocker, so an unconfirmed value
  cannot reach `approved` disguised as prose.
- Specs are committed: they are the reference during code review, and their history explains why a
  slice looks the way it does.

## Skills

`spec-create` · `spec-review` · `spec-implement` · `spec-close` — see `.claude/skills/` and the
**Spec-driven development** section in `CLAUDE.md`.
