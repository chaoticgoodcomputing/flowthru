#!/usr/bin/env node
/**
 * Per-package README conformance gate for shippable src/ packages.
 *
 * A "shippable package" is a packable .csproj under src/ — i.e. every .csproj
 * except IsPackable=false projects (source generators, code-fix projects ship
 * *inside* a parent package's analyzers/, never standalone) and *.Tests. Each
 * shippable package must carry a README.md that ships as its NuGet package
 * readme (wired in Directory.Build.props) and conforms to the skeleton:
 *
 *   # <PackageId>                            <- H1 equals the PackageId
 *   <one non-empty lead paragraph>           <- the "what it is"
 *   [![coverage](…?component=<id>)](…)        <- this package's codecov component
 *   ## Mental model                          <- iff under src/extensions/
 *   ## Install                               <- dotnet add package + snippet
 *
 * `## Reference` is intentionally NOT required yet — it lands with the
 * per-package reference landing (ADR-0022's structural tranche). The lint
 * neither requires nor forbids it, so that work isn't blocked.
 *
 * The badge's component id must also exist in codecov.yml's
 * component_management — closing the package <-> component <-> badge loop
 * (the third leg, "every component has a package", is sync-codecov-flags' job).
 *
 * Pure function of the READMEs + the csproj set + codecov.yml; NX caches it
 * honestly. Exits 1 on any violation.
 */

import { readFileSync, existsSync, globSync } from 'node:fs';
import { basename, dirname, resolve } from 'node:path';
import { fileURLToPath } from 'node:url';
import yaml from 'js-yaml';

const ROOT = resolve(dirname(fileURLToPath(import.meta.url)), '..');

// ── Known codecov components (badge cross-check) ──────────────────────────────
const codecov = yaml.load(readFileSync(resolve(ROOT, 'codecov.yml'), 'utf8'));
const knownComponents = new Set(
  (codecov?.component_management?.individual_components ?? []).map((c) => c.component_id),
);

// ── Discover shippable packages ───────────────────────────────────────────────
function isShippable(csprojRel) {
  if (/[/\\](bin|obj)[/\\]/.test(csprojRel)) return false;
  if (/\.Tests\.csproj$/.test(csprojRel)) return false;
  const content = readFileSync(resolve(ROOT, csprojRel), 'utf8');
  if (/<IsPackable>\s*false\s*<\/IsPackable>/i.test(content)) return false;
  return true;
}

function packageId(csprojRel, fallback) {
  const content = readFileSync(resolve(ROOT, csprojRel), 'utf8');
  const m = content.match(/<PackageId>\s*([^<]+?)\s*<\/PackageId>/);
  return m ? m[1].trim() : fallback;
}

const packages = globSync('src/**/*.csproj', { cwd: ROOT })
  .filter(isShippable)
  .map((csprojRel) => {
    const projectDir = csprojRel.replace(/[/\\][^/\\]+\.csproj$/, '');
    const name = basename(projectDir);
    return {
      name,
      dir: projectDir,
      readme: resolve(ROOT, projectDir, 'README.md'),
      pkgId: packageId(csprojRel, name),
      componentId: name.toLowerCase().replace(/\./g, '_'),
      isExtension: projectDir.replace(/\\/g, '/').startsWith('src/extensions/'),
    };
  })
  .sort((a, b) => a.name.localeCompare(b.name));

// ── Check each package against the skeleton ───────────────────────────────────
const violations = [];
const fail = (pkg, msg) => violations.push({ pkg: pkg.name, msg });

for (const pkg of packages) {
  if (!knownComponents.has(pkg.componentId)) {
    fail(pkg, `no codecov component "${pkg.componentId}" in codecov.yml (run sync-codecov-flags)`);
  }

  if (!existsSync(pkg.readme)) {
    fail(pkg, `missing README.md (shippable package — every one needs a skeleton README)`);
    continue;
  }

  const text = readFileSync(pkg.readme, 'utf8');
  const lines = text.split(/\r?\n/);

  // H1 must equal "# <PackageId>"
  const firstContent = lines.find((l) => l.trim().length > 0) ?? '';
  if (firstContent.trim() !== `# ${pkg.pkgId}`) {
    fail(pkg, `H1 must be "# ${pkg.pkgId}" (found "${firstContent.trim() || '<empty>'}")`);
  }

  // Lead paragraph: a non-empty, non-heading, non-badge line before the first "## "
  const h1Idx = lines.findIndex((l) => l.startsWith('# '));
  const firstSection = lines.findIndex((l, i) => i > h1Idx && /^##\s/.test(l));
  const window = lines.slice(h1Idx + 1, firstSection === -1 ? undefined : firstSection);
  const hasLead = window.some(
    (l) => l.trim() && !l.startsWith('#') && !l.trimStart().startsWith('[!['),
  );
  if (!hasLead) fail(pkg, `no lead paragraph between the H1 and the first section`);

  // Coverage badge keyed to this package's component
  if (!text.includes(`graph/badge.svg?component=${pkg.componentId}`)) {
    fail(pkg, `missing coverage badge "graph/badge.svg?component=${pkg.componentId}"`);
  }

  // ## Install
  if (!/^##\s+Install\b/m.test(text)) fail(pkg, `missing "## Install" section`);

  // ## Mental model — required for extensions, forbidden elsewhere
  const hasMentalModel = /^##\s+Mental model\b/m.test(text);
  if (pkg.isExtension && !hasMentalModel) {
    fail(pkg, `extensions require a "## Mental model" section`);
  } else if (!pkg.isExtension && hasMentalModel) {
    fail(pkg, `"## Mental model" is for src/extensions/ only (core/misc READMEs omit it)`);
  }
}

// ── Report ────────────────────────────────────────────────────────────────────
if (violations.length) {
  console.error(`[lint-readmes] ${violations.length} README conformance violation(s):\n`);
  for (const v of violations) console.error(`  ${v.pkg}\n     → ${v.msg}`);
  console.error(
    `\nEvery shippable src/ package needs a README.md matching the skeleton ` +
      `(H1=PackageId, lead paragraph, coverage badge for its codecov component, ` +
      `## Install, and ## Mental model iff under src/extensions/).`,
  );
  process.exit(1);
}

console.log(`[lint-readmes] ${packages.length} shippable package(s) scanned, all READMEs conform. ✓`);
