#!/usr/bin/env node
/**
 * Generates the extension catalog for the umbrella `flowthru` skill from the
 * per-extension skill shards, and the vercel-skills plugin manifest that makes
 * each shard individually installable.
 *
 * Source of truth: `src/extensions/<Ext>/SKILL.md` frontmatter —
 *   metadata.flowthru.{extension, surface, capability, register}  (+ top-level `name`)
 * See src/extensions/CONTRIBUTING.md § "The Skill Shard" for the shard contract.
 *
 * Two generated artifacts (both fully derived — never hand-edit):
 *   1. `.claude/skills/flowthru/extensions.md` — the managed block between
 *      <!-- flowthru:extensions:start --> / <!-- flowthru:extensions:end -->
 *      is replaced with a per-surface capability table. Same managed-block
 *      mechanism as scripts/update-example-readmes.mjs.
 *   2. `.claude-plugin/marketplace.json` — declares the umbrella skill plus
 *      every extension shard dir so `npx skills add …/flowthru --skill flowthru-<ext>`
 *      resolves. Regenerated wholesale from the discovered shards, so adding a
 *      shard needs no manual manifest edit.
 *
 * Honesty: a shard missing a required frontmatter field, or declaring a surface
 * outside the known set, is a HARD error naming the file (fail-fast). Nothing is
 * written if any shard is malformed.
 *
 * --check: non-mutating freshness mode for the freshness test. Computes what both
 * artifacts WOULD become and exits 1 if either differs from its committed state,
 * writing nothing. Wired as a test so `nx affected -t test` catches an
 * un-regenerated shard edit; CI's `git diff --exit-code` is the backstop.
 *
 * Usage:
 *   node scripts/generate-skill-extensions.mjs           # write
 *   node scripts/generate-skill-extensions.mjs --check    # verify freshness
 */

import { existsSync, mkdirSync, readdirSync, readFileSync, writeFileSync } from 'node:fs';
import { join, resolve, dirname, relative } from 'node:path';
import { fileURLToPath } from 'node:url';
import yaml from 'js-yaml';

const __dirname = dirname(fileURLToPath(import.meta.url));
const ROOT = resolve(__dirname, '..');

// Shard containers: any shippable package under these roots may carry a
// SKILL.md shard — extensions, plus core packages that are optional add-ons
// from a Flow Developer's perspective (e.g. Flowthru.FUnit).
const SHARD_DIRS = [join(ROOT, 'src', 'extensions'), join(ROOT, 'src', 'core')];
const EXTENSIONS_MD = join(ROOT, '.claude', 'skills', 'flowthru', 'extensions.md');
const MANIFEST = join(ROOT, '.claude-plugin', 'marketplace.json');
const UMBRELLA_SKILL_DIR = './.claude/skills/flowthru';

const BLOCK_START = '<!-- flowthru:extensions:start -->';
const BLOCK_END = '<!-- flowthru:extensions:end -->';

// Surface groups, in render order. A shard's `surface` MUST be one of these keys;
// an unknown surface is a hard error (add it here — with a heading — in the same
// change that introduces it).
const SURFACES = [
  ['format', 'Formats — how bytes serialize'],
  ['medium', 'Media — where bytes live'],
  ['database', 'Databases'],
  ['engine', 'Execution engines'],
  ['step', 'Step hosts'],
  ['testing', 'Testing'],
  ['metadata', 'Metadata & diagnostics'],
];
const KNOWN_SURFACES = new Set(SURFACES.map(([k]) => k));

/** Parse the leading `--- … ---` YAML frontmatter block. */
function parseFrontmatter(text, file) {
  const m = /^---\n([\s\S]*?)\n---/.exec(text);
  if (!m) {
    fail(`${rel(file)}: no YAML frontmatter (expected a leading '--- … ---' block)`);
    return null;
  }
  try {
    return yaml.load(m[1]) ?? {};
  } catch (e) {
    fail(
      `${rel(file)}: invalid frontmatter YAML — ${e.message.split('\n')[0]}. ` +
        `Tip: quote any value containing a colon-space (': '), e.g. description or capability.`,
    );
    return null;
  }
}

function rel(p) {
  return relative(ROOT, p);
}

const errors = [];
function fail(msg) {
  errors.push(msg);
}

/** Discover and validate every extension shard. */
function collectShards() {
  const shards = [];
  const entries = [];
  for (const dir of SHARD_DIRS) {
    if (!existsSync(dir)) {
      fail(`shard container not found: ${rel(dir)}`);
      continue;
    }
    for (const entry of readdirSync(dir, { withFileTypes: true })) {
      if (entry.isDirectory()) entries.push(join(dir, entry.name));
    }
  }
  {
    for (const pkgDir of entries.sort((a, b) => a.localeCompare(b))) {
      const skillPath = join(pkgDir, 'SKILL.md');
      if (!existsSync(skillPath)) continue; // not every package has a shard
      const fm = parseFrontmatter(readFileSync(skillPath, 'utf8'), skillPath);
      if (!fm) continue; // parse failure already recorded by parseFrontmatter
      const ft = fm.metadata?.flowthru ?? {};
      const missing = [];
      if (!fm.name) missing.push('name');
      for (const k of ['extension', 'surface', 'capability', 'register']) {
        if (ft[k] === undefined || ft[k] === null || `${ft[k]}`.trim() === '') missing.push(`metadata.flowthru.${k}`);
      }
      if (missing.length) {
        fail(`${rel(skillPath)}: missing required frontmatter field(s): ${missing.join(', ')}`);
        continue;
      }
      if (!KNOWN_SURFACES.has(ft.surface)) {
        fail(
          `${rel(skillPath)}: unknown surface '${ft.surface}' — must be one of ${[...KNOWN_SURFACES].join(', ')} ` +
            `(add it to SURFACES in ${rel(fileURLToPath(import.meta.url))} if it is genuinely new)`,
        );
        continue;
      }
      shards.push({
        dir: `./${relative(ROOT, pkgDir).split('\\').join('/')}`,
        name: `${fm.name}`,
        extension: `${ft.extension}`,
        surface: `${ft.surface}`,
        capability: `${ft.capability}`.trim(),
        register: `${ft.register}`.trim(),
      });
    }
  }
  return shards;
}

/** Escape a value for a Markdown table cell. */
function cell(s) {
  return s.replace(/\|/g, '\\|');
}

/** Render the per-surface capability tables (the managed-block body). */
function renderIndex(shards) {
  const bySurface = new Map();
  for (const s of shards) {
    if (!bySurface.has(s.surface)) bySurface.set(s.surface, []);
    bySurface.get(s.surface).push(s);
  }
  const parts = [];
  for (const [key, heading] of SURFACES) {
    const group = bySurface.get(key);
    if (!group || group.length === 0) continue;
    group.sort((a, b) => a.extension.localeCompare(b.extension));
    parts.push(`### ${heading}`);
    parts.push('');
    parts.push('| Package | Capability | Enable | Deep skill |');
    parts.push('|---------|-----------|--------|-----------|');
    for (const s of group) {
      parts.push(
        `| \`${cell(s.extension)}\` | ${cell(s.capability)} | \`${cell(s.register)}\` | \`--skill ${cell(s.name)}\` |`,
      );
    }
    parts.push('');
  }
  return parts.join('\n').trimEnd();
}

/** Splice the rendered index into the managed block of extensions.md. */
function renderExtensionsMd(shards) {
  if (!existsSync(EXTENSIONS_MD)) fail(`missing ${rel(EXTENSIONS_MD)}`);
  const original = existsSync(EXTENSIONS_MD) ? readFileSync(EXTENSIONS_MD, 'utf8') : '';
  const sIdx = original.indexOf(BLOCK_START);
  const eIdx = original.indexOf(BLOCK_END);
  if (sIdx === -1 || eIdx === -1 || eIdx < sIdx) {
    fail(`${rel(EXTENSIONS_MD)}: missing managed markers ${BLOCK_START} … ${BLOCK_END}`);
    return { path: EXTENSIONS_MD, original, next: original };
  }
  const before = original.slice(0, sIdx + BLOCK_START.length);
  const after = original.slice(eIdx);
  const next = `${before}\n${renderIndex(shards)}\n${after}`;
  return { path: EXTENSIONS_MD, original, next };
}

/** Render the plugin manifest (fully generated). */
function renderManifest(shards) {
  const manifest = {
    '//': 'GENERATED by scripts/generate-skill-extensions.mjs from src/extensions/*/SKILL.md — do not edit by hand.',
    plugins: [
      {
        name: 'flowthru',
        skills: [UMBRELLA_SKILL_DIR, ...shards.map((s) => s.dir)],
      },
    ],
  };
  const original = existsSync(MANIFEST) ? readFileSync(MANIFEST, 'utf8') : '';
  const next = `${JSON.stringify(manifest, null, 2)}\n`;
  return { path: MANIFEST, original, next };
}

function main() {
  const check = process.argv.includes('--check');
  const shards = collectShards();

  if (errors.length) {
    console.error('[generate-skill-extensions] shard validation failed:');
    for (const e of errors) console.error(`  ${e}`);
    console.error('Nothing written.');
    process.exit(1);
  }

  const outputs = [renderExtensionsMd(shards), renderManifest(shards)];

  if (errors.length) {
    console.error('[generate-skill-extensions] generation failed:');
    for (const e of errors) console.error(`  ${e}`);
    process.exit(1);
  }

  const stale = outputs.filter((o) => o.next !== o.original).map((o) => rel(o.path));

  if (check) {
    if (stale.length) {
      console.error('[generate-skill-extensions] stale — re-run `node scripts/generate-skill-extensions.mjs`:');
      for (const f of stale) console.error(`  ${f}`);
      process.exit(1);
    }
    console.log(`[generate-skill-extensions] ${shards.length} shard(s) in sync. ✓`);
    return;
  }

  for (const o of outputs) {
    if (o.next !== o.original) {
      mkdirSync(dirname(o.path), { recursive: true });
      writeFileSync(o.path, o.next, 'utf8');
      console.log(`  updated ${rel(o.path)}`);
    }
  }
  console.log(`[generate-skill-extensions] ${shards.length} shard(s) → ${outputs.length} artifact(s).`);
}

main();
