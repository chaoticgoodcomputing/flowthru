# Storage Composition

Flowthru's storage layer uses a three-layer composition pattern that separates *where* data lives, *how* it's serialized, and *what* in-memory structure holds it. This document explains why.

## The Problem with Monolithic Adapters

Traditional data frameworks often have dedicated adapters for each combination:

- `CsvFileEnumerableReader`
- `CsvFileDataViewReader`
- `ParquetFileEnumerableReader`
- `ParquetFileDataViewReader`
- `JsonMemoryEnumerableReader`
- ...

With 3 storage locations, 4 formats, and 3 container types, you'd need 36 separate implementations. Each new format or container multiplies the work.

## The Composition Solution

Flowthru factors these concerns into three independent layers:

```
Medium (WHERE)    →    Format (HOW)       →    Container (WHAT)
File/Memory/Net        CSV/JSON/Parquet        IEnumerable/Seq/IDataView
```

Each layer has a small interface:

- **`IStorageMedium`**: Provides read/write streams. Implementations: `FileStorageMedium`, `MemoryStorageMedium`
- **`IFormatSerializer<TRow>`**: Converts between streams and row sequences. Implementations: `CsvFormatSerializer`, `JsonFormatSerializer`, `ParquetFormatSerializer`
- **`IContainerAdapter<TContainer, TRow>`**: Wraps row sequences in application-specific containers. Implementations: `EnumerableContainerAdapter`, `SeqContainerAdapter`

The `ComposedStorageAdapter` combines these three:

```csharp
var adapter = new ComposedStorageAdapter<IEnumerable<Company>, Company>(
    medium: new FileStorageMedium("data.csv"),
    format: new CsvFormatSerializer<Company>(),
    container: new EnumerableContainerAdapter<Company>()
);
```

## Multiplicative Flexibility

With M mediums, F formats, and C containers, you get M × F × C combinations from only M + F + C implementations:

| Mediums | Formats | Containers | Monolithic | Composed |
| ------- | ------- | ---------- | ---------- | -------- |
| 2       | 3       | 2          | 12         | 7        |
| 3       | 4       | 3          | 36         | 10       |
| 4       | 5       | 4          | 80         | 13       |

Adding a new format (e.g., Avro) requires one implementation that immediately works with all existing mediums and containers.

## Schema Compatibility

Not all combinations are valid. CSV can't represent nested structures; Parquet requires specific column types. Flowthru enforces this at compile time through marker interfaces:

- `IFlatSchema` — compatible with CSV and other tabular formats
- `ITextSerializable` — can serialize to text-based formats
- `IBinarySerializable` — can serialize to binary formats
- `IStructuredSerializable` — can handle nested/complex structures

The `[FlowthruSchema]` source generator analyzes your schema's properties and adds the appropriate interfaces. If you try to save a nested schema to CSV, the code won't compile.

## Where This Lives in Code

The composition implementation is in:

- `src/Flowthru/Data/Storage/ComposedStorageAdapter.cs` — the compositor
- `src/Flowthru/Data/Storage/Mediums/` — medium implementations
- `src/Flowthru/Data/Storage/Formats/` — format serializers
- `src/Flowthru/Data/Storage/Containers/` — container adapters

The catalog entry factories (`CatalogEntries.Enumerable.Csv<T>(...)`) are convenience methods that construct composed adapters with common configurations.
