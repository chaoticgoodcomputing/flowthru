#!/usr/bin/env node
/**
 * Deterministic internal-link audit for hand-authored docs. The merge-gating
 * link check (docs:_test:link-audit) — a PURE function of the docs source, so
 * NX caches it honestly and it returns the same result locally and in CI,
 * unlike the Starlight build validator (incremental-cache-sensitive, and it
 * skips relative links entirely under errorOnRelativeLinks:false).
 *
 * Link convention (enforced here, resolved by the ingest interceptor):
 *   - REPO-ROOT-ANCHORED (`/docs/...`, `/src/...`, `/CONTRIBUTING.md`): the
 *     target must exist — a docs page for `/docs/...`, a real repo file/dir
 *     otherwise (the ingest interceptor rewrites the latter to a GitHub URL).
 *   - FILE-LOCAL DOWNWARD (`sibling.md`, `sub/page.md`, `./x.md`): resolves
 *     relative to the file; the target page must exist.
 *   - UPWARD `..` IS BANNED. It's the fragile, ambiguous form (how many levels?
 *     breaks on move) — anchor at the repo root instead. Both allowed forms
 *     render correctly on GitHub *and* on the deployed site.
 *   - External (http/https/mailto) and pure-anchor (#x) links are skipped.
 *   - Links inside fenced or inline code are ignored.
 */

import { readdirSync, readFileSync, existsSync, statSync } from 'node:fs';
import { join, dirname, resolve, relative, posix } from 'node:path';
import { fileURLToPath } from 'node:url';
import { maskCode } from './lib/markdown-code.mjs';

const ROOT = resolve(dirname(fileURLToPath(import.meta.url)), '..');
const SECTIONS = ['docs/tutorials', 'docs/guides', 'docs/explanation'];
const LINK_RE = /\]\(([^)]+)\)/g;

function walk(dir, out = []) {
  let entries;
  try { entries = readdirSync(dir, { withFileTypes: true }); } catch { return out; }
  for (const e of entries) {
    const full = join(dir, e.name);
    if (e.isDirectory()) walk(full, out);
    else if (e.isFile() && e.name.endsWith('.md')) out.push(full);
  }
  return out;
}

// Does a path resolve to a real Starlight page? (exact .md file, dir/index.md, or name.md)
function pageExists(abs) {
  if (existsSync(abs) && statSync(abs).isFile()) return true;
  if (existsSync(join(abs, 'index.md'))) return true;
  if (existsSync(`${abs}.md`)) return true;
  return false;
}

function main() {
  const files = SECTIONS.flatMap((s) => walk(join(ROOT, s)));
  const broken = [];

  for (const file of files) {
    const body = maskCode(readFileSync(file, 'utf8'));
    const rel = relative(ROOT, file);
    for (const m of body.matchAll(LINK_RE)) {
      const full = m[1].trim().split(/\s+/)[0]; // drop link title
      const target = full.split('#')[0]; // drop anchor
      if (!target) continue; // pure anchor (#section)
      if (/^(https?:|mailto:|tel:|\/\/)/i.test(full)) continue; // external

      if (target.includes('../')) {
        broken.push({ rel, target, why: 'uses `..` — anchor at the repo root (/…) or use a file-local downward path' });
      } else if (target.startsWith('/')) {
        const repoRel = posix.normalize(target.replace(/^\/+/, ''));
        const inDocs = repoRel === 'docs' || repoRel.startsWith('docs/');
        if (inDocs && !pageExists(resolve(ROOT, repoRel))) {
          broken.push({ rel, target, why: 'docs page does not exist (renamed or typo?)' });
        } else if (!inDocs && !existsSync(resolve(ROOT, repoRel))) {
          broken.push({ rel, target, why: 'repo path does not exist (source link)' });
        }
      } else if (!pageExists(resolve(dirname(file), target))) {
        broken.push({ rel, target, why: 'target page does not exist (renamed or typo?)' });
      }
    }
  }

  if (broken.length) {
    console.error(`[lint-doc-links] ${broken.length} bad internal link(s):\n`);
    for (const b of broken) console.error(`  ${b.rel}\n     → ${b.target}   [${b.why}]`);
    console.error('\nLinks must be repo-root-anchored (/…) or file-local downward (no `..`), and resolve.');
    process.exit(1);
  }
  console.log(`[lint-doc-links] ${files.length} page(s) scanned, all internal links valid. ✓`);
}

main();
