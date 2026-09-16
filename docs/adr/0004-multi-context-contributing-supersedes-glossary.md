---
status: superseded
superseded_by: /docs/adr/0028-self-declaring-contexts-tiered-glossary-and-adr-frontmatter.md
contexts:
  - /
exemplars: []
---

# Multi-context CONTRIBUTING.md files supersede single /GLOSSARY.md

Per-context vocabulary now lives as **Glossary** sections inside per-context CONTRIBUTING.md files (`/examples/`, `/src/extensions/`, `/src/core/`, `/tests/core/`, `/tests/extensions/`); root `/CONTRIBUTING.md` carries the context map and cross-cutting design rules; `/GLOSSARY.md` has been removed. Rationale: each context's glossary stays small enough to colocate with the conventions it grounds, the audience-scope filter becomes structural (a term leaking into `examples/CONTRIBUTING.md` is a real signal about API-surface clarity), and Matt Pocock's original multi-context grill-with-docs pattern (CONTEXT-MAP + per-context CONTEXT files) maps cleanly onto Flowthru's role-based contributor structure.

Superseded by [ADR-0028](/docs/adr/0028-self-declaring-contexts-tiered-glossary-and-adr-frontmatter.md), which restores a root glossary for repo-wide vocabulary — this ADR removed `/GLOSSARY.md` without leaving anywhere for terms that belong to no single context.

Supersedes [ADR 0001](/docs/adr/0001-glossary-split-by-developer-role.md) — the role-sectioning concept survives, but moves from a single file to per-context CONTRIBUTING.md files.
