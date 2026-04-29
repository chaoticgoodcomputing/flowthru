import { defineCollection, z } from "astro:content";
import { docsSchema } from "@astrojs/starlight/schema";

// Flowthru's docs follow Starlight's standard frontmatter (see
// https://starlight.astro.build/reference/frontmatter/), tightened to require
// `description`. The same schema is enforced at lint time by
// scripts/lint-docs.mjs against ingested output, and again here by Astro
// during the Starlight build — single source of truth.
export const collections = {
  docs: defineCollection({
    schema: docsSchema({
      extend: z.object({
        description: z
          .string({
            required_error: "Flowthru docs require a 'description' frontmatter field.",
          })
          .min(1, "Frontmatter 'description' must not be empty."),
      }),
    }),
  }),
};
