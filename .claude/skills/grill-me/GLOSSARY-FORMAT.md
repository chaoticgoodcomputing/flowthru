# Glossary-Section Format

Flowthru's glossaries live as **Glossary** sections at the bottom of per-context CONTRIBUTING.md files (not as a single `/GLOSSARY.md`). Per-context CONTRIBUTING files:

| Context | File |
| --- | --- |
| Flow / Catalog Developer | `/examples/CONTRIBUTING.md` |
| Extension Developer | `/src/extensions/CONTRIBUTING.md` |
| Core Developer | `/src/core/CONTRIBUTING.md` |
| Core test author | `/tests/core/CONTRIBUTING.md` |
| Extension test author | `/tests/extensions/CONTRIBUTING.md` |

Each glossary defines terms specific to its context. Later contexts inherit upstream vocabulary by reference (M2: pointer-only inheritance, no duplication).

## CONTRIBUTING.md structure

Each per-context CONTRIBUTING.md follows this shape:

```md
# Contributing to {Context Name}

{Brief audience-scope paragraph + companion-doc pointers.}

## {Body sections — Diátaxis "explanation": things not easily grokked from source}
...

## Glossary

### Roles
{Role definitions for this context's audience.}

### {Context} Vocabulary
{Terms specific to this context, in thematic order.}
```

Body sections are *explanation*: conventions, patterns, and decision frameworks that the source code alone doesn't make obvious. Things easily grokked from code (a directory structure, a class hierarchy) don't need their own body section — name them in the glossary and move on.

## Entry format — standard term

```md
**Term name**: One or two sentences defining what the term IS in this project's specific context.
_Avoid_: alias-to-avoid, another-alias (optional parenthetical explaining why)
```

Notes:
- **Bold term name + colon + prose body.** Definition is prose, not bullets.
- **`_Avoid_` line is mandatory.** Listing close-but-wrong terms is part of the entry, even when no alias is dangerous — tells future contributors "we considered these and rejected them."
- **Cross-references use `[[Term name]]` wiki-style syntax.** Text-searchable even when not rendered as live links. Use `[[Term name|display text]]` for inline phrasing.

## Entry format — role definition

The first entry in each context's Glossary uses an expanded format that adds a Responsibilities bullet list:

```md
**Role name**: Definition prose — what the role is and what its focus is.

**Responsibilities:**
- Verb-first imperative (e.g., "Write Steps as...", "Author Catalog classes...")
- Each bullet is one concrete responsibility
- 3–5 bullets typical

_Avoid_: alias-to-avoid (parenthetical reasoning)
```

The Responsibilities format is reserved for role definitions only — other entries stay in the standard prose-only format.

## Two-depth entries — same term, multiple contexts

A term may appear in more than one context glossary at different depths. The upstream entry shields readers from machinery the downstream context handles precisely. **DAG** is the canonical case — it appears in `examples/CONTRIBUTING.md` (Flow Dev framing: directed, acyclic, bipartite between Steps and Items) and `src/core/CONTRIBUTING.md` (bipartite arrow/place + Kleisli arrows of `FlowIO`).

Use this pattern only when the depth split is genuinely warranted — typically only for foundational terms that span Flow Dev and Core Dev concerns. Most terms appear once.

## `*Note*:` lines — cross-term disambiguation

When two glossary entries reuse the same word in genuinely different senses (e.g., "category" in **Data category** vs the formal mathematical category in **DAG (Core view)**), add a `*Note*:` line to both entries explaining which sense applies where:

```md
*Note*: "category" here is the classification sense (Raw, Models, etc.); the Core [[DAG]] entry uses "category" in the formal mathematical sense (objects + morphisms). Context disambiguates.
```

Per-entry notes are clearer than a separate disambiguation index — the disambiguation lives where the reader encounters the term.

## Depth rules by context

- **examples/** — definitions stay tight, 1–2 sentences. No FP jargon. The Jupyter-cell analogy for [[Step]] is the kind of grounding to reach for; the formal Kleisli framing is not.
- **src/extensions/** — may run 2–3 sentences when earning weight. Begins to use FP vocabulary precisely (closed sum, applicative composition) because extension authors must reason in those terms when designing extensibility points.
- **src/core/** — rich entries allowed when teaching a concept. May run 3–4 sentences. Uses precise FP terminology without softening — the framework's compile-time safety promise depends on Core Devs reasoning correctly. See `feedback-core-glossary-fp-precision` in agent memory.
- **tests/core/** — captures test-author vocabulary (Architecture test, Laws kit, Law, Test mirror, Example test) plus test categories paralleling the error phases (Design-time test, Pre-flight test, Runtime test).
- **tests/extensions/** — captures extension-test-specific vocabulary (Backend matrix); inherits most testing vocabulary from tests/core via pointer.

## What belongs in a glossary

Include:
- **Emergent patterns** that span multiple types and aren't visible from any one primitive (closed sum, Kleisli arrow, combinator, bipartite arrow/place model, laws kit / law).
- **Project-specific concepts** with non-obvious names (Flow, Catalog Item, Data category, Configuration Item, the three error phases, the API/Error Surface framing).
- **Role definitions** for the Developer roles, in the context whose CONTRIBUTING.md the role primarily targets.
- **Two-depth refinements** of cross-cutting terms when depth differs meaningfully across contexts.

Exclude:
- **Specific framework types** (FlowIO, EffResult, specific storage adapters, the `[FlowthruSchema]` attribute). These self-document via XML doc comments on the classes; enumerating them in the glossary opens the can of glossarizing every type. See `feedback-glossary-patterns-not-types` in agent memory.
- **General programming concepts** (timeouts, async, dependency injection, even "DAG" as a general concept). Only the *Flowthru-specific shape* of a general concept belongs (Flowthru's DAG is bipartite Step/Item — that's specific; "DAG" alone is not).

## General rules

- **Be opinionated.** When multiple words exist for the same concept, pick the best and list others as `_Avoid_` aliases.
- **Flag conflicts explicitly** via `*Note*:` lines.
- **Define what something IS**, not what it does — the glossary is for naming concepts, not explaining mechanism.
- **Group terms thematically within a section** when the order helps reading flow. Strict alphabetical is fine when no thematic grouping suggests itself.
- **Use canonical terms in your own output** — when you reference a term in body prose or another entry's definition, use the bold form and the `_Avoid_`-respecting spelling.

## When to update the glossary

The grill-me skill captures terms inline during a grilling session — when a term resolves, append it to the appropriate context's glossary immediately. Don't batch.
