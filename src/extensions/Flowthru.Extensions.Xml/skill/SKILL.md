---
name: flowthru-xml
description: Deep skill for the Flowthru Xml format extension — declaring document-mode XML-backed Catalog Items in a Flowthru (.NET) pipeline. Use when a project reads or writes .xml, especially hierarchical documents (config, manifests, coverage/build reports) rather than flat row sequences. Pairs with the umbrella `flowthru` skill.
metadata:
  flowthru:
    extension: Flowthru.Extensions.Xml
    surface: format
    capability: Document-mode XML format for a whole object per file — nested trees like config, manifests, and coverage reports, not row sequences.
    register: "— (declare a .Xml() item)"
---

# flowthru-xml

Adds the **XML format** to the Catalog. Format is one axis of a catalog item (format × medium × container — see the `flowthru` umbrella skill's `catalog-developers.md`); it decides how bytes serialize, not where they live or their in-memory shape.

**Reach for XML** when the payload is a hierarchical *document* — one file holds one whole object, a nested tree. That is the key difference from the row-oriented formats (Csv, Excel, Parquet): those back `IItem<IEnumerable<TSchema>>` (a row sequence); XML backs `IItem<T>` where `T` is the document type itself. Coverage reports, build manifests, and config are the natural fits. Reserve it for the raw edge where an external producer emits XML; prefer Parquet for typed intermediate/primary data.

## Use it

Reference the package — there is **no `UseXxx()` call**. Once referenced, `.Xml()` is available on the item builder:

```bash
dotnet add package Flowthru.Extensions.Xml
```

Bring the `System.Xml.Serialization` mental model: annotate the document type with `[XmlRoot]`, `[XmlAttribute]`, `[XmlElement]`, `[XmlArray]`, and the framework maps the tree onto that shape. A single document is one `.xml` file:

```csharp
public IItem<CoberturaReport> CoverageReport =>
  CreateItem(() => Item.Of<CoberturaReport>("CoverageReport")
    .Xml()
    .AtPath($"{_basePath}/_01_Raw/coverage.xml")
    .Build());
```

To read a whole **folder** of like documents as one Item, lift the format with the universal `.Directory(file => file.Xml())` — the container becomes `DirectoryOf<T>` and the path points at a directory:

<!-- flowthru:snippet:docs:item-xml:start -->
```csharp
public IItem<DirectoryOf<CoberturaReport>> CoverageXmlFiles =>
  CreateItem(() =>
    Item.Of<DirectoryOf<CoberturaReport>>("CoverageXmlFiles")
      .Directory(file => file.Xml())
      .AtPath($"{_basePath}/_01_Raw/Datasets")
      .Build()
  );
```
_(source: [`FlowthruCoverage/Catalog.Raw.cs`](https://github.com/chaoticgoodcomputing/flowthru/blob/main/examples/advanced/FlowthruCoverage/Data/_01_Raw/Catalog.Raw.cs))_
<!-- flowthru:snippet:docs:item-xml:end -->

## Notes

- **Container is the type, not a sequence.** A single-document item is `IItem<T>` (e.g. `IItem<CoberturaReport>`), never `IItem<IEnumerable<T>>`. The directory-lift form is `IItem<DirectoryOf<T>>`. Getting this wrong is the most common declaration mistake.
- **Filesystem-only today.** XML is filesystem-backed; a non-file path (an Http/S3 medium) is rejected at build time — a pre-flight/design-time error, not a runtime surprise.
- **Shape lives on the type, not the builder.** `.Xml()` maps whatever `System.Xml.Serialization` attributes decorate your record. If the document round-trips wrong, fix the `[Xml*]` annotations on the type — the builder has no column/element knobs.
- **Medium is orthogonal in principle** (format × medium × container), but XML's medium is pinned to the filesystem for now; combine with `.Directory(...)` to fan a folder into one Item.
