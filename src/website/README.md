# @flowthru/docs

Marketing site + documentation for [Flowthru](https://github.com/chaoticgoodcomputing/flowthru),
built with [Astro](https://astro.build) and [Starlight](https://starlight.astro.build).

## Quick start

```bash
pnpm install
pnpm dev       # http://localhost:4321/flowthru/
pnpm build     # → dist/
pnpm preview   # serve the production build locally
```

## Site layout

| Route       | Source                                |
| ----------- | ------------------------------------- |
| `/`         | `src/pages/index.astro` (marketing)   |
| `/docs/*`   | `src/content/docs/docs/**/*.{md,mdx}` (Starlight) |

## Structure

```
src/
├── assets/                   Logo + static images
├── pages/
│   └── index.astro           Marketing homepage at /
├── content/
│   ├── config.ts             Content collection schema
│   └── docs/docs/            Starlight content, served at /docs/*
│       ├── index.mdx
│       ├── tutorials/        Diátaxis: Tutorials
│       ├── how-to/           Diátaxis: How-to guides
│       ├── explanation/      Diátaxis: Explanation
│       ├── reference/        Auto-generated from C# XML (see CONTRIBUTING.md)
│       └── extensions/       Per-extension docs
└── styles/
    ├── marketing.css         Marketing homepage styles
    └── flowthru.css          Starlight theme overrides
```

## Integration

See [CONTRIBUTING.md](./CONTRIBUTING.md) for this context's conventions and
glossary — the ingest pipeline, the frontmatter contract, the link interceptor,
and the nx caching rules that keep the build honest.

The moving parts:

- **nx wiring** — `src/website/project.json`. `implicitDependencies: ["docs"]`
  is load-bearing: nx does not synthesize graph edges from target-level
  `dependsOn`, so without it a docs change would not mark the site affected.
- **C# → Markdown reference** — `scripts/docfx-metadata.sh` generates
  `docs/reference/src/` from XML doc comments; `scripts/ingest-docs.mjs` copies
  it into the content tree, synthesizing frontmatter that docfx does not emit.
- **Deployment** — GitHub Pages, via this package's own workflow.
