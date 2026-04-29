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
│       ├── reference/        Auto-generated from C# XML (see INTEGRATION.md)
│       └── extensions/       Per-extension docs
└── styles/
    ├── marketing.css         Marketing homepage styles
    └── flowthru.css          Starlight theme overrides
```

## Integration

See [INTEGRATION.md](./INTEGRATION.md) for the full handoff guide:
how to wire this into the Nx monorepo, the C# → Markdown reference
generator contract, and GitHub Pages deployment.
