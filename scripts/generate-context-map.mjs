#!/usr/bin/env node
/**
 * Generates the context map in root `/CONTRIBUTING.md` from the repo's actual
 * directory layout.
 *
 * Source of truth: the filesystem. Per #154, **a directory is a context iff it
 * DIRECTLY contains a `CONTRIBUTING.md`** — which makes the context set
 * mechanically enumerable, makes growth opt-in, and means a package cannot own
 * an ADR until it has a `CONTRIBUTING.md` (a deliberate forcing function).
 *
 * Because the set is derivable, a hand-maintained map is guaranteed to drift.
 * This generator writes it, and `--check` gates the committed copy against a
 * fresh derivation — the same generate-then-freshness-gate pattern as
 * `scripts/generate-skill-extensions.mjs` (ADR-0025) and the `#region docs:`
 * splicing in `scripts/sync-doc-snippets.mjs` (ADR-0009).
 *
 * The managed block between the two sentinels is fully generated — never
 * hand-edit it. Prose ABOUT the contexts (the roles section) stays outside the
 * block and is maintained by hand.
 *
 * The "Owns ADRs" column encodes #154's single exclusion: `docs/` may not own
 * ADRs — documentation decisions are repo-wide, and `docs/docs/adr/` would be
 * the only alternative. Every other context may.
 *
 * --check: non-mutating freshness mode for `_test:context-map-freshness`.
 * Computes what the block WOULD become and exits 1 if it differs, writing nothing.
 *
 * Usage:
 *   node scripts/generate-context-map.mjs           # write
 *   node scripts/generate-context-map.mjs --check   # verify freshness
 */

import { existsSync, readFileSync, writeFileSync } from 'node:fs';
import { join } from 'node:path';
import { ROOT } from './_test/_lib.mjs';
import { contexts } from './_test/_domain.mjs';

const CONTRIBUTING = join(ROOT, 'CONTRIBUTING.md');
const BLOCK_START = '<!-- flowthru:contexts:start -->';
const BLOCK_END = '<!-- flowthru:contexts:end -->';

/** Render the managed-block body: one row per context. */
function renderMap(all) {
  const rows = all.map((ctx) => {
    const link = `[${ctx.contributing}](/${ctx.contributing})`;
    const adrs = ctx.ownsAdrs
      ? existsSync(join(ROOT, ctx.adrDir))
        ? `[\`${ctx.adrDir}/\`](/${ctx.adrDir})`
        : 'yes — none yet'
      : 'no';
    return `| \`${ctx.id}\` | ${link} | ${adrs} |`;
  });

  return [
    '',
    `A directory is a context iff it *directly* contains a \`CONTRIBUTING.md\`. This`,
    `table is generated from the repo layout by \`scripts/generate-context-map.mjs\` —`,
    `to add a context, add its \`CONTRIBUTING.md\` and re-run the generator.`,
    '',
    '| Context | Conventions | Owns ADRs |',
    '|---|---|---|',
    ...rows,
    '',
    `${all.length} context(s). A context may own ADRs under its own \`docs/adr/\`;`,
    '`docs/` is the single exclusion, since documentation decisions are repo-wide.',
    '',
  ].join('\n');
}

function main() {
  const check = process.argv.includes('--check');
  const all = contexts();

  if (!existsSync(CONTRIBUTING)) {
    console.error(`[generate-context-map] missing ${CONTRIBUTING}. Nothing written.`);
    process.exit(1);
  }

  const original = readFileSync(CONTRIBUTING, 'utf8');
  const sIdx = original.indexOf(BLOCK_START);
  const eIdx = original.indexOf(BLOCK_END);
  if (sIdx === -1 || eIdx === -1 || eIdx < sIdx) {
    console.error(
      `[generate-context-map] CONTRIBUTING.md: missing managed markers\n` +
        `  ${BLOCK_START}\n  ${BLOCK_END}\n` +
        'Add both sentinels around the context map section. Nothing written.',
    );
    process.exit(1);
  }

  const before = original.slice(0, sIdx + BLOCK_START.length);
  const after = original.slice(eIdx);
  const next = `${before}\n${renderMap(all)}\n${after}`;

  if (check) {
    if (next !== original) {
      console.error(
        '\n_test:context-map-freshness — the context map in CONTRIBUTING.md is stale.\n\n' +
          `  Derived ${all.length} context(s): ${all.map((c) => c.id).join(', ')}\n\n` +
          '  Fix: run `node scripts/generate-context-map.mjs` and commit the result.\n' +
          '  A context appears or disappears when a CONTRIBUTING.md is added or removed —\n' +
          '  the map is generated, never hand-edited.\n',
      );
      process.exit(1);
    }
    console.log(`_test:context-map-freshness — context map matches ${all.length} derived context(s). ✓`);
    return;
  }

  if (next === original) {
    console.log(`[generate-context-map] ${all.length} context(s) already in sync.`);
    return;
  }

  writeFileSync(CONTRIBUTING, next, 'utf8');
  console.log(`[generate-context-map] updated CONTRIBUTING.md — ${all.length} context(s).`);
}

main();
