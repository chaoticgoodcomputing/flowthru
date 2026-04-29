---
title: Storage Adapter Architecture
description: Flowthru's storage layer factors *where* data lives, *how* it's serialized, and *what* container holds it into three composable layers, with capabilities and constraints unified through a trait system.
---

Flowthru's storage layer uses a three-layer composition pattern that separates *where* data lives, *how* it's serialized, and *what* container holds it. Storage capabilities and constraints propagate through this composition via a unified trait system.

## The Problem with Monolithic Adapters

Traditional data frameworks often have dedicated adapters for each combination:

- `CsvFileEnumerableReader`
- `CsvFileDataViewReader`
- `ParquetFileEnumerableReader`
- `ParquetFileDataViewReader`
- `JsonMemoryEnumerableReader`
- ...

With 3 storage locations, 4 formats, and 3 container types, you need 36 separate implementations. Each new format or container multiplies the work. Worse, capabilities like "read-only" or "streaming" must be duplicated across all implementations.

## The Composition Solution

Flowthru factors these concerns into three independent layers:

```
Medium (WHERE)    →    Format (HOW)       →    Container (WHAT)
File/Memory/Net        CSV/JSON/Parquet        IEnumerable/T
```

Each layer has a small interface:

- **`IStorageMedium`**: Provides read/write streams. Declares capabilities like persistence and network requirements.
- **`IFormatSerializer<TRow>`**: Converts between streams and row sequences. Declares capabilities like streaming support and write support.
- **`IStorageAdapter<T>`**: Combines medium and format (container is implicit in the type parameter). Merges capability traits from both layers.

The `ComposedStorageAdapter` combines medium and format:

```csharp
var adapter = new ComposedStorageAdapter<IEnumerable<Company>, Company>(
    medium: new FileStorageMedium("data.csv"),
    format: new CsvFormatSerializer<Company>()
);
```

## Storage Traits: Capability and Constraint Declaration

Each layer declares its capabilities through a `StorageTraits` property. This is a record struct with eight boolean flags:

```csharp
public record struct StorageTraits
{
    public bool CanRead { get; init; } = true;
    public bool CanWrite { get; init; } = true;
    public bool CanInspect { get; init; } = true;
    public bool IsPersistent { get; init; } = true;
    public bool RequiresNetwork { get; init; } = false;
    public bool CanStream { get; init; } = false;
    public bool CanAppend { get; init; } = false;
    public bool IsTransactional { get; init; } = false;
}
```

### Baseline Semantics

The default values encode **filesystem-file** semantics — the baseline data source that most developers understand intuitively:

- **Constraints narrow from baseline**: A medium can be less capable (memory is not persistent, HTTP sources can't write)
- **Capabilities widen beyond baseline**: A format can add features (CSV can stream, databases are transactional)

This design means the common case requires no explicit trait configuration:

```csharp
// Filesystem-file baseline — all defaults are correct
public StorageTraits Traits => new StorageTraits();
```

### Trait Composition: AND Logic

When `ComposedStorageAdapter` merges traits from medium and format, it uses AND logic — the most restrictive constraint wins, and only capabilities supported by *both* layers are available:

```csharp
public StorageTraits Traits
{
    get
    {
        var mediumTraits = _medium.Traits;
        var formatTraits = _format.Traits;
        
        return new StorageTraits
        {
            CanRead = mediumTraits.CanRead && formatTraits.CanRead,
            CanWrite = mediumTraits.CanWrite && formatTraits.CanWrite,
            CanStream = mediumTraits.CanStream && formatTraits.CanStream,
            // ... all 8 traits merged this way
        };
    }
}
```

**Why AND logic?** A composed adapter can only do what *both* layers support:

- CSV format can stream, but memory medium has the full dataset loaded → `CanStream = false`
- File medium persists data, but Excel format is read-only → `CanWrite = false`
- Network medium requires connectivity, local format doesn't → `RequiresNetwork = true` (constraint propagates)

### Practical Examples

**Read-only format (Excel):**
```csharp
public class ExcelFormatSerializer<TRow> : IFormatSerializer<TRow>
{
    public StorageTraits Traits => new StorageTraits { CanWrite = false };
    // All other traits use filesystem baseline defaults
}
```

**Non-persistent medium (in-memory):**
```csharp
public class MemoryStorageMedium : IStorageMedium
{
    public StorageTraits Traits => new StorageTraits { IsPersistent = false };
}
```

**Network-dependent, transactional adapter (database):**
```csharp
public class EFCoreStorageAdapter<T> : IStorageAdapter<IEnumerable<T>>
{
    public StorageTraits Traits { get; }
    
    public EFCoreStorageAdapter(DbContext context, bool readOnly)
    {
        Traits = new StorageTraits
        {
            CanWrite = !readOnly,
            RequiresNetwork = true,
            IsTransactional = true,
            CanStream = true  // EF Core supports streaming queries
        };
    }
}
```

## Catalog-Level Constraint Narrowing

After adapter creation, pipeline authors can further constrain catalog entries using `Item<T>.Constrain()`:

```csharp
public IItem<IEnumerable<Company>> Companies => GetOrCreateEntry(() =>
    ItemFactory.Enumerable.Csv<Company>("companies", "data/companies.csv")
        .Constrain(traits => traits with { CanWrite = false })
);
```

The `Constrain()` method enforces a **one-way ratchet**: you can only make entries *more* restrictive, never *less*. Attempting to grant capabilities the adapter doesn't support throws `InvalidOperationException` during catalog initialization:

```csharp
// ❌ Runtime error: adapter doesn't support streaming
csvEntry.Constrain(traits => traits with { CanStream = true })

// ✅ Valid: narrowing constraint
csvEntry.Constrain(traits => traits with { CanWrite = false })
```

This fail-fast behavior catches configuration errors during pipeline construction (pre-flight phase), not during node execution.

## Multiplicative Flexibility

With M mediums and F formats, you get M × F combinations from only M + F implementations:

| Mediums | Formats | Monolithic | Composed |
| ------- | ------- | ---------- | -------- |
| 2       | 3       | 6          | 5        |
| 3       | 4       | 12         | 7        |
| 4       | 5       | 20         | 9        |

Adding a new format (e.g., Avro) requires one implementation that immediately works with all existing mediums. The trait system propagates capabilities automatically.

## Design Philosophy

This architecture reflects three core principles:

### 1. Composition Over Inheritance

Rather than a deep class hierarchy of storage adapters, Flowthru uses composition to combine orthogonal concerns. Each layer is independently testable and reusable.

### 2. Fail-Fast Validation

Trait constraints surface at catalog initialization, not at node execution. If a pipeline tries to write to a read-only source, it fails *before* processing any data.

### 3. Explicit Over Implicit

Capabilities are declared explicitly via traits, not inferred from interface implementations or discovered at runtime. The available operations are visible at the type level.

## Migration from Marker Interfaces

Previous versions used marker interfaces (`ISeedable`, `IReadOnly`, `IStreamable<T>`). These had several problems:

- **Not composable**: Couldn't express "read-only + network-dependent"
- **Not queryable**: Couldn't check capabilities before attempting operations
- **Not fail-fast**: Wrote to read-only adapter failed at save time, not at construction

The trait system solves all three:

- **Composable**: Eight independent boolean flags
- **Queryable**: `adapter.Traits.CanWrite` checked before operation
- **Fail-fast**: `Item.Constrain()` validates at initialization

The old interfaces remain as `[Obsolete]` for backward compatibility but have no effect on pipeline behavior.

## Where This Lives in Code

The storage architecture is implemented in:

- `src/core/Flowthru/Data/Capabilities/StorageTraits.cs` — the trait record
- `src/core/Flowthru/Data/Storage/ComposedStorageAdapter.cs` — trait merging logic
- `src/core/Flowthru/Data/Item.cs` — `Constrain()` with ratchet validation
- `src/core/Flowthru/Data/Storage/Medium/` — medium implementations with traits
- `src/core/Flowthru/Data/Storage/Format/` — format serializers with traits

Catalog entry factories (`ItemFactory.Enumerable.Csv<T>(...)`) construct composed adapters with sensible defaults. Extension authors creating custom adapters should declare traits that accurately reflect their capabilities.

