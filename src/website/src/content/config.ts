import { defineCollection, z } from "astro:content";
import { docsSchema } from "@astrojs/starlight/schema";

// Flowthru's docs follow Starlight's standard frontmatter (see
// https://starlight.astro.build/reference/frontmatter/), tightened to require
// `description`. The same schema is enforced at lint time by
// scripts/lint-docs.mjs against ingested output, and again here by Astro
// during the Starlight build — single source of truth.
//
// `review` records whether a human has refined and signed off on the page (not
// who drafted it). Absent is treated as `draft` so a forgotten field can never
// read as reviewed. Promotion to `reviewed` is always a manual human action,
// never automated; any substantive edit flips the page back to `draft`.
// `draft` status is a non-blocking pre-flight warning, surfaced by the
// terminology-lint meta-test — never a build gate.
export const collections = {
  docs: defineCollection({
    schema: docsSchema({
      extend: z.object({
        description: z
          .string({
            required_error: "Flowthru docs require a 'description' frontmatter field.",
          })
          .min(1, "Frontmatter 'description' must not be empty."),
        review: z.enum(["draft", "reviewed"]).default("draft"),
      }),
    }),
  }),
};
