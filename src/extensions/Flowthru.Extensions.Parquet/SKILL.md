---
name: flowthru-parquet
description: Deep skill for the Flowthru Parquet format extension — declaring Parquet-backed Catalog Items in a Flowthru (.NET) pipeline. Use when a project reads or writes .parquet, or when choosing a serialization format for large intermediate/primary data. Pairs with the umbrella `flowthru` skill.
metadata:
  flowthru:
    extension: Flowthru.Extensions.Parquet
    surface: format
    capability: Columnar, compressed on-disk format for large typed row sets — the default for intermediate and primary data.
    register: "— (declare a .Parquet() item)"
---

# flowthru-parquet

Adds the **Parquet format** to the Catalog. This is one axis of a catalog item (format × medium × container — see the `flowthru` umbrella skill's `catalog-developers.md`); it decides how bytes serialize, not where they live.

**Reach for Parquet** for intermediate and primary layers: it's binary, columnar, and compressed, so large typed row sets round-trip far faster and smaller than CSV. Use CSV/Excel only at the raw edge where an external producer dictates the format.

## Use it

Reference the package — there is **no `UseXxx()` call**. Once referenced, `.Parquet()` is available on the item builder:

```bash
dotnet add package Flowthru.Extensions.Parquet
```

<!-- flowthru:snippet:docs:item-parquet:start -->
```csharp
public IItem<IEnumerable<PreprocessedShuttleSchema>> PreprocessedShuttles =>
  CreateItem(() => Item.Of<IEnumerable<PreprocessedShuttleSchema>>("PreprocessedShuttles")
    .Parquet()
    .AtPath($"{_basePath}/_02_Intermediate/Datasets/preprocessed_shuttles.parquet")
    .Build());
```
<!-- flowthru:snippet:docs:item-parquet:end -->

_(real source: [Spaceflights `Catalog.Intermediate.cs`](https://github.com/chaoticgoodcomputing/flowthru/blob/main/examples/starter/Spaceflights/Data/_02_Intermediate/Catalog.Intermediate.cs))_

## Notes

- **Defaults:** Snappy compression, 1,000,000-row groups, dictionary encoding. Tune with `.WithOptions(...)`.
- **Container:** Parquet backs row-sequence items (`IItem<IEnumerable<TSchema>>`); it maps your `[FlowthruSchema]` record to and from the columnar layout — you don't manage columns or row groups.
- **Medium is orthogonal:** `.Parquet()` doesn't care where the `.parquet` lives. Combine with the Http or S3 medium (their own skills) to read/write remote Parquet with the same declaration.
