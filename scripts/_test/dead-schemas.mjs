#!/usr/bin/env node
/**
 * `_test:dead-schemas` — every schema type declared under
 * `tests/helpers/Flowthru.Tests.Kits/Schemas/` must be referenced by at least one
 * conformance subclass (i.e., be the type argument to a `*Conformance<TRow>` base).
 *
 * A "dead schema" is one that exists in the kit catalog but isn't exercised by any
 * conformance suite. Adding it provided no test coverage — and over time, dead schemas
 * provide false confidence that "we test that scenario."
 *
 * Detection strategy:
 *   1. Enumerate type declarations (`record`, `record struct`, `class`, `struct`) in
 *      `Flowthru.Tests.Kits/Schemas/**\/*.cs`. Public, top-level types only.
 *   2. Enumerate `*Conformance<...>` base-class type-argument references across
 *      `tests/extensions/**\/Conformance/*.cs` and `tests/core/**\/Conformance/*.cs`.
 *   3. Schema is alive iff its name appears as a type argument to any conformance base.
 *
 * Exits with non-zero on any orphaned schema. Caveats:
 *   - Nested types (helper records inside a parent class) are intentionally skipped;
 *     conformance subclasses should reference top-level types only.
 *   - Schemas referenced indirectly (e.g., as a sub-schema of a wrapper schema that's
 *     itself referenced) are NOT considered alive — every kit-catalog schema is expected
 *     to be a direct conformance target. If that becomes too strict, narrow the rule.
 *
 * Usage:
 *   node scripts/_test/dead-schemas.mjs
 */

import { readFileSync } from 'node:fs';
import { join } from 'node:path';
import { findCs, rel, KITS_DIR, TESTS_DIR } from './_lib.mjs';

const SCHEMAS_DIR = join(KITS_DIR, 'Schemas');
const CONFORMANCE_SEARCH_DIRS = [
  join(TESTS_DIR, 'extensions'),
  join(TESTS_DIR, 'core'),
];

/**
 * Extract top-level public *schema* type names declared in the file. A schema is a
 * record, record-struct, class, or struct that either:
 *   - carries a `[FlowthruSchema]` attribute on its declaration (the source-generated
 *     path), OR
 *   - explicitly implements one of the schema marker interfaces (`IFlatSchema`,
 *     `INestedSchema`, `IStructuredSerializable`, `ITextSerializable`,
 *     `IBinarySerializable`) in its base list (the manual path).
 *
 * Enums, interfaces, and supporting types (e.g., enums used as property types within
 * a schema) are intentionally excluded — they are infrastructure for schemas, not
 * direct conformance targets. The kit's own `CheckStatus` / `Rarity` enums are the
 * canonical example of "supporting type that isn't itself a schema."
 */
function extractTopLevelSchemas(filePath) {
  const text = readFileSync(filePath, 'utf8');
  const declared = new Set();

  // Strip line comments and block comments to avoid matching commented-out declarations.
  const stripped = text
    .replace(/\/\/.*$/gm, '')
    .replace(/\/\*[\s\S]*?\*\//g, '');

  const SCHEMA_MARKERS = new Set([
    'IFlatSchema',
    'INestedSchema',
    'IStructuredSerializable',
    'ITextSerializable',
    'IBinarySerializable',
  ]);

  // Walk every public record/class/struct declaration. For each, determine whether the
  // surrounding context marks it as a schema:
  //   - look back up to ~5 lines for a `[FlowthruSchema]` attribute, OR
  //   - inspect the base list (after the type name) for any schema marker interface.
  const declRe = /^([ \t]*)public\s+(?:partial\s+|sealed\s+|abstract\s+|static\s+|readonly\s+)*(?:record\s+struct|record|class|struct)\s+([A-Z][A-Za-z0-9_]*)([^{;]*)/gm;
  let m;
  while ((m = declRe.exec(stripped)) !== null) {
    const typeName = m[2];
    const tail = m[3] ?? ''; // everything after the type name on the declaration line(s)

    // Look at preceding ~10 lines for a [FlowthruSchema] attribute.
    const declStart = m.index;
    const lookbackStart = Math.max(0, declStart - 600);
    const preceding = stripped.slice(lookbackStart, declStart);
    const hasFlowthruSchemaAttr = /\[\s*FlowthruSchema\s*(?:\(|\])/.test(preceding.split('\n').slice(-12).join('\n'));

    // Inspect base list for schema marker interfaces. The tail captures everything until
    // the opening brace or terminator, which is where the base list lives if present.
    let implementsMarker = false;
    if (tail.includes(':')) {
      for (const marker of SCHEMA_MARKERS) {
        const re = new RegExp(`[:,]\\s*${marker}(?:\\b|<)`);
        if (re.test(tail)) {
          implementsMarker = true;
          break;
        }
      }
    }

    if (hasFlowthruSchemaAttr || implementsMarker) {
      declared.add(typeName);
    }
  }
  return declared;
}

/**
 * Extract type names appearing as type arguments to `*Conformance<...>` base-class
 * references. Matches `: FormatSerializerConformance<TraditionalSchema>` and similar.
 */
function extractConformanceTypeArgs(filePath) {
  const text = readFileSync(filePath, 'utf8');
  const collapsed = text.replace(/\s+/g, ' ');
  const referenced = new Set();

  // Match `: SomethingConformance<TArg>` or `: SomethingConformance<IEnumerable<TArg>>`
  // — capture TArg (the innermost type name in the generic arg). We treat the conformance
  // base as anything ending in "Conformance".
  const re = /[:,]\s*[A-Z][A-Za-z0-9_]*Conformance\s*<\s*(?:[A-Z][A-Za-z0-9_]*\s*<\s*)?([A-Z][A-Za-z0-9_]*)/g;
  let m;
  while ((m = re.exec(collapsed)) !== null) {
    referenced.add(m[1]);
  }
  return referenced;
}

/**
 * For each declared schema, extract the set of *other declared-schema names* it
 * references via property types. Used to compute transitive aliveness: a sub-schema
 * referenced by a directly-tested parent counts as alive.
 */
function extractSchemaPropertyTypeNames(filePath, declaredNames) {
  const text = readFileSync(filePath, 'utf8');
  const stripped = text
    .replace(/\/\/.*$/gm, '')
    .replace(/\/\*[\s\S]*?\*\//g, '');

  const referenced = new Set();
  // Match `public[ required] [Type] PropName { get; init; }` patterns. The type token
  // is whatever sits after the modifiers; we look for any of the declared schema names
  // (with optional `?` for nullable) appearing as a token before a property identifier.
  for (const candidate of declaredNames) {
    const re = new RegExp(
      `public\\s+(?:required\\s+)?${candidate}\\??\\s+[A-Z][A-Za-z0-9_]*\\s*\\{`,
      'g'
    );
    if (re.test(stripped)) {
      referenced.add(candidate);
    }
  }
  return referenced;
}

// ── Walk schemas ─────────────────────────────────────────────────────────────

const declaredSchemas = new Map(); // name → file path
for (const csFile of findCs(SCHEMAS_DIR)) {
  const types = extractTopLevelSchemas(csFile);
  for (const t of types) {
    if (!declaredSchemas.has(t)) {
      declaredSchemas.set(t, csFile);
    }
  }
}

// ── Walk conformance subclasses ──────────────────────────────────────────────

const directlyReferenced = new Set();
for (const dir of CONFORMANCE_SEARCH_DIRS) {
  for (const csFile of findCs(dir)) {
    if (!csFile.includes(`${'Conformance'}`) && !readFileSync(csFile, 'utf8').includes('Conformance')) continue;
    const args = extractConformanceTypeArgs(csFile);
    for (const a of args) directlyReferenced.add(a);
  }
}

// ── Compute transitive aliveness ─────────────────────────────────────────────
//
// A schema is alive iff it's directly referenced by a conformance subclass OR it's
// used as a property type within another schema that is alive. This handles
// sub-schemas (e.g., AddressSchema referenced via NestedSimpleSchema.Address) —
// adding a conformance subclass for AddressSchema directly would be redundant when
// the parent NestedSimpleSchema's conformance already exercises round-trip through
// the sub-schema.
const declaredNames = [...declaredSchemas.keys()];
const propertyReferences = new Map(); // schema name → set of declared-schema names it references
for (const [name, filePath] of declaredSchemas) {
  propertyReferences.set(
    name,
    extractSchemaPropertyTypeNames(filePath, declaredNames.filter((n) => n !== name))
  );
}

const alive = new Set(directlyReferenced);
let added = true;
while (added) {
  added = false;
  for (const [name, refs] of propertyReferences) {
    if (alive.has(name)) {
      for (const ref of refs) {
        if (!alive.has(ref) && declaredSchemas.has(ref)) {
          alive.add(ref);
          added = true;
        }
      }
    }
  }
}

// ── Report orphans ───────────────────────────────────────────────────────────

const orphans = [];
for (const [name, filePath] of declaredSchemas) {
  if (!alive.has(name)) {
    orphans.push({ name, file: rel(filePath) });
  }
}

let exitCode = 0;
if (orphans.length > 0) {
  exitCode = 1;
  console.error(
    `\n${orphans.length} kit schema(s) declared but not referenced by any conformance subclass:\n`
  );
  for (const { name, file } of orphans) {
    console.error(`  ${name}  (${file})`);
  }
  console.error(
    '\nA schema is "alive" iff a *Conformance<...> subclass uses it as the type argument.'
  );
  console.error(
    'Either add a conformance subclass that exercises this schema, or remove the schema.\n'
  );
}

if (exitCode === 0) {
  console.log(
    `_test:dead-schemas — all ${declaredSchemas.size} kit schemas are referenced by at least one conformance subclass.`
  );
}

process.exit(exitCode);
