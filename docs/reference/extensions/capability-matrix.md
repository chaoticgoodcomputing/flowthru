# Flowthru Format Extension Capability Matrix

This document is **auto-generated** from each format extension's marker-interface declarations (`ISupportsIScalar`, `ISupportsNested`) and which capability segments it implements (`IFormatRowReader<TRow>`, `IFormatRowWriter<TRow>`, `IFormatStreamReader<TRow>`). Do not edit by hand — the `_test:capability-matrix-freshness` meta-test fails on drift.

Regenerate locally via `nx run tests:_test:capability-matrix-freshness`, `nx run docs:build`, or directly via `dotnet run scripts/_test/capability-matrix.cs`.

## Universal baseline

All four formats round-trip the universal row-shape baseline:

- CLR primitives (`int`, `string`, `bool`, `double`, `decimal`, …)
- BCL scalar structs (`Guid`, `DateTime`, `TimeSpan`, `DateTimeOffset`, …)
- `Nullable<T>` value types and nullable reference types
- `[SerializedLabel("…")]` field-name mapping
- `[SerializedEnum("…")]` enum value mapping
- `required` members and positional-record activation

These features are intrinsic to the planner's classification cascade and don't vary across formats. The matrix below tracks capabilities **on top of** that baseline — features where format-by-format support genuinely differs.

## Row-shape Features

Each format declares which row-shape features it round-trips on top of the universal baseline. Cell semantics:

- **`✓`** — format implements the marker interface; the matching kit conformance fixture round-trips successfully.
- **`✗`** — format does not implement the marker; could be implemented but isn't. Tracked as a follow-up; kit fixtures requiring the feature skip vacuously.
- **`—`** — structurally not applicable; the format's generic constraint (`where TRow : IFlatSchema`) prevents the schema shape from compiling. The matching fixture cannot be wired against this format.

| Format | Schema shape | IScalar wrappers | Nested rows |
|---|---|:---:|:---:|
| **CSV** (Flowthru.Extensions.Csv) | Flat-only | ✓ | — |
| **Excel** (Flowthru.Extensions.Excel) | Flat-only | ✓ | — |
| **Parquet** (Flowthru.Extensions.Parquet) | Flat-only | ✗ | — |
| **JSON** (Flowthru.Core (built-in)) | Flat or nested | ✓ | ✓ |

Primitive-level format mechanics (`byte[]` blobs handled as base64/binary, timezone semantics on `DateTimeOffset`, etc.) are intrinsic to each format's underlying serialization library and aren't tracked here.

## Property Mapping

Format extensions are expected to consume Core's `PropertyMappingPlanner` for per-property classification (see `docs/scratch/data-extension-contract.md` Phase B). Formats with a structural reason can opt out via `[OptOutOfPropertyPlanner(...)]` — those formats handle row-shape classification on their own and may diverge from the planner-driven baseline.

| Format | Planner consumption | Opt-out reason |
|---|---|---|
| **CSV** | ✓ consumes planner | — |
| **Excel** | ✓ consumes planner | — |
| **Parquet** | ✓ consumes planner | — |
| **JSON** | ✓ consumes planner | — |

## Storage Traits

Medium-level capabilities of each format. See `Flowthru.Data.Storage.StorageTraits` for the full surface.

**Read / Write / Stream columns** carry two signals. Phase D (capability-segmented interfaces) split the format surface into `IFormatRowReader<TRow>`, `IFormatRowWriter<TRow>`, and `IFormatStreamReader<TRow>` (a sub-interface of the row reader, marking bounded-memory decoding). A format that does not implement a segment is *structurally* incapable of that operation — the absence is enforced by the type system, not a runtime trait flag. A format that implements the segment but reports `Traits.CanWrite = false` (etc.) is *runtime*-disabled.

- **`✓`** — segment implemented and runtime trait permits.
- **`—`** — segment not implemented (structural / compile-time signal). Calling code paths against the missing segment fail at compile time.
- **`✗`** — segment implemented but runtime trait reports unavailable (e.g., medium pointed at a read-only file system).

| Format | Read | Write | Stream | Append | Transactional |
|---|:---:|:---:|:---:|:---:|:---:|
| **CSV** | ✓ | ✓ | ✓ | ✗ | ✗ |
| **Excel** | ✓ | — | — | ✗ | ✗ |
| **Parquet** | ✓ | ✓ | ✓ | ✗ | ✗ |
| **JSON** | ✓ | ✓ | — | ✗ | ✗ |

