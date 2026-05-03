#!/usr/bin/env node
/**
 * `_test:diagnostic-id-registration` — every `FT###` and `FU###` diagnostic ID emitted
 * from `src/**\/*.cs` (via `Diagnostic.Create(...)` or `new DiagnosticDescriptor(...)`)
 * has a matching entry in some `AnalyzerReleases.{Un,}shipped.md` file under `src/`.
 *
 * Backstops Roslyn's release-tracking analyzer for IDs whose source files aren't in the
 * analyzer's compilation scope. The release-tracking analyzer catches missing
 * registration when both the diagnostic's defining file and the releases file are in the
 * same Roslyn analyzer project; this meta-test catches the workspace-level invariant
 * that the IDs and the release files agree, regardless of project boundaries.
 *
 * Detection strategy:
 *   1. Walk every `.cs` file under `src/`. Extract `FT###` and `FU###` IDs from string
 *      literals. We're conservative — we match the digits-only pattern in literal strings
 *      so that incidental occurrences in comments/identifiers don't false-match.
 *   2. Walk every `AnalyzerReleases.{Un,}shipped.md` file under `src/`. Extract IDs from
 *      Markdown table rows.
 *   3. Source IDs not in any release file fail.
 *
 * Caveats:
 *   - This is a textual scan, not a Roslyn-level inspection. An ID assigned to a `const
 *     string` and referenced indirectly will still be picked up because the literal
 *     appears in source. False positives are possible if a string literal coincidentally
 *     matches `FT###` outside a diagnostic context (rare; surface that case as needed).
 *   - The check is union-membership: an ID in *any* `AnalyzerReleases.{Un,}shipped.md`
 *     under `src/` counts as registered. This admits Flowthru's pattern of multiple
 *     analyzer projects (`Flowthru.Core.SourceGenerators`, `Flowthru.FUnit.SourceGenerators`,
 *     `Flowthru.Extensions.Python.SourceGenerators`) each maintaining their own release
 *     file.
 *
 * Usage:
 *   node scripts/_test/diagnostic-id-registration.mjs
 */

import { readFileSync, readdirSync, existsSync, statSync } from 'node:fs';
import { join } from 'node:path';
import { findCs, rel, SRC_DIR } from './_lib.mjs';

// ── Recursively find every AnalyzerReleases.*.md under src/ ──────────────────

function findAnalyzerReleaseFiles(dir) {
  const results = [];
  if (!existsSync(dir)) return results;
  for (const entry of readdirSync(dir, { withFileTypes: true })) {
    if (entry.name.startsWith('.')) continue;
    if (entry.name === 'obj' || entry.name === 'bin') continue;
    const fullPath = join(dir, entry.name);
    if (entry.isDirectory()) {
      results.push(...findAnalyzerReleaseFiles(fullPath));
    } else if (
      entry.name === 'AnalyzerReleases.Unshipped.md'
      || entry.name === 'AnalyzerReleases.Shipped.md'
    ) {
      results.push(fullPath);
    }
  }
  return results;
}

// ── Extract FT### / FU### IDs from a source file's string literals ───────────

function extractDiagnosticIds(filePath) {
  const text = readFileSync(filePath, 'utf8');
  const found = new Map(); // id → first line number it appeared on

  // Regex: capture FT or FU followed by 3+ digits, when wrapped in double-quoted
  // string literals OR when assigned to a string field commonly used for diagnostic IDs.
  // We accept the literal context as a coarse filter; the release-tracking analyzer is
  // the precise source of truth.
  const lines = text.split('\n');
  const re = /"((?:FT|FU)\d{3,})"/g;
  for (let i = 0; i < lines.length; i++) {
    let m;
    re.lastIndex = 0;
    while ((m = re.exec(lines[i])) !== null) {
      if (!found.has(m[1])) {
        found.set(m[1], i + 1);
      }
    }
  }
  return found;
}

// ── Extract IDs registered in an AnalyzerReleases.*.md file ──────────────────

function extractRegisteredIds(filePath) {
  const text = readFileSync(filePath, 'utf8');
  const found = new Set();

  // Markdown table rows for AnalyzerReleases look like:
  //   | FT1001 | Flowthru.Schema | Error | … |
  // Strip leading whitespace and pipe, capture the first cell if it matches FT### or FU###.
  const re = /^\s*\|\s*((?:FT|FU)\d{3,})\b/gm;
  let m;
  while ((m = re.exec(text)) !== null) {
    found.add(m[1]);
  }
  return found;
}

// ── Walk source ──────────────────────────────────────────────────────────────

const sourceIds = new Map(); // id → [{ file, line }, ...]
for (const csFile of findCs(SRC_DIR)) {
  const ids = extractDiagnosticIds(csFile);
  for (const [id, line] of ids) {
    if (!sourceIds.has(id)) sourceIds.set(id, []);
    sourceIds.get(id).push({ file: rel(csFile), line });
  }
}

// ── Walk releases ────────────────────────────────────────────────────────────

const registeredIds = new Set();
const releaseFiles = findAnalyzerReleaseFiles(SRC_DIR);
for (const releaseFile of releaseFiles) {
  const ids = extractRegisteredIds(releaseFile);
  for (const id of ids) registeredIds.add(id);
}

// ── Report unregistered IDs ──────────────────────────────────────────────────

const unregistered = [];
for (const [id, sites] of sourceIds) {
  if (!registeredIds.has(id)) {
    unregistered.push({ id, sites });
  }
}
unregistered.sort((a, b) => a.id.localeCompare(b.id));

let exitCode = 0;
if (unregistered.length > 0) {
  exitCode = 1;
  console.error(
    `\n${unregistered.length} diagnostic ID(s) emitted from src/ but not registered in any AnalyzerReleases.{Un,}shipped.md:\n`
  );
  for (const { id, sites } of unregistered) {
    console.error(`  ${id}`);
    for (const { file, line } of sites) {
      console.error(`    - ${file}:${line}`);
    }
  }
  console.error(
    `\nFound ${releaseFiles.length} release file(s) under src/:`
  );
  for (const f of releaseFiles) {
    console.error(`  ${rel(f)}`);
  }
  console.error(
    `\nAdd each unregistered ID as a row in the Unshipped release file for the project that emits it.\n`
  );
}

if (exitCode === 0) {
  console.log(
    `_test:diagnostic-id-registration — all ${sourceIds.size} diagnostic ID(s) in src/ are registered across ${releaseFiles.length} release file(s).`
  );
}

process.exit(exitCode);
