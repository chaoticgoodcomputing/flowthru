# Contributing to the Flowthru Website

This document is for **Website Developers** — maintaining the Astro/Starlight site that publishes Flowthru's documentation. Where a Documentation Developer writes the prose under `docs/`, a Website Developer maintains the machinery that turns it into a deployed site: ingestion, frontmatter validation, link resolution, theming, and the nx wiring that keeps all of it honest.

**Audience scope:** assumes the repo-wide glossary in [/CONTRIBUTING.md](/CONTRIBUTING.md) and the tone and Diátaxis conventions in [/docs/CONTRIBUTING.md](/docs/CONTRIBUTING.md). Nothing here requires .NET knowledge — this is the one context in the repo that is Node/Astro/TypeScript rather than C#, which is why it is a context at all under the bar ADR-0007 set (structurally different artifacts: an application, not a library; `"private": true`; not in `Flowthru.slnx`; deployed by its own workflow).

## The canonical direction

`docs/` is the source of truth; `src/website/src/content/docs/docs/` is a **generated copy**. Every rule below follows from that one fact:

- Never edit the ingested copy. It is gitignored (except a hand-authored `index.mdx`) and overwritten on every build.
- Every error message a contributor sees must name the file under `docs/` they would edit, never the ingested path. This is [[Source-path mapping]], and it is the difference between an actionable error and a confusing one.
- nx targets list the **source** docs as inputs, never the ingested output — see [[Source-inputs-not-ingested-outputs]].

## Honoring Fail-Fast in the docs pipeline

The site build is the documentation's pre-flight. Errors belong as early in it as they can be caught:

- **Ingest time** — missing frontmatter in a hand-written section fails immediately, with a source path. Synthesis is deliberately *not* a fallback there; see [[Passthrough vs. synthesize]].
- **Lint time** (`_lint-docs`) — the [[Frontmatter contract]] is checked before Astro is invoked at all, so a contract violation is a fast, legible failure rather than a stack trace from a content collection.
- **Build time** — the Starlight link validator fails the build on broken internal links. This is why [[Build honesty]] matters: a cached, incremental build can skip pages *and* their link checks, so the build is forced.

A change that lets a docs defect reach the deployed site, rather than failing the build, is a regression in exactly the way `/CONTRIBUTING.md` means it.

## Glossary

### Roles

**Website Developer**: The role that maintains the Astro/Starlight site publishing Flowthru's documentation — the ingest pipeline, frontmatter validation, link resolution, theming, and the nx wiring that binds them. Distinct from a documentation *author*, who writes under `docs/` and never needs to touch this package.
_Avoid_: "docs developer" (ambiguous with the author role), "frontend developer" (the site is a static documentation build, not an application UI), "Tool Developer" (a Tool consumes a Flow Dev's project from outside it; the website consumes only this repo's prose).

### Website Vocabulary

**Ingest**: The `_ingest-docs` step that copies `docs/{tutorials,guides,explanation,reference}` into the Starlight content tree, applying frontmatter rules and the [[Link interceptor]] on the way through. It is a copy, not a mount: the content tree is disposable output, and `docs/` is the only thing a contributor edits.
_Avoid_: "sync" (implies bidirectional), "import" (Astro uses "import" for modules), "copy" (understates the transformation applied in transit).

**Passthrough vs. synthesize**: The two per-file ingest modes. *Passthrough* — the file already opens with a `---` block, so its frontmatter is preserved verbatim and the body left intact. *Synthesize* — no frontmatter, so a title is derived from the first H1 and a description from the first paragraph. Synthesis is permitted **only** under `reference/`, because that subtree is docfx output that ships without YAML. Allowing it in a hand-written section would silently paper over a contributor's missing-frontmatter mistake, converting a design-time error into a plausible-looking page.
_Avoid_: "auto-frontmatter" (hides that it is restricted to one subtree), "fallback" (it is not a fallback — outside `reference/` the absence of frontmatter is a hard failure).

**Link interceptor**: The ingest-time rewriter that makes a link resolve on the deployed site regardless of how an author wrote it. A link resolving **inside** `docs/` stays site-internal; a link that **escapes** `docs/` (into `src/`, `examples/`, a root `CONTRIBUTING.md`) is rewritten to an absolute GitHub URL. Links inside fenced or inline code are never touched. The single exception is `docs/adr/`: ADRs live under `docs/` but are deliberately never ingested, so treating them as site-internal would mint a link to a page that cannot exist — they resolve as repo source instead.
_Avoid_: "link rewriter" (accurate but loses that it *decides* a destination), "link fixer" (the authoring style is already valid; this resolves it for a different render target).

**Frontmatter contract**: `title` plus a non-empty `description` on every ingested page, enforced twice — once by `_lint-docs` before Astro runs, and again by Starlight's `docsSchema()` at build. The duplication is deliberate: the first check produces a legible error naming the source file, the second guarantees nothing slips past if the pipeline is invoked differently.
_Avoid_: "metadata" (too broad — the site reads several kinds), "schema" (that is the Zod schema *implementing* the contract).

**Source-path mapping**: The requirement that any error raised about an ingested file reports the path under `docs/` that a contributor would edit, not the generated path it was found at. Without it, every docs error points at a file that is about to be overwritten.
_Avoid_: "path rewriting" (that is the link interceptor's job), "error mapping" (too generic).

**Source-inputs-not-ingested-outputs**: The nx caching rule that every website target lists the *source* docs under `docs/**` as inputs rather than the ingested copy. nx hashes inputs at the start of a run — before the upstream ingest has written the new copy — so listing the ingested output would compute a cache key from the previous run's state and memoize a stale build.
_Avoid_: "cache inputs" (names the mechanism, not the trap), "input scoping" (too generic to carry the warning).

**Build honesty**: The practice of building with `--force` so Astro's content-layer cache (`node_modules/.astro`) is cleared every time. That cache is hidden state nx cannot see, and Starlight's link validator runs as a *build hook* — an incremental build skips unchanged pages, so the validator skips them too, and a cached build can pass links that are actually broken. nx would then memoize that pass. Forcing the build is what keeps a green result meaningful.
_Avoid_: "clean build" (describes the action, not why a dirty one lies), "cache busting" (implies a performance concern rather than a correctness one).

**Scope-primary IA**: The information architecture in which the site's top-level navigation is organized by *scope* — which package or area a page concerns — with the Diátaxis quadrant as the secondary axis. Recorded as an accepted decision but not yet implemented; `astro.config.mjs` is still flat Diátaxis, and the migration is outstanding work.
_Avoid_: "sidebar structure" (an implementation of the IA, not the IA), "navigation" (too generic).

**Review provenance**: The `review: draft | reviewed` frontmatter field recording whether a human has refined and signed off on a page — not who drafted it. Absent is treated as `draft`, so a forgotten field can never read as reviewed; promotion to `reviewed` is always a manual human action, and any substantive edit flips the page back. `draft` surfaces as a non-blocking pre-flight warning rather than a build failure, because an honestly-labelled draft is better than an unpublished page.
_Avoid_: "status" (collides with ADR frontmatter's `status`), "approved" (implies a gate; this is a signal), "authored-by" (the field deliberately records review, not authorship).
