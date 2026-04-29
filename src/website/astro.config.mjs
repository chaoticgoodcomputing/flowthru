// @ts-check
import { readFileSync } from "node:fs";
import { fileURLToPath } from "node:url";
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

// Custom Shiki theme aligned to the Notebook tokenizer used by the marketing
// homepage's CodeBlock component. Loaded as a JSON file so it can be edited
// without touching this config.
const notebookTheme = JSON.parse(
  readFileSync(
    fileURLToPath(new URL("./src/styles/shiki-notebook.json", import.meta.url)),
    "utf8",
  ),
);

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
      // Starlight renders fenced code blocks via expressive-code (a Shiki
      // wrapper). Configure its theme here, not via markdown.shikiConfig —
      // expressive-code ignores the latter. Single theme = light-only,
      // matching the site's design direction.
      expressiveCode: {
        themes: [notebookTheme],
        useDarkModeMediaQuery: false,
        // The Notebook palette was hand-tuned against the paper background
        // already; let it through unmodified instead of having expressive-
        // code's contrast adjuster nudge our purples and ambers around.
        minSyntaxHighlightingColorContrast: 0,
        styleOverrides: {
          frames: {
            frameBoxShadowCssValue: "none",
          },
        },
      },
      // Site is light-only.
      //   ThemeProvider — replaced with a version that hard-pins
      //     `data-theme="light"` instead of consulting prefers-color-scheme.
      //     Without this, users on dark-mode OSes see Starlight's dark theme.
      //   ThemeSelect   — replaced with a no-op so users aren't presented with
      //     a control that does nothing useful.
      components: {
        ThemeProvider:
          "./src/components/starlight/LightThemeProvider.astro",
        ThemeSelect: "./src/components/starlight/EmptyThemeSelect.astro",
      },
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
