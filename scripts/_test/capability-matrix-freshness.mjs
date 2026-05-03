#!/usr/bin/env node
/**
 * `_test:capability-matrix-freshness` — regenerates `docs/reference/extensions/capability-matrix.md`
 * via the file-based C# program at `scripts/_test/capability-matrix.cs`, then asserts
 * the regenerated file matches the committed copy. Drift means a format's declared
 * `RowFeatures`, `[OptOutOfPropertyPlanner]` reason, or implemented capability segment
 * changed without a corresponding doc update.
 *
 * Failures here are most often a sign that a format extension was edited but the
 * matrix wasn't regenerated. Resolution: run the generator locally
 * (`dotnet run scripts/_test/capability-matrix.cs` or `nx run docs:build`), commit
 * the updated matrix file, and re-run the meta-test.
 *
 * Usage:
 *   node scripts/_test/capability-matrix-freshness.mjs
 */

import { execFileSync } from 'node:child_process';
import { existsSync } from 'node:fs';
import { join } from 'node:path';
import { ROOT } from './_lib.mjs';

const MATRIX_PATH = join(ROOT, 'docs', 'reference', 'extensions', 'capability-matrix.md');
const GENERATOR_SCRIPT = join(ROOT, 'scripts', '_test', 'capability-matrix.cs');

if (!existsSync(MATRIX_PATH)) {
  console.error(
    `\nMatrix file not found at ${MATRIX_PATH}.\n`
      + 'Run `dotnet run scripts/_test/capability-matrix.cs` to generate it.\n'
  );
  process.exit(1);
}

if (!existsSync(GENERATOR_SCRIPT)) {
  console.error(
    `\nMatrix generator script not found at ${GENERATOR_SCRIPT}.\n`
      + 'The capability matrix freshness check cannot run without the generator script.\n'
  );
  process.exit(1);
}

// Regenerate via the file-based C# program (.NET 10 `dotnet run path/to/script.cs`).
// The generator writes to its default location (the committed file at MATRIX_PATH)
// when invoked with no arguments.
try {
  execFileSync('dotnet', ['run', GENERATOR_SCRIPT, '--verbosity', 'quiet'], {
    stdio: ['ignore', 'inherit', 'inherit'],
    cwd: ROOT,
  });
} catch (err) {
  console.error(
    '\nCapability matrix generator failed to run. See output above for details.\n'
  );
  process.exit(1);
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
