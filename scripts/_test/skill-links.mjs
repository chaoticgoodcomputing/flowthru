#!/usr/bin/env node
/**
 * `_test:skill-links` — skill files link to specific repo projects, examples, and
 * source files by ABSOLUTE `https://github.com/chaoticgoodcomputing/flowthru/…`
 * URL, never a repo-relative path (`examples/…`, `src/…`).
 *
 * Why: a skill is read from an INSTALLED COPY in a downstream project (via
 * `npx skills add` or the `dotnet new` template pack), where a repo-relative path
 * resolves to nothing. An absolute GitHub URL is clickable for a human and
 * directly fetchable for an agent. See src/extensions/CONTRIBUTING.md § "The Skill
 * Shard" → "Linking".
 *
 * Scanned: `.claude/skills/flowthru/*.md` (the umbrella + subdocs) and each
 * `src/extensions/<Ext>/SKILL.md` shard.
 *
 * Flagged: a Markdown link `[text](target)` whose target is a repo-relative path
 * into `examples/` or `src/`. Allowed: absolute URLs, anchors, and sibling links
 * inside the same skill (bare `*.md` names — the umbrella's subdocs cross-reference
 * each other and must stay relative so they resolve in the installed copy).
 *
 * Exits non-zero on any violation, each with a fix hint.
 *
 * Usage:
 *   node scripts/_test/skill-links.mjs
 */

import { existsSync, readdirSync, readFileSync } from 'node:fs';
import { join, relative } from 'node:path';
import { ROOT } from './_lib.mjs';

const SKILL_DIRS = [join(ROOT, '.claude', 'skills', 'flowthru')];
const SHARD_DIRS = [join(ROOT, 'src', 'extensions'), join(ROOT, 'src', 'core')];

function skillFiles() {
  const files = [];
  for (const dir of SKILL_DIRS) {
    if (!existsSync(dir)) continue;
    for (const name of readdirSync(dir)) {
      if (name.endsWith('.md')) files.push(join(dir, name));
    }
  }
  for (const dir of SHARD_DIRS) {
    if (!existsSync(dir)) continue;
    for (const entry of readdirSync(dir, { withFileTypes: true })) {
      if (!entry.isDirectory()) continue;
      const p = join(dir, entry.name, 'SKILL.md');
      if (existsSync(p)) files.push(p);
    }
  }
  return files.sort();
}

// Markdown inline link: [text](target ...optional "title").
const LINK_RE = /\[[^\]]*\]\(([^)]+)\)/g;
// A repo-relative link target reaching into a project/example/source tree —
// tolerates leading ./ , ../ , and a leading / (root-absolute).
const REPO_RELATIVE_RE = /^(?:\.{1,2}\/)*\/?(?:examples|src)\//;

const violations = [];
for (const file of skillFiles()) {
  const lines = readFileSync(file, 'utf8').split('\n');
  lines.forEach((line, i) => {
    for (const m of line.matchAll(LINK_RE)) {
      const target = m[1].trim().split(/\s+/)[0]; // drop optional "title"
      if (/^(?:https?:|mailto:|#)/.test(target)) continue; // absolute / anchor
      if (REPO_RELATIVE_RE.test(target)) {
        violations.push({ file: relative(ROOT, file), line: i + 1, target });
      }
    }
  });
}

if (violations.length === 0) {
  console.log('_test:skill-links — all skill links to repo projects/examples/files are absolute. ✓');
  process.exit(0);
}

console.error(
  `\n${violations.length} skill-link violation(s) — link repo projects/examples/files by absolute github.com URL:\n`,
);
for (const v of violations) {
  console.error(`  ${v.file}:${v.line}  →  [...](${v.target})`);
}
console.error(
  '\n  Fix: https://github.com/chaoticgoodcomputing/flowthru/blob/main/<path> (file) ' +
    'or /tree/main/<path> (directory).',
);
console.error(
  '  Sibling links inside the same skill (e.g. catalog-developers.md) stay relative and are allowed.\n',
);
process.exit(1);
