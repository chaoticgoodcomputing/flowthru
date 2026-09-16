---
status: accepted
supersedes: /docs/adr/0004-multi-context-contributing-supersedes-glossary.md
contexts:
  - /
exemplars:
  - /CONTRIBUTING.md
  - /scripts/generate-context-map.mjs
  - /scripts/_test/_domain.mjs
  - /scripts/_test/adr-frontmatter.mjs
  - /scripts/_test/context-file-guard.mjs
  - /scripts/_test/wikilink-terms.mjs
  - /scripts/_test/adr-citations.mjs
---

# Contexts are self-declaring, the glossary is tiered, and ADRs carry enforced frontmatter

A directory **is** a context iff it *directly* contains a `CONTRIBUTING.md`. That one rule makes the context set mechanically enumerable, makes growth opt-in, and means a package cannot own an ADR until it has a `CONTRIBUTING.md` — a deliberate forcing function. Root `/CONTRIBUTING.md` regains a glossary for repo-wide vocabulary and carries a *generated* map of every context. Every ADR declares `contexts`, `exemplars` and `status` in frontmatter, and five meta-tests enforce the whole arrangement, because a convention nothing checks is a convention that rots.

This supersedes [ADR-0004](/docs/adr/0004-multi-context-contributing-supersedes-glossary.md) and extends [ADR-0007](/docs/adr/0007-tools-as-development-context.md).

## What went wrong under the previous arrangement

**Vocabulary flowed uphill.** Of 11 glossary terms in `src/extensions/CONTRIBUTING.md`, only 3 were extensions-owned; the rest were Core or repo-wide concepts that had simply landed there. `src/core/CONTRIBUTING.md` could not state the Core Developer's two primary responsibilities without linking *down* into the extensions tier for **API Surface** and **Error Surface**. [ADR-0004](/docs/adr/0004-multi-context-contributing-supersedes-glossary.md) removed `/GLOSSARY.md` without leaving anywhere for genuinely repo-wide terms to live, so they silted up in whichever context was written next.

**Nothing verified that an ADR was practised.** The concurrency ADR ([now /src/core/docs/adr/0006](/src/core/docs/adr/0006-concurrency-conflict-relation-and-resource-profiles.md)) declared 3 packages in its anchor-code section while being cited in source by 8. Only 12 of 27 ADRs carried a governed-code section at all, under three different headings.

**`CONTEXT.md` had no defense.** [ADR-0004](/docs/adr/0004-multi-context-contributing-supersedes-glossary.md) recorded the substance of Flowthru's per-context arrangement but never said the words "do not create a `CONTEXT.md`" — and four vendored skills under `.agents/skills/` actively instruct agents to create one, mid-task, forever.

## Decided

- **A context is a directory directly containing a `CONTRIBUTING.md`.** Contexts are minted **lazily** — a directory becomes one when it accrues vocabulary or decisions with nowhere else to live. `src/website` and the Python and Google.Sheets extension packages were minted this way; the set grows toward roughly 22 as packages follow.

- **`docs/` is the single exclusion from ADR ownership.** It is a vocabulary context, but documentation decisions bind every contributor in every context, and the alternative path is `docs/docs/adr/`. `tests/core` and `tests/extensions` are full ADR-owning contexts.

- **No `CONTEXT.md` or `CONTEXT-MAP.md`, ever.** Per-context `CONTRIBUTING.md` is Flowthru's equivalent; root carries the map. Enforced by `_test:context-file-guard`, which scans vendored trees too, since a confused agent could drop one there.

- **Root regains a glossary**, partially reversing [ADR-0004](/docs/adr/0004-multi-context-contributing-supersedes-glossary.md). Where a term genuinely means different things to different readers it is disambiguated in the **entry name** — `Runtime error (phase)` vs `Runtime error (Core Developer)` — never by reciprocal prose notes. Root vocabulary never references a context's.

- **ADR frontmatter carries `contexts`, `exemplars`, `status`.** `contexts` is *direct* application only: if a decision governs how Core interacts with extensions, Core is the context and the conforming extensions are `exemplars`. One context ⇒ the ADR lives in that context's `docs/adr/`; more than one ⇒ root.

- **`exemplars` is what makes an ADR falsifiable.** An accepted decision with nothing demonstrating it is a claim, not a decision.

- **ADRs are repo-only.** Never built into the docs site. Documentation must not reference a specific ADR — only that an ADR directory exists. The sole exception is **code comments**, and therefore the reference documentation generated from them.

- **Numbering is per-directory; citations carry the path.** ADRs are renumbered on relocation, so a bare `ADR-NNNN` no longer identifies anything — five ADR directories now each start at `0001`. Every citation is therefore a markdown link carrying the full root-anchored path. This is a transitive requirement, not a style preference: markdown must not contain dead links, C# docstrings are built into markdown reference documentation, therefore docstrings must not contain dead links.

## An ADR reaches `main` with the work that implements it

`main` is the canonical state of the repository, so an ADR merged into it
describes something the repository actually **does**. A decision still under
review lives on its own `adr/<slug>` branch and merges when its implementation
merges.

This is what makes `exemplars` mean anything. Backfilling frontmatter surfaced
three ADRs deciding something nothing implements — the diagnostic anchor contract
(no analyzer carries anchor metadata), the Inspector RPC surface (`src/tools`
contains no .NET project at all), and the AWS Lambda harness (its package does not
exist). The first instinct was to add a `proposed` status for them. That was
wrong: a `proposed` ADR on `main` needs an exemption from the non-empty-`exemplars`
rule, and **every exemption is a way to assert a decision without practising it** —
precisely what `exemplars` exists to prevent. One unimplemented ADR sitting on
`main` under a soft status teaches the next reader that the standard is optional.

So there is no `proposed` status. The three ADRs above moved to `adr/*` branches
for review, and the rule on `main` has no escape hatch: **`accepted` means
something demonstrates this.**

`superseded` and `rejected` remain exempt, and they are the only two. Both
describe a decision whose exemplars are expected to be *gone*, not *absent*:
[ADR-0001](/docs/adr/0001-glossary-split-by-developer-role.md) is the proof — its exemplar was `/GLOSSARY.md`, which
[ADR-0004](/docs/adr/0004-multi-context-contributing-supersedes-glossary.md) deleted. Those ADRs are also frozen in place: never relocated,
never renumbered, because their value is being findable at the number the
superseding ADR cites.

## Considered options

**A `CONTEXT.md` per context, as the installed skills assume.** Rejected: it would duplicate `CONTRIBUTING.md`'s role, and the split between "conventions" and "vocabulary" is not one contributors reliably make. One file per context, with a `## Glossary` section, keeps the vocabulary next to the rules that use it.

**A `proposed` status for decisions not yet built.** Rejected — see above. It is the
same shape as any other "temporarily exempt" flag: cheap to add, and it quietly
converts a hard invariant into a default.

**Keeping ADR numbers globally unique and relocating without renumbering.** This would have preserved all 87 existing citations untouched. Rejected in favour of per-directory numbering plus fully-specified paths, which makes each context's ADR sequence readable on its own and makes every citation independently resolvable — including from generated reference documentation, where a bare number is useless to a reader.

**Sharding vocabulary per package.** Explicitly not done. Across all glossaries, zero terms were bound to a named package; the problem was tiering, not granularity.

## Consequences

- The context map in root `/CONTRIBUTING.md` is generated by `scripts/generate-context-map.mjs` and freshness-gated. It is never hand-edited. Drift there is worse than drift elsewhere: it is the file both the meta-tests and every agent read to learn what a context *is*.
- A package wanting its own ADR directory must first write a `CONTRIBUTING.md` with a real glossary. This is the forcing function working, not an obstacle to route around.
- Five gates enforce all of the above and run in the `tests:test` barrel. Two of them found live defects on their first run — three dangling wikilinks, and 19 links into a `.claude/docs/adr/` directory that has never existed.
- Every ADR on `main` is `accepted` with resolving exemplars, or `superseded`. Numbering gaps left by a decision moving to a branch are kept rather than closed: renumbering the survivors would invalidate citations that currently resolve, and a gap costs nothing.
- Because ADRs sit under `docs/` but are never ingested, the website's link interceptor had to learn that `docs/adr/` resolves as repo source rather than as a site page. Left alone it would have minted links to pages that cannot exist.
