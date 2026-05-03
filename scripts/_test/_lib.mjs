/**
 * Shared workspace-walking helpers for the `_test:*` private NX subtargets.
 *
 * Each `_test:*` subtarget under `tests:test`'s barrel has a focused script in this
 * directory that enforces one structural workspace invariant. They share the helpers
 * defined here — `findCsproj`, `findCs`, `findJson`, path resolution — so the cost of
 * adding a new invariant is one new script, not one new toolchain.
 *
 * See [docs/scratch/data-extension-contract.md §6 "Workspace Target Architecture"](../../docs/scratch/data-extension-contract.md#6-workspace-target-architecture)
 * for the full pattern; see [tests/README.md](../../tests/README.md) for kit infrastructure.
 */

import { readdirSync, existsSync } from 'node:fs';
import { join, sep } from 'node:path';
import { fileURLToPath } from 'node:url';
import { resolve, dirname } from 'node:path';

const __dirname = dirname(fileURLToPath(import.meta.url));

/** Workspace root (two levels up from this file: scripts/_test/_lib.mjs → repo root). */
export const ROOT = resolve(__dirname, '..', '..');

/** Conventional source / tests / kit directories under ROOT. */
export const SRC_DIR = join(ROOT, 'src');
export const TESTS_DIR = join(ROOT, 'tests');
export const KITS_DIR = join(TESTS_DIR, 'helpers', 'Flowthru.Tests.Kits');

/** Recursively collect all .csproj paths under `dir`. */
export function findCsproj(dir) {
  const results = [];
  if (!existsSync(dir)) return results;
  for (const entry of readdirSync(dir, { withFileTypes: true })) {
    if (entry.name.startsWith('.')) continue;
    const fullPath = join(dir, entry.name);
    if (entry.isDirectory()) {
      results.push(...findCsproj(fullPath));
    } else if (entry.name.endsWith('.csproj')) {
      results.push(fullPath);
    }
  }
  return results;
}

/**
 * Recursively collect all .cs files under `dir`, skipping `obj/`, `bin/`, and
 * `TestResults/` directories.
 */
export function findCs(dir) {
  const results = [];
  if (!existsSync(dir)) return results;
  for (const entry of readdirSync(dir, { withFileTypes: true })) {
    if (entry.name.startsWith('.')) continue;
    if (entry.name === 'obj' || entry.name === 'bin' || entry.name === 'TestResults') {
      continue;
    }
    const fullPath = join(dir, entry.name);
    if (entry.isDirectory()) {
      results.push(...findCs(fullPath));
    } else if (entry.name.endsWith('.cs')) {
      results.push(fullPath);
    }
  }
  return results;
}

/** Recursively collect all .json files under `dir`, skipping standard build dirs. */
export function findJson(dir) {
  const results = [];
  if (!existsSync(dir)) return results;
  for (const entry of readdirSync(dir, { withFileTypes: true })) {
    if (entry.name.startsWith('.')) continue;
    if (entry.name === 'obj' || entry.name === 'bin' || entry.name === 'TestResults') {
      continue;
    }
    const fullPath = join(dir, entry.name);
    if (entry.isDirectory()) {
      results.push(...findJson(fullPath));
    } else if (entry.name.endsWith('.json')) {
      results.push(fullPath);
    }
  }
  return results;
}

/** Recursively collect all .md files under `dir`, skipping standard build dirs. */
export function findMd(dir) {
  const results = [];
  if (!existsSync(dir)) return results;
  for (const entry of readdirSync(dir, { withFileTypes: true })) {
    if (entry.name.startsWith('.') && entry.name !== '.claude') continue;
    if (entry.name === 'obj' || entry.name === 'bin' || entry.name === 'node_modules') continue;
    const fullPath = join(dir, entry.name);
    if (entry.isDirectory()) {
      results.push(...findMd(fullPath));
    } else if (entry.name.endsWith('.md')) {
      results.push(fullPath);
    }
  }
  return results;
}

/** Relative path from ROOT, using forward slashes for legible output. */
export function rel(absPath) {
  return absPath.slice(ROOT.length + 1).replaceAll(sep, '/');
}
