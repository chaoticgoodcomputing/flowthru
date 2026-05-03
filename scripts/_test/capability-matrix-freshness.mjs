#!/usr/bin/env node
/**
 * `_test:capability-matrix-freshness` — regenerates `docs/reference/extensions/capability-matrix.md`
 * via the `Flowthru.Tools.CapabilityMatrix` tool, then asserts the regenerated file
 * matches the committed copy. Drift means a format's declared `RowFeatures` (or
 * `[OptOutOfPropertyPlanner]` reason) changed without a corresponding doc update.
 *
 * Failures here are most often a sign that a format extension was edited but the
 * matrix wasn't regenerated. Resolution: run the tool locally
 * (`dotnet run --project tools/Flowthru.Tools.CapabilityMatrix`), commit the updated
 * matrix file, and re-run the meta-test.
 *
 * Usage:
 *   node scripts/_test/capability-matrix-freshness.mjs
 */

import { execFileSync } from 'node:child_process';
import { existsSync } from 'node:fs';
import { join } from 'node:path';
import { ROOT } from './_lib.mjs';

const MATRIX_PATH = join(ROOT, 'docs', 'reference', 'extensions', 'capability-matrix.md');
const TOOL_PROJECT = join(
  ROOT,
  'tools',
  'Flowthru.Tools.CapabilityMatrix',
  'Flowthru.Tools.CapabilityMatrix.csproj'
);

if (!existsSync(MATRIX_PATH)) {
  console.error(
    `\nMatrix file not found at ${MATRIX_PATH}.\n`
      + `Run \`dotnet run --project tools/Flowthru.Tools.CapabilityMatrix\` to generate it.\n`
  );
  process.exit(1);
}

if (!existsSync(TOOL_PROJECT)) {
  console.error(
    `\nMatrix generator project not found at ${TOOL_PROJECT}.\n`
      + 'The capability matrix freshness check cannot run without the generator tool.\n'
  );
  process.exit(1);
}

// Regenerate. The tool writes to its default location (the committed file).
try {
  execFileSync(
    'dotnet',
    ['run', '--project', TOOL_PROJECT, '--no-build', '-c', 'Debug', '--verbosity', 'quiet'],
    { stdio: ['ignore', 'inherit', 'inherit'], cwd: ROOT }
  );
} catch (err) {
  // If --no-build fails because the tool hasn't been built, fall back to a build run.
  // This happens on first invocation; later runs hit the no-build fast path.
  try {
    execFileSync(
      'dotnet',
      ['run', '--project', TOOL_PROJECT, '-c', 'Debug', '--verbosity', 'quiet'],
      { stdio: ['ignore', 'inherit', 'inherit'], cwd: ROOT }
    );
  } catch (innerErr) {
    console.error(
      '\nCapability matrix generator failed to run. See output above for details.\n'
    );
    process.exit(1);
  }
}

// Diff against the committed file. `git diff --quiet` exits non-zero if there's a
// difference (including the just-regenerated file vs. its committed state).
try {
  execFileSync('git', ['diff', '--quiet', '--exit-code', '--', MATRIX_PATH], {
    cwd: ROOT,
    stdio: 'pipe',
  });
} catch {
  console.error(
    `\nCapability matrix is stale. The regenerated content differs from the committed file:\n  ${MATRIX_PATH}\n\n`
      + 'Either commit the regenerated file, or revert the source change that produced new output.\n'
  );
  // Show the diff to make the failure actionable.
  try {
    execFileSync('git', ['--no-pager', 'diff', '--', MATRIX_PATH], {
      cwd: ROOT,
      stdio: ['ignore', 'inherit', 'inherit'],
    });
  } catch {
    // ignore — diff output is best-effort
  }
  process.exit(1);
}

console.log(
  '_test:capability-matrix-freshness — capability matrix matches the committed file.'
);
process.exit(0);
