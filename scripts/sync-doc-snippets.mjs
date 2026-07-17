#!/usr/bin/env node
/**
 * Splices example code snippets into prose surfaces. The consuming half of the
 * doc-snippet pipeline: consumes the fenced snippet files produced by
 * `examples:generate-snippets` under `dist/examples/docs/snippets/` and
 * populates them into two surfaces that transcribe real example source:
 *   - the docs:     `docs/{tutorials,guides,explanation}/**​/*.md`
 *   - the skills:   `.claude/skills/flowthru/*.md` + `src/extensions/*​/SKILL.md`
 * Both are walked in ONE pass so the orphan-snippet gate (below) sees a single
 * consumer set — a `#region docs:` referenced only by a skill is not an orphan.
 *
 * Authoring: drop a one-line sentinel where the code should appear —
 *   <!-- flowthru:snippet docs:<label> -->
 * On first run it is expanded IN PLACE into a managed block:
 *   <!-- flowthru:snippet:docs:<label>:start -->
 *   ```csharp … ```
 *   <!-- flowthru:snippet:docs:<label>:end -->
 * On later runs the block body is refreshed from `dist`. Same managed-block
 * mechanism as scripts/update-example-readmes.mjs.
 *
 * Honesty (both HARD-FAIL, atomic — nothing is written if either trips):
 *   - missing token: a sentinel references a `docs:<label>` with no snippet in
 *     `dist` (the source region was deleted/renamed). Doc points at code that
 *     no longer exists.
 *   - orphan snippet: a `dist` snippet no sentinel or block ever references
 *     (a `#region docs:` marking code that documents nothing).
 * Freshness — a C# edit that wasn't re-synced — is caught separately by
 * `git diff --exit-code docs/` in CI, per the sync-readmes precedent.
 *
 * The source of truth is the C# source; this script only ever transcribes it.
 */

import {
  existsSync,
  readdirSync,
  readFileSync,
  writeFileSync,
} from 'node:fs';
import { join, relative, resolve } from 'node:path';
import { fileURLToPath } from 'node:url';
import { dirname } from 'node:path';

const __dirname = dirname(fileURLToPath(import.meta.url));
const ROOT = resolve(__dirname, '..');
const SNIPPET_DIR = join(ROOT, 'dist', 'examples', 'docs', 'snippets');
const DOC_SECTIONS = ['docs/tutorials', 'docs/guides', 'docs/explanation'];
// Skill surfaces that transcribe example source through the same sentinel
// mechanism: the umbrella skill's subdocs and each per-package shard
// (extensions, plus optional core packages like Flowthru.FUnit).
const SKILL_SECTIONS = ['.claude/skills/flowthru'];
const SHARD_DIRS = [join(ROOT, 'src', 'extensions'), join(ROOT, 'src', 'core')];

/** The per-package shards (`src/{extensions,core}/<Pkg>/SKILL.md`) that exist today. */
function extensionSkillFiles() {
  const out = [];
  for (const dir of SHARD_DIRS) {
    if (!existsSync(dir)) continue;
    for (const entry of readdirSync(dir, { withFileTypes: true })) {
      if (!entry.isDirectory()) continue;
      const p = join(dir, entry.name, 'SKILL.md');
      if (existsSync(p)) out.push(p);
    }
  }
  return out;
}

const SENTINEL_RE = /<!-- flowthru:snippet (docs:[\w.-]+) -->/g;
const BLOCK_START_RE = /<!-- flowthru:snippet:(docs:[\w.-]+):start -->/g;

function fileNameForToken(token) {
  return `${token.replace(/:/g, '-')}.md`;
}
function tokenForFileName(name) {
  // docs-<slug>.md → docs:<slug>  (only the prefix separator was sanitized)
  return name.replace(/\.md$/, '').replace('-', ':');
}
function startMarker(token) {
  return `<!-- flowthru:snippet:${token}:start -->`;
}
function endMarker(token) {
  return `<!-- flowthru:snippet:${token}:end -->`;
}
function wrapBlock(token, payload) {
  return `${startMarker(token)}\n${payload.trimEnd()}\n${endMarker(token)}`;
}

function walk(dir, out) {
  let entries;
  try {
    entries = readdirSync(dir, { withFileTypes: true });
  } catch {
    return out;
  }
  for (const entry of entries) {
    const full = join(dir, entry.name);
    if (entry.isDirectory()) walk(full, out);
    else if (entry.isFile() && entry.name.endsWith('.md')) out.push(full);
  }
  return out;
}

function loadSnippets() {
  const map = new Map(); // token → fenced payload
  if (!existsSync(SNIPPET_DIR)) return map;
  for (const name of readdirSync(SNIPPET_DIR)) {
    if (!name.endsWith('.md')) continue;
    map.set(
      tokenForFileName(name),
      readFileSync(join(SNIPPET_DIR, name), 'utf8'),
    );
  }
  return map;
}

// All tokens referenced by a file, from both bare sentinels and the start
// markers of already-expanded managed blocks.
function referencedTokens(content) {
  const tokens = new Set();
  for (const m of content.matchAll(SENTINEL_RE)) tokens.add(m[1]);
  for (const m of content.matchAll(BLOCK_START_RE)) tokens.add(m[1]);
  return tokens;
}

function refreshBlocks(content, snippets) {
  let out = content;
  for (const m of [...content.matchAll(BLOCK_START_RE)]) {
    const token = m[1];
    const start = startMarker(token);
    const end = endMarker(token);
    const sIdx = out.indexOf(start);
    if (sIdx === -1) continue;
    const eIdx = out.indexOf(end, sIdx + start.length);
    if (eIdx === -1) continue;
    const before = out.slice(0, sIdx);
    const after = out.slice(eIdx + end.length);
    out = `${before}${wrapBlock(token, snippets.get(token))}${after}`;
  }
  return out;
}

function expandSentinels(content, snippets) {
  return content.replace(SENTINEL_RE, (_full, token) =>
    wrapBlock(token, snippets.get(token)),
  );
}

function main() {
  // --check: non-mutating freshness mode for the docs:_test:snippet-freshness
  // target. Computes what each doc WOULD become and fails if any differs from
  // its committed state, writing nothing — so it leaves no working-tree side
  // effects and never false-positives on a dev's unrelated local edits.
  const check = process.argv.includes('--check');
  const snippets = loadSnippets();
  const docFiles = [];
  for (const section of DOC_SECTIONS) walk(join(ROOT, section), docFiles);
  for (const section of SKILL_SECTIONS) walk(join(ROOT, section), docFiles);
  docFiles.push(...extensionSkillFiles());
  docFiles.sort();

  // ── Lint pass (before any write) ──
  const referenced = new Set();
  const missing = []; // { file, token }
  for (const file of docFiles) {
    const content = readFileSync(file, 'utf8');
    for (const token of referencedTokens(content)) {
      referenced.add(token);
      if (!snippets.has(token)) {
        missing.push({ file: relative(ROOT, file), token });
      }
    }
  }
  const orphans = [...snippets.keys()].filter((t) => !referenced.has(t)).sort();

  if (missing.length || orphans.length) {
    if (missing.length) {
      console.error('[sync-doc-snippets] references with no snippet in dist:');
      for (const { file, token } of missing) {
        console.error(`  ${file}: ${token}  (region deleted or renamed?)`);
      }
    }
    if (orphans.length) {
      console.error(
        '[sync-doc-snippets] orphan snippets — #region docs: with no reference:',
      );
      for (const token of orphans) console.error(`  ${token}`);
    }
    console.error('Nothing written. Fix the regions/sentinels and re-run.');
    process.exit(1);
  }

  // ── Write / check pass ──
  const stale = [];
  let changed = 0;
  for (const file of docFiles) {
    const original = readFileSync(file, 'utf8');
    const next = expandSentinels(refreshBlocks(original, snippets), snippets);
    if (next === original) continue;
    if (check) {
      stale.push(relative(ROOT, file));
    } else {
      writeFileSync(file, next, 'utf8');
      changed++;
      console.log(`  updated ${relative(ROOT, file)}`);
    }
  }

  if (check) {
    if (stale.length) {
      console.error('[sync-doc-snippets] stale doc snippets — re-run `nx run docs:sync-snippets`:');
      for (const f of stale) console.error(`  ${f}`);
      process.exit(1);
    }
    console.log(`[sync-doc-snippets] ${snippets.size} snippet(s) in sync. ✓`);
    return;
  }

  console.log(
    `[sync-doc-snippets] ${snippets.size} snippet(s), ${referenced.size} referenced, ${changed} file(s) updated`,
  );
}

main();
