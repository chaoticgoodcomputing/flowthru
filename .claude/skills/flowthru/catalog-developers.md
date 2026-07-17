# Catalog Developers

This is the **conceptual** guide to schemas and catalog items — the typed data definitions a Flow reads and writes. For Steps and Flows, see [flow-developers.md](flow-developers.md). For the formats, media, and databases you can back an item with, see [extensions.md](extensions.md).

## Schemas: the shape of a record

A schema is the typed shape of one record. Declare it as a `[FlowthruSchema] public partial record` in the `Schemas/` folder of the data layer it describes.

```csharp
[FlowthruSchema]
public partial record IrisRawSchema
{
    [SerializedLabel("sepal_length")]
    public required double SepalLength { get; init; }

    [SerializedLabel("species")]
    public required string Species { get; init; }
}
```

- **`partial` is required** — a source generator adds members, and omitting `partial` is a design-time error (FT1001).
- **`[SerializedLabel("ext_name")]`** aliases a property to its external serialized name (a CSV header, JSON key, column). Without it, the property name is used verbatim.
- **`[SerializedEnum]`** serializes an enum as its string name rather than its numeric value.
- Prefer `required … { get; init; }` properties — records are immutable data, and `required` makes a missing field a compile error at every construction site.

## Catalog items: typed handles to stored data

A **catalog item** is a typed handle to a piece of stored data — `IItem<T>`, where `T` is the schema (or a collection of schemas). Items live as properties on the `Catalog`, which is a `partial class Catalog : CatalogAbstract` split across one file per data layer (`Catalog.Raw.cs`, `Catalog.Intermediate.cs`, …) with the root `Catalog.cs` holding the constructor.

```csharp
public partial class Catalog   // in Data/_01_Raw/Catalog.Raw.cs
{
    public IItem<IEnumerable<IrisRawSchema>> IrisRaw =>
        CreateItem(() => Item.Of<IEnumerable<IrisRawSchema>>("IrisRaw")
            .Json()
            .AtPath($"{_basePath}/_01_Raw/Datasets/iris.json")
            .Build());
}
```

Every item is a property returning `CreateItem(() => Item.Of<T>("label")…​.Build())`. The `"label"` is the item's identity in the DAG and diagnostics; keep it stable.

## The three storage axes

An item is defined by three independent axes. Reading them off the fluent chain above: **container × format × medium.**

| Axis | Question it answers | How it's expressed | Where it comes from |
|------|--------------------|--------------------|---------------------|
| **Container** | What in-memory shape? | `IItem<IEnumerable<T>>` = many rows; `IItem<T>` = a singleton (one object). | Core |
| **Format** | How do bytes serialize? | `.Json()`, `.Csv()`, `.Excel().WithSheet(...)`, `.Parquet()`, `.Xml()`, … | **Extensions** ([extensions.md](extensions.md)) |
| **Medium** | Where do the bytes live? | `.AtPath(...)` — a local path, or an `https://…` / `s3://…` URI that routes through a registered medium. | Core (filesystem) + **Extensions** (Http, S3) |

These axes are orthogonal: a Parquet format doesn't know about HTTP, and the HTTP medium doesn't know about Parquet — they meet through the resolver. So "read a Parquet file over HTTP" is just `.Parquet().AtPath("https://…")` once `UseHttp()` is registered. **`JSON` is built into Core; every other format and non-filesystem medium comes from an extension** — check [extensions.md](extensions.md) for what's available and which package/`UseXxx()` to add.

## Configuration-bound items

A step's options are modeled as a catalog item too, bound from `appsettings.json` so a config change invalidates the affected downstream cache automatically:

```csharp
public IItem<SplitAndEncodeStep.Options> SplitOptions =>
    CreateItem(() => Item.Of<SplitAndEncodeStep.Options>("SplitOptions")
        .FromConfiguration(_configuration)
        .AtSection("Flowthru:Flows:DataEngineering:SplitOptions")
        .Build());
```

For this to resolve, `Program.cs` registers the configuration with `b.UseConfiguration(configuration)` and the `Catalog` constructor receives the `IConfiguration`.

## Two constructor forms

You'll see two ways to construct an item; **both are valid — match the form already used in this project.**

- **Fluent (canonical):** `Item.Of<T>("label").<Format>().AtPath(...).Build()` — reads as container → format → medium, and is what the reference examples use.
- **Factory:** `ItemFactory.<Container>.<Format><T>(...)` — e.g. `ItemFactory.Enumerable.Parquet<Row>(...)`, `ItemFactory.Enumerable.EFCore<Row, TDbContext>(...)`. Common where an extension ships a factory helper.

Both are wrapped in `CreateItem(() => …)` on the property. When adding to an existing catalog, mirror its prevailing style rather than mixing the two.

## Registration

The `Catalog` is registered once in `Program.cs`:

```csharp
b.RegisterCatalog(sp => new Catalog(basePath, sp.GetRequiredService<IConfiguration>()));
```

From there, `Flow.Create(Catalog catalog, …)` receives it and references items by property (`catalog.IrisRaw`) when wiring steps — see [flow-developers.md](flow-developers.md).
