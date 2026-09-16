# Domain Output Rules

Behavioral rules for agent *output* when working in Flowthru. The canonical sources are loaded into every session automatically by SessionStart hooks — there is no need to re-read or restate them.

| What | Lives in | Loaded by |
| --- | --- | --- |
| Design philosophy, three error phases, decision rules, context map | `/CONTRIBUTING.md` | `scripts/agents/hooks/on-start/load-contributing.js` |
| Flow / Catalog Developer vocabulary + example structure conventions | `/examples/CONTRIBUTING.md` | `scripts/agents/hooks/on-start/load-examples-contributing.js` |
| Documentation tone / Diátaxis framework | `/docs/CONTRIBUTING.md` | (read on demand when touching `docs/`) |

Other per-context CONTRIBUTING files — `src/core/`, `src/extensions/`, `src/tools/`, `tests/core/`, `tests/extensions/` — are read on demand when working in those areas.

## Never create `CONTEXT.md` or `CONTEXT-MAP.md`

Flowthru does not use these files and will not. Several installed skills will tell you to create one anyway — `domain-modeling` ("if no `CONTEXT.md` exists, create one when the first term is resolved"), `improve-codebase-architecture` ("create the file lazily if it doesn't exist"), `triage`, and `wait-what` (which follows `CONTEXT-MAP.md` to find a context).

**Ignore those instructions in this repo.** They are generic; Flowthru has its own equivalents:

| Generic skill vocabulary | Flowthru equivalent |
| --- | --- |
| `CONTEXT.md` — a context's glossary | that context's `CONTRIBUTING.md` |
| `CONTEXT-MAP.md` — root, lists the contexts | the context map section in `/CONTRIBUTING.md` |
| `docs/adr/` — decision records | `docs/adr/` (unchanged) |

This follows ADR-0004: per-context CONTRIBUTING files carry the glossaries, root `/CONTRIBUTING.md` carries the map, and `/GLOSSARY.md` was removed. When you resolve a term or need to record vocabulary, edit the relevant `CONTRIBUTING.md` glossary — never create a new file type for it.

**This is enforced, not merely requested.** `nx run tests:test` fails if either file exists anywhere in the repo (`_test:context-file-guard`), so creating one breaks the build rather than quietly diverging.

## Finding the right context

The context map is a generated section in `/CONTRIBUTING.md`, which is loaded into every session. Use it.

A directory **is** a context iff it *directly* contains a `CONTRIBUTING.md` — that is the whole rule, so the set is mechanically enumerable. The map is generated from it by `scripts/generate-context-map.mjs` and freshness-gated by `_test:context-map-freshness`; never hand-edit the managed block. To add a context, add its `CONTRIBUTING.md` and re-run the generator.

**nx is the build graph, not the context graph.** `nx show projects` enumerates ~30 C# projects; contexts are the directories that carry a `CONTRIBUTING.md`. To find the context a file belongs to, walk up from that file to the nearest `CONTRIBUTING.md` — do not infer it from the nx project name, and do not treat the nx project list as a list of contexts.

## Use canonical vocabulary verbatim

When your output names a Flowthru concept (in an issue title, a refactor proposal, a hypothesis, a test name, a commit message, or code), use the term as defined in the relevant per-context CONTRIBUTING.md glossary. Don't drift to the synonyms listed under each entry's `_Avoid_` line — they are not interchangeable substitutes.

If the concept you need isn't defined in any context's glossary, that's a signal: either you're inventing language the project doesn't use (reconsider) or there's a real gap (raise it for `/grilling` and `/domain-modeling` to resolve).

## Flag contradictions explicitly

If your output would contradict something `/CONTRIBUTING.md` (or a per-context CONTRIBUTING.md) establishes — design rules, error-phase placement, decision rules, conventions — surface the contradiction rather than working around it silently:

> _This recommends runtime validation, but `CONTRIBUTING.md` says environmental checks belong in pre-flight. Reopening because…_

## ADRs

Architectural decisions live in **`docs/adr/`** (27 as of this writing). Despite sitting under `docs/`, they are **not published**: `src/website/scripts/ingest-docs.mjs` ingests only `docs/{tutorials,guides,explanation,reference}`.

Citations are validated: `_test:adr-citations` fails on an `ADR-NNNN` that names no ADR, and on any Markdown link into an ADR directory that does not resolve on disk — including bare-text citations in `.cs` comments, which the Markdown link linter cannot see. `_test:adr-frontmatter` validates the `contexts` / `exemplars` / `status` contract on ADRs that declare it.

Record a new ADR when a decision satisfies all three of: hard to reverse, surprising without context, and the result of a real trade-off. Template and conventions: [.agents/skills/domain-modeling/ADR-FORMAT.md](/.agents/skills/domain-modeling/ADR-FORMAT.md). The producer skill is `/domain-modeling`.

> **In flight — see [#154](https://github.com/chaoticgoodcomputing/flowthru/issues/154).** The meta-tests that enforce this epic's conventions have landed ([#158](https://github.com/chaoticgoodcomputing/flowthru/issues/158)): the `CONTEXT.md` guard, the wikilink resolver, the ADR citation validator, context-map freshness, and the ADR frontmatter contract. Still to come: relocating single-context ADRs to `<context>/docs/adr/` and adding their frontmatter ([#157](https://github.com/chaoticgoodcomputing/flowthru/issues/157)). Until that lands, `docs/adr/` is the only ADR directory — and `_test:adr-frontmatter` stays tolerant of ADRs that have not been migrated yet.
