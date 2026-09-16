# Domain Output Rules

Behavioral rules for agent *output* when working in Flowthru. The canonical sources are loaded into every session automatically by SessionStart hooks — there is no need to re-read or restate them.

| What | Lives in | Loaded by |
| --- | --- | --- |
| Design philosophy, three error phases prose, decision rules, context map | `/CONTRIBUTING.md` | `scripts/agents/hooks/on-start/load-contributing.js` |
| Flow / Catalog Developer vocabulary + example structure conventions | `/examples/CONTRIBUTING.md` | `scripts/agents/hooks/on-start/load-examples-contributing.js` |
| Documentation tone / Diátaxis framework | `/docs/CONTRIBUTING.md` | (read on demand when touching `docs/`) |

Other per-context CONTRIBUTING files — `src/extensions/CONTRIBUTING.md`, `src/core/CONTRIBUTING.md`, `tests/core/CONTRIBUTING.md`, `tests/extensions/CONTRIBUTING.md` — are read on demand when working in those areas.

## Use canonical vocabulary verbatim

When your output names a Flowthru concept (in an issue title, a refactor proposal, a hypothesis, a test name, a commit message, or code), use the term as defined in the relevant per-context CONTRIBUTING.md glossary. Don't drift to the synonyms listed under each entry's `_Avoid_` line — they are not interchangeable substitutes.

If the concept you need isn't defined in any context's glossary, that's a signal: either you're inventing language the project doesn't use (reconsider) or there's a real gap (raise it for `/grill-me` to resolve).

## Flag contradictions explicitly

If your output would contradict something `/CONTRIBUTING.md` (or a per-context CONTRIBUTING.md) establishes — design rules, error-phase placement, decision rules, conventions — surface the contradiction rather than working around it silently:

> _This recommends runtime validation, but `CONTRIBUTING.md` says environmental checks belong in pre-flight. Reopening because…_

## ADRs

Architectural decisions live in `.claude/docs/adr/` (NOT `docs/adr/`, which is reserved for the public-facing Flowthru website). The producer skill (`/grill-me`) appends new ADRs when a decision satisfies all three of: hard to reverse, surprising without context, and the result of a real trade-off. See [.claude/skills/grill-me/ADR-FORMAT.md](/.claude/skills/grill-me/ADR-FORMAT.md) for the template and conventions.
