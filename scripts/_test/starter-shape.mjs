#!/usr/bin/env node
/**
 * `_test:starter-shape` — every `examples/starter/<X>/<X>.csproj` that ships as a
 * `dotnet new` template (has a `.template.config/template.json`) follows the
 * dual-mode pattern so it builds correctly BOTH in-repo and as an instantiated
 * template against the published NuGet feed.
 *
 * The pattern, in three parts:
 *
 *   1. Runtime Flowthru.* references are `<PackageReference Include="..." />`
 *      (no Version attribute). In-repo, FlowthruInRepoSwap.props rewrites these
 *      to ProjectReferences; out-of-repo, NuGet resolves them from the feed,
 *      using FlowthruVersion entries the per-template Directory.Packages.props
 *      ships. Direct `<ProjectReference Include="$(RepoRoot)src/..." />` for
 *      runtime Flowthru.* references is forbidden — the path won't resolve
 *      once the template lands in a user directory.
 *
 *   2. NO explicit analyzer `<ProjectReference OutputItemType="Analyzer">` to
 *      in-repo source-generator csprojs. In-repo, the FlowthruInRepoSwap.props
 *      Import chain pulls in `Flowthru.Core/build/Flowthru.Core.targets` (and
 *      conditionally `Flowthru.FUnit.targets` / `Flowthru.Extensions.Python.targets`
 *      based on which Flowthru.* packages this csproj references), and each of
 *      those per-package targets files owns the analyzer ProjectReference. Out
 *      of repo, NuGet auto-imports the same targets files from `build/` and
 *      auto-registers DLLs from `analyzers/dotnet/cs/`. Either way, declaring
 *      the analyzer reference directly on the starter is redundant noise.
 *
 *   3. The csproj ends with the FlowthruInRepoSwap.props Import (conditional
 *      on $(RepoRoot) and Exists()), which performs the in-repo
 *      PackageReference → ProjectReference rewrite.
 *
 * Why this matters: a starter that compiles in-repo but breaks the moment a
 * user runs `dotnet new <ShortName>` is a silent regression — the in-repo
 * build always works because $(RepoRoot) is defined and ProjectReferences
 * resolve. This script catches drift at csproj-edit time, before it ships in
 * a release.
 *
 * Exits with non-zero on any violation. Each violation includes a fix hint.
 *
 * Usage:
 *   node scripts/_test/starter-shape.mjs
 */

import { existsSync, readFileSync } from 'node:fs';
import { join, basename, dirname } from 'node:path';
import { rel, ROOT } from './_lib.mjs';

const STARTER_ROOT = join(ROOT, 'examples', 'starter');

if (!existsSync(STARTER_ROOT)) {
  console.error(`\nStarter directory not found: ${STARTER_ROOT}\n`);
  process.exit(1);
}

// Discover starters: any direct child of examples/starter/ with a
// .template.config/template.json is a `dotnet new`-installable template.
import { readdirSync } from 'node:fs';
const starters = readdirSync(STARTER_ROOT, { withFileTypes: true })
  .filter((e) => e.isDirectory())
  .map((e) => join(STARTER_ROOT, e.name))
  .filter((d) => existsSync(join(d, '.template.config', 'template.json')));

const violations = [];

for (const starterDir of starters) {
  const name = basename(starterDir);
  const csprojPath = join(starterDir, `${name}.csproj`);
  const packagesPropsPath = join(starterDir, 'Directory.Packages.props');

  if (!existsSync(csprojPath)) {
    violations.push({
      starter: name,
      issue: `Expected ${rel(csprojPath)} not found`,
      fix: 'A starter directory shipping a .template.config must contain a matching <Name>.csproj',
    });
    continue;
  }

  const csproj = readFileSync(csprojPath, 'utf8');

  // Rule 1: no direct ProjectReferences to in-repo Flowthru runtime csprojs.
  // Analyzer ProjectReferences are allowed but must be in a conditional ItemGroup
  // (checked separately below). We detect runtime PRs by looking for
  // ProjectReference entries that do NOT carry OutputItemType="Analyzer".
  const runtimeProjectRefRegex = /<ProjectReference\s+Include="\$\(RepoRoot\)src\/[^"]+Flowthru\.[^"]*\.csproj"(?![^/]*OutputItemType="Analyzer")[^/]*\/>/g;
  const runtimeRefs = csproj.match(runtimeProjectRefRegex) || [];
  if (runtimeRefs.length > 0) {
    violations.push({
      starter: name,
      issue: `Found ${runtimeRefs.length} direct <ProjectReference> to in-repo Flowthru runtime csproj(s)`,
      fix: `Convert each to <PackageReference Include="Flowthru.X" />; ` +
        `the FlowthruInRepoSwap.props Import resolves them to ProjectReferences in-repo.`,
      detail: runtimeRefs.map((r) => r.replace(/\s+/g, ' ').trim()).join('\n      '),
    });
  }

  // Rule 2: no explicit analyzer ProjectReferences. The
  // FlowthruInRepoSwap.props chain wires them automatically based on which
  // Flowthru.* packages this csproj references, so declaring them here is
  // redundant — and worse, can leave a starter half-wired if someone later
  // adds a new Flowthru.* PackageReference and forgets the matching
  // analyzer PR. Trust the chain.
  const analyzerRefRegex = /<ProjectReference[^>]*OutputItemType="Analyzer"[^/]*\/>/g;
  const analyzerRefs = csproj.match(analyzerRefRegex) || [];
  if (analyzerRefs.length > 0) {
    violations.push({
      starter: name,
      issue: `Found ${analyzerRefs.length} explicit analyzer <ProjectReference> entries`,
      fix: `Delete the analyzer <ItemGroup>. FlowthruInRepoSwap.props imports ` +
        `Flowthru.Core.targets unconditionally (covers SchemaInterfaceGenerator + ` +
        `FlowBuilderGenerator + StepMetadataGenerator), and conditionally imports ` +
        `Flowthru.FUnit.targets / Flowthru.Extensions.Python.targets when those ` +
        `packages are referenced. Out-of-repo, the same targets files are auto-imported ` +
        `from each package's build/ folder.`,
      detail: analyzerRefs.map((r) => r.replace(/\s+/g, ' ').trim()).join('\n      '),
    });
  }

  // Rule 3: csproj imports FlowthruInRepoSwap.props.
  const hasSwapImport =
    /<Import\s+Project="\$\(RepoRoot\)build\/FlowthruInRepoSwap\.props"/.test(csproj);
  if (!hasSwapImport) {
    violations.push({
      starter: name,
      issue: 'Missing <Import> of $(RepoRoot)build/FlowthruInRepoSwap.props',
      fix: 'Add at end of csproj:\n      <Import Project="$(RepoRoot)build/FlowthruInRepoSwap.props"\n              Condition="\'$(RepoRoot)\' != \'\' AND Exists(\'$(RepoRoot)build/FlowthruInRepoSwap.props\')" />',
    });
  }

  // Rule 4: every Flowthru.* PackageReference in the csproj has a matching
  // PackageVersion entry (under Condition="'$(RepoRoot)' == ''") in the
  // per-template Directory.Packages.props. Missing entries break the
  // out-of-repo restore.
  if (existsSync(packagesPropsPath)) {
    const props = readFileSync(packagesPropsPath, 'utf8');
    const flowthruPkgRefs = [
      ...csproj.matchAll(/<PackageReference\s+Include="(Flowthru[^"]*)"/g),
    ].map((m) => m[1]);

    const declaredVersions = new Set(
      [...props.matchAll(/<PackageVersion\s+Include="(Flowthru[^"]*)"/g)].map((m) => m[1])
    );

    const missing = flowthruPkgRefs.filter((p) => !declaredVersions.has(p));
    if (missing.length > 0) {
      violations.push({
        starter: name,
        issue: `${missing.length} Flowthru.* PackageReference(s) lack matching PackageVersion in Directory.Packages.props`,
        fix: `Add under the existing <ItemGroup Condition="'$(RepoRoot)' == ''">:\n` +
          missing.map((p) => `      <PackageVersion Include="${p}" Version="FlowthruVersion" />`).join('\n'),
      });
    }
  } else {
    violations.push({
      starter: name,
      issue: `Missing per-template Directory.Packages.props at ${rel(packagesPropsPath)}`,
      fix: 'Ship per-template CPM file declaring Flowthru.* PackageVersion entries with Version="FlowthruVersion".',
    });
  }
}

if (violations.length === 0) {
  console.log(
    `_test:starter-shape — all ${starters.length} starter(s) follow the dual-mode pattern.`
  );
  process.exit(0);
}

console.error(`\n${violations.length} starter shape violation(s):\n`);
for (const { starter, issue, fix, detail } of violations) {
  console.error(`  starter: ${starter}`);
  console.error(`  issue:   ${issue}`);
  if (detail) console.error(`  detail:  ${detail}`);
  console.error(`  fix:     ${fix}`);
  console.error('');
}
process.exit(1);
