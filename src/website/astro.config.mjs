// @ts-check
import { defineConfig } from "astro/config";
import starlight from "@astrojs/starlight";

// https://astro.build/config
//
// Site layout:
//   /              → marketing homepage (src/pages/index.astro)
//   /docs/         → Starlight-served documentation
//
// Starlight reads from src/content/docs/. To serve under /docs/* the content
// lives nested at src/content/docs/docs/*. That subtree is populated at build
// time by `scripts/ingest-docs.mjs`, which ingests the canonical `docs/`
// directory at the repo root. See scripts/ingest-docs.mjs for the contract.
export default defineConfig({
  site: "https://chaoticgoodcomputing.github.io",
  base: "/flowthru",

  integrations: [
    starlight({
      title: "Flowthru",
      description:
        "A type-safe, fail-fast data engineering framework for .NET.",
      logo: {
        src: "./src/assets/flowthru-mark.svg",
        replacesTitle: false,
      },
      social: {
        github: "https://github.com/chaoticgoodcomputing/flowthru",
      },
      customCss: ["./src/styles/flowthru.css"],
      sidebar: [
        {
          label: "Tutorials",
          collapsed: false,
          autogenerate: { directory: "docs/tutorials" },
        },
        {
          label: "Guides",
          collapsed: false,
          autogenerate: { directory: "docs/guides" },
        },
        {
          label: "Explanation",
          collapsed: false,
          autogenerate: { directory: "docs/explanation" },
        },
        {
          label: "Reference",
          collapsed: true,
          autogenerate: { directory: "docs/reference" },
        },
      ],
      lastUpdated: true,
      pagination: true,
      tableOfContents: { minHeadingLevel: 2, maxHeadingLevel: 4 },
    }),
  ],
});
