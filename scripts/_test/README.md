# `scripts/_test/` — cross-project meta-tests

These node scripts enforce invariants whose verification crosses project
boundaries (filesystem layout, NuGet versioning, the `Flowthru.slnx`
graph) — concerns that don't fit cleanly into a single `dotnet test`
project's reflection-based architecture-test set.

Type-graph invariants that *can* be expressed against a single
assembly's reflection have been migrated to
`tests/core/Flowthru.Core.Architecture.Tests/`. The scripts that
remain here are the ones whose inputs include the filesystem itself
or multiple repos / external indices.

## Active scripts

| Script | Concern | Why it's not a `dotnet test` |
|---|---|---|
| `capability-matrix-freshness.mjs` | The capability matrix doc reflects the current type graph | Reads markdown + `.cs` from disk and re-derives the matrix; an architecture test would still need to shell out to read the doc. |
| `dead-fixtures.mjs` | Every fixture file under `tests/**/_Fixtures/` is referenced by at least one test class | Walks the filesystem; reflection alone can't see fixture-content files. |
| `dead-schemas.mjs` | Every `[FlowthruSchema]` type is referenced by an `IItem<…>` / `IFormatSerializer<…>` somewhere | Cross-project — needs to walk every test/example assembly's source to confirm usage. |
| `diagnostic-id-registration.mjs` | Every FT diagnostic constant is also listed in `AnalyzerReleases.{Shipped,Unshipped}.md` | Compares C# constants against markdown release tracking — straddles two source kinds. |
| `package-versions.mjs` | Central package versions in `Directory.Packages.props` are consistent with consumer csprojs | Reads csproj XML across the repo. |
| `project-mirror.mjs` | The mirror property holds: every csproj has a sibling test/code-fix/source-gen csproj where the layout demands it | Filesystem-shape check across projects. |
| `capability-matrix.cs` | Source for the capability matrix derivation tool | Companion compiled binary, not a meta-test itself. |
| `_lib.mjs` | Shared helpers used by the scripts above | — |

## Migrated to architecture tests

The following scripts were retired in Phase 6 of the FP rewrite — their
invariants are now enforced by `tests/core/Flowthru.Core.Architecture.Tests/`:

- `conformance-presence.mjs` — kit-presence checks fold into the typed
  reflection assertions over `IStorageAdapter` / `IFormatSerializer` /
  `IStorageMedium` / `IMetadataProvider` interfaces.
- `planner-consumption.mjs` — replaced by reflection-based check that
  every `IFormatSerializer<TRow>` implementor either references
  `PropertyMappingPlanner` symbols or carries
  `[OptOutOfPropertyPlanner]`. (To be added when the first non-Core
  format serializer migrates in Phase 8 — the architecture-test
  assembly currently has no extension formats to walk.)
- `row-features-claims.mjs` — capability markers (§2.11) are
  type-level interfaces (`ISupportsIScalar`, `ISupportsNested`); the
  architecture test asserts every format that declares the marker
  has the corresponding implementation, replacing the runtime-bool
  cross-check.
- `row-features-inventory.mjs` — same as above; the marker-interface
  approach removed the runtime-bool surface the script was checking.
