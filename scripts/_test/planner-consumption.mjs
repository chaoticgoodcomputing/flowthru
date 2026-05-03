#!/usr/bin/env node
/**
 * `_test:planner-consumption` — every class implementing `IFormatSerializer<TRow>` under
 * `src/extensions/**\/*.cs` either:
 *   - references `PropertyMappingPlanner` or `PropertyMappingPlan<` in its source, OR
 *   - carries `[OptOutOfPropertyPlanner(...)]` on the implementing class.
 *
 * Backstops the contract that format extensions consume Core's universal property-walk
 * cascade rather than reimplementing it. New format extensions that bypass the planner
 * without an explicit opt-out fail this meta-test, surfacing the discipline violation
 * during normal `nx run-many -t test` runs rather than at PR review.
 *
 * Detection strategy:
 *   1. Walk every `.cs` file under `src/extensions/`. Find class declarations whose base
 *      list includes `IFormatSerializer<...>` (matched textually after whitespace
 *      collapse).
 *   2. For each implementing class file, check whether the file's source contains either:
 *        - a reference to `PropertyMappingPlanner` or `PropertyMappingPlan<`, OR
 *        - an `[OptOutOfPropertyPlanner(` attribute decoration.
 *   3. Fail if any class lacks both signals.
 *
 * Caveats:
 *   - Textual match scope is the file containing the class declaration. If a format
 *     pushes its planner consumption into a separate helper file, this check might emit
 *     a false positive. Closer-fit detection would require Roslyn semantic analysis;
 *     for now, the file-level check matches Flowthru's existing meta-test conventions.
 *   - The check is per-class (not per-implementor-of-IFormatSerializer-instance) — a
 *     single .cs file declaring multiple format serializers passes if any one of the
 *     two signals is present anywhere in the file.
 *
 * Usage:
 *   node scripts/_test/planner-consumption.mjs
 */

import { readFileSync } from 'node:fs';
import { join } from 'node:path';
import { findCs, rel, SRC_DIR } from './_lib.mjs';

const EXTENSIONS_DIR = join(SRC_DIR, 'extensions');

// Find all class declarations implementing IFormatSerializer<...> under src/extensions/.
function findFormatSerializerClasses() {
  const results = [];
  for (const file of findCs(EXTENSIONS_DIR)) {
    const text = readFileSync(file, 'utf8');
    const collapsed = text.replace(/\s+/g, ' ');

    // Match: `class <ClassName> ... : ... IFormatSerializer<...>`
    // The base list can wrap across lines; the collapse normalizes whitespace first.
    const classRe = /\b(class|record)\s+([A-Z][A-Za-z0-9_]*)(?:<[^>]+>)?\s*:[^{]*\bIFormatSerializer\s*</g;
    let m;
    while ((m = classRe.exec(collapsed)) !== null) {
      results.push({ file, className: m[2] });
    }
  }
  return results;
}

const violations = [];
const classes = findFormatSerializerClasses();

for (const { file, className } of classes) {
  const text = readFileSync(file, 'utf8');

  const consumesPlanner =
    text.includes('PropertyMappingPlanner') || text.includes('PropertyMappingPlan<');

  const optsOut = text.includes('[OptOutOfPropertyPlanner(');

  if (!consumesPlanner && !optsOut) {
    violations.push({ file: rel(file), className });
  }
}

let exitCode = 0;
if (violations.length > 0) {
  exitCode = 1;
  console.error(
    `\n${violations.length} IFormatSerializer<TRow> implementation(s) neither consume PropertyMappingPlanner nor declare [OptOutOfPropertyPlanner]:\n`
  );
  for (const { file, className } of violations) {
    console.error(`  ${className}  (${file})`);
  }
  console.error(
    "\nFormat extensions are expected to delegate per-property classification to Core's"
  );
  console.error(
    'PropertyMappingPlanner. Either consume the planner via `PropertyMappingPlanner.Build<TRow>()`'
  );
  console.error(
    'or, if the format has a structural reason to walk properties on its own (Parquet'
  );
  console.error(
    "is the canonical case), apply `[OptOutOfPropertyPlanner(\"<reason>\")]` to the"
  );
  console.error(
    'implementing class. The opt-out reason renders into the capability matrix and is reviewed in PR.\n'
  );
}

if (exitCode === 0) {
  console.log(
    `_test:planner-consumption — all ${classes.length} IFormatSerializer<TRow> implementor(s) consume the planner or declare an opt-out.`
  );
}

process.exit(exitCode);
