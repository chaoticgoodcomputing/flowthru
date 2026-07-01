#!/usr/bin/env node
/**
 * Fail-fast guard: refuse to release a version that already exists on nuget.org.
 *
 * Why this exists: NuGet package versions are immutable — once X.Y.Z is pushed
 * it can never be replaced, only unlisted. The release pipeline derives the next
 * version from the latest git tag (see scripts/release.mjs + the "Determine last
 * published release tag" step). If that tag is ever deleted *after* a successful
 * publish, the tag-derived baseline falls behind NuGet reality and the pipeline
 * recomputes an already-published version. Re-releasing it collides at the
 * "Create release tag" step (HTTP 422 "Reference already exists") and can never
 * succeed — but only after a doomed bump commit has already landed on main.
 *
 * This guard runs at pre-flight, before the bump commit is created, and stops
 * the run with an actionable message. It aligns with Flowthru's fail-fast
 * philosophy: surface the contract violation as early as possible.
 *
 * Input (env):
 *   NEW_VERSION  required — the version the pipeline intends to publish.
 *
 * Exit 0 if the version is free on every checked package ID; exit 1 otherwise.
 */

const version = process.env.NEW_VERSION;
if (!version) {
  console.error('NEW_VERSION is required.');
  process.exit(1);
}

// Canary package IDs. Every Flowthru package shares a single <Version> from
// Directory.Build.props, so a collision on any one means the whole set collides.
const PACKAGES = ['flowthru', 'flowthru.core'];

async function publishedVersions(pkg) {
  const url = `https://api.nuget.org/v3-flatcontainer/${pkg}/index.json`;
  const res = await fetch(url);
  if (res.status === 404) return []; // package ID not yet registered on nuget.org
  if (!res.ok) {
    throw new Error(`NuGet version query for ${pkg} failed: HTTP ${res.status}`);
  }
  const body = await res.json();
  return Array.isArray(body.versions) ? body.versions : [];
}

let collided = false;
for (const pkg of PACKAGES) {
  const versions = await publishedVersions(pkg);
  if (versions.includes(version)) {
    collided = true;
    console.error(`✗ ${pkg} ${version} is already published to NuGet.`);
  } else {
    console.log(`✓ ${pkg} ${version} is free on NuGet.`);
  }
}

if (collided) {
  const msg =
    `Version ${version} is already published to NuGet and cannot be ` +
    `republished (NuGet versions are immutable). This almost always means the ` +
    `v${version} tag was deleted after a successful publish, leaving the ` +
    `tag-derived release baseline behind NuGet. Restore the v${version} tag so ` +
    `the pipeline bumps past it, or bump to the next version, then re-run.`;
  console.error(`::error title=Version already published to NuGet::${msg}`);
  process.exit(1);
}

console.log(`\nVersion ${version} is free on NuGet — proceeding with release.`);
