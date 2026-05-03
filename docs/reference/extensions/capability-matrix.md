# Flowthru Format Extension Capability Matrix

This document is **auto-generated** from each format extension's `IFormatSerializer<TRow>.RowFeatures` declaration. Do not edit by hand — the `_test:capability-matrix-freshness` meta-test fails on drift.

Regenerate locally via `nx run tests:_test:capability-matrix-freshness` or directly via the `Flowthru.Tools.CapabilityMatrix` tool.

## Row-shape Features

Each format declares which row-shape features it round-trips. A `✓` cell means the format claims support and the corresponding kit conformance fixture (under `tests/helpers/Flowthru.Tests.Kits/Fixtures/`) round-trips successfully. A `✗` cell means the format declares the feature unsupported — kit fixtures requiring that feature skip vacuously for this format.

| Format | IScalar wrappers | byte[] columns | Nested rows |
|---|:---:|:---:|:---:|
| **CSV** (Flowthru.Extensions.Csv) | ✓ | ✗ | ✗ |
| **Excel** (Flowthru.Extensions.Excel) | ✓ | ✗ | ✗ |
| **Parquet** (Flowthru.Extensions.Parquet) | ✗ | ✗ | ✗ |
| **JSON** (Flowthru.Core (built-in)) | ✓ | ✓ | ✓ |

## Property Mapping

Format extensions are expected to consume Core's `PropertyMappingPlanner` for per-property classification (see `docs/scratch/data-extension-contract.md` Phase B). Formats with a structural reason can opt out via `[OptOutOfPropertyPlanner(...)]` — those formats handle row-shape classification on their own and may diverge from the planner-driven baseline.

| Format | Planner consumption | Opt-out reason |
|---|---|---|
| **CSV** | ✓ consumes planner | — |
| **Excel** | ✓ consumes planner | — |
| **Parquet** | ✗ manual mapping | Parquet's runtime DTO synthesis via System.Reflection.Emit is structurally different from the reflection walks PropertyMappingPlanner subsumes for CSV/Excel/JSON. Migrating Parquet to consume the planner is a deliberate follow-up effort outside Phase B's scope — it requires reworking how the typed DTO is built per-row from PropertyBinding metadata. The capability matrix surfaces this opt-out under 'manual mapping' so reviewers and end users see the gap explicitly. |
| **JSON** | ✓ consumes planner | — |

## Storage Traits

Medium-level capabilities of each format. See `Flowthru.Core.Data.Capabilities.StorageTraits` for the full surface.

| Format | Read | Write | Stream | Append | Transactional |
|---|:---:|:---:|:---:|:---:|:---:|
| **CSV** | ✓ | ✓ | ✓ | ✗ | ✗ |
| **Excel** | ✓ | ✗ | ✗ | ✗ | ✗ |
| **Parquet** | ✓ | ✓ | ✓ | ✗ | ✗ |
| **JSON** | ✓ | ✓ | ✗ | ✗ | ✗ |

