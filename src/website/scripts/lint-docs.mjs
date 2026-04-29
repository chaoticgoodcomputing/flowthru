#!/usr/bin/env node
// Validates ingested doc frontmatter against Flowthru's contract.
//
// Runs between `_ingest-docs` and `build` so a contract violation surfaces
// fast — before Astro/Starlight is invoked — and points at the *source*
// path the contributor would actually edit (under docs/), not the ingested
// copy (under src/website/src/content/docs/docs/).
//
// Contract:
//   - title: non-empty string (required by Starlight)
//   - description: non-empty string (Flowthru tightens Starlight's optional)
//
// Starlight performs full schema validation at build time; this script
// catches the common shape errors fast with friendlier messages.

import { readdir, readFile } from "node:fs/promises";
import { join, relative, resolve } from "node:path";
import { dirname } from "node:path";
import { fileURLToPath } from "node:url";
import { parse as parseYaml } from "yaml";

const HERE = dirname(fileURLToPath(import.meta.url));
const PROJECT_ROOT = resolve(HERE, "..");
const REPO_ROOT = resolve(PROJECT_ROOT, "..", "..");
const INGESTED = resolve(PROJECT_ROOT, "src", "content", "docs", "docs");
const DOCS_SRC = resolve(
  process.env.FLOWTHRU_DOCS_DIR ?? resolve(REPO_ROOT, "docs"),
);

const FRONTMATTER_RE = /^---\r?\n([\s\S]*?)\r?\n---\r?\n?/;

async function walk(dir) {
  const out = [];
  const entries = await readdir(dir, { withFileTypes: true });
  for (const entry of entries) {
    const full = join(dir, entry.name);
    if (entry.isDirectory()) out.push(...(await walk(full)));
    else if (
      entry.isFile() &&
      (entry.name.endsWith(".md") || entry.name.endsWith(".mdx"))
    )
      out.push(full);
  }
  return out;
}

// Map an ingested path back to its source under docs/. The ingest script
// preserves the relative tree, so this is a straightforward swap.
function sourcePath(ingestedPath) {
  const rel = relative(INGESTED, ingestedPath);
  return join(DOCS_SRC, rel);
}

function validate(frontmatter, file) {
  const errors = [];
  const fm = frontmatter ?? {};

  if (typeof fm.title !== "string" || fm.title.trim() === "") {
    errors.push("missing or empty `title`");
  }
  if (typeof fm.description !== "string" || fm.description.trim() === "") {
    errors.push("missing or empty `description`");
  }

  return errors;
}

async function lint() {
  const files = await walk(INGESTED);
  const failures = [];

  for (const file of files) {
    const raw = await readFile(file, "utf8");
    const match = FRONTMATTER_RE.exec(raw);

    if (!match) {
      failures.push({
        file,
        errors: [
          "no YAML frontmatter found — every doc page must start with a `---` block",
        ],
      });
      continue;
    }

    let parsed;
    try {
      parsed = parseYaml(match[1]);
    } catch (err) {
      failures.push({
        file,
        errors: [`invalid YAML in frontmatter: ${err.message}`],
      });
      continue;
    }

    const errors = validate(parsed, file);
    if (errors.length) failures.push({ file, errors });
  }

  if (failures.length === 0) {
    console.log(`[lint-docs] ${files.length} pages OK`);
    return;
  }

  console.error(`[lint-docs] ${failures.length} page(s) failed validation:\n`);
  for (const { file, errors } of failures) {
    const src = sourcePath(file);
    const display = relative(REPO_ROOT, src);
    console.error(`  ${display}`);
    for (const e of errors) console.error(`    - ${e}`);
    console.error("");
  }
  console.error(
    "Fix the source files under docs/ and re-run. See docs/CONTRIBUTING.md for the frontmatter contract.",
  );
  process.exit(1);
}

lint().catch((err) => {
  console.error("[lint-docs] failed:", err);
  process.exit(1);
});
