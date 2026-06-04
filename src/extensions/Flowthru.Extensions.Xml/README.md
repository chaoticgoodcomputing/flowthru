# Flowthru.Extensions.Xml

Read and write Flowthru Catalog Items as XML documents. Adds the XML **format** to the Catalog
builder, so an Item backed by a single object serializes to and from one `.xml` file with a
one-line declaration. Unlike the row-oriented formats (Csv, Excel, Parquet), XML here is
**document-mode**: one file holds one whole object — a nested tree, not a flat row sequence — so
it's the right fit for hierarchical documents like config, manifests, or coverage reports.

[![coverage](https://codecov.io/gh/chaoticgoodcomputing/flowthru/branch/main/graph/badge.svg?component=flowthru_extensions_xml)](https://codecov.io/gh/chaoticgoodcomputing/flowthru)

## Mental model

Storage in Flowthru is three independent axes: **format** (how bytes serialize) × **medium**
(where bytes live) × **container** (the in-memory shape). This package supplies one format — XML —
in its document shape. Bring the `System.Xml.Serialization` mental model: you annotate your type
with `[XmlRoot]`, `[XmlAttribute]`, `[XmlElement]`, and `[XmlArray]`, and the framework maps the
document tree onto that shape. One file is one document of type `T`; to read a whole folder of
like documents, lift the format with `.Directory(file => file.Xml())`. XML is filesystem-backed
today — a non-file path is rejected at build time.

## Install

```bash
dotnet add package Flowthru.Extensions.Xml
```

Declare an XML-backed Item over a serializable type. Note the container is the type itself
(`IItem<CoberturaReport>`), not `IEnumerable<...>`:

```csharp
public IItem<CoberturaReport> CoverageReport =>
    CreateItem(() => Item.Of<CoberturaReport>("CoverageReport")
        .Xml()
        .AtPath($"{_basePath}/Data/_01_Raw/coverage.xml")
        .Build());
```

To read a directory of like documents as one Item, lift the format with the universal
`.Directory(...)`:

```csharp
public IItem<DirectoryOf<CoberturaReport>> CoverageXmlFiles =>
    CreateItem(() => Item.Of<DirectoryOf<CoberturaReport>>("CoverageXmlFiles")
        .Directory(file => file.Xml())
        .AtPath($"{_basePath}/Data/_01_Raw/Datasets")
        .Build());
```
