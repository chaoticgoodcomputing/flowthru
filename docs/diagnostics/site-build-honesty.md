# site:build honesty verification

Verification record for whether the docs build fails on the design-time /
pre-flight documentation errors the honesty model requires it to catch.
`docs/diagnostics/` is repo-internal — not ingested or served.

## Audit: no masking flags

`.github/workflows/website-deploy.yml` runs `pnpm nx run site:build` with no
`continue-on-error`, no `--ignore-errors`, no `--no-fail-on-error`.
`src/website/project.json`'s `build` target runs `astro build` plainly.
There is nothing suppressing a build failure.

## Broken in-source snippet reference → GATED ✓

A `<!-- flowthru:snippet docs:<label> -->` sentinel referencing a label with no
extracted snippet causes `docs:sync-snippets` to exit non-zero (verified:
"references with no snippet in dist … Nothing written"). `docs-checks.yml` runs
`docs:sync-snippets`, so this fails the PR check. The reverse (an orphan
`#region docs:` nothing references) is gated the same way.

## Broken internal markdown link → NOW GATED ✓ (was a real gap)

The ticket assumed "Starlight errors on broken internal links by default." It
did NOT — an early smoke (`[x](./does-not-exist.md)`) built clean, exit 0,
silently. Closed by adding the `starlight-links-validator` plugin to
`src/website/astro.config.mjs` (`errorOnRelativeLinks: false`, so relative
links — the repo convention — are allowed but still resolved).

Proven the hard way: on its first run the validator failed the build with
**5 real pre-existing broken links** in `index.mdx` (root-absolute hrefs that
double-encoded the `/flowthru` base — never shipped only because `index.mdx`
was untracked). Fixing them to relative links returned the build to green:

```
pnpm nx run site:build --skip-nx-cache
# with broken links:  "✗ Found 5 invalid links", exit 1
# after fixing:       "✓ All internal links are valid", "[build] Complete!", exit 0
```

PR-time gating: `pr-tests.yml` runs `nx affected -t build`, which includes
`site:build` whenever docs change (site `implicitDependencies` docs), so a
broken internal link now fails the PR check — not just the release deploy.

External link rot remains out-of-band (scheduled `docs-external-links.yml`),
per the honesty model.
