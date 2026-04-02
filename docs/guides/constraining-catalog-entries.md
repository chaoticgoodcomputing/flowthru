# Constraining Catalog Entries

Apply additional constraints to catalog entries to enforce data access policies and catch configuration errors early. Constraints fail fast during pipeline construction, not during execution.

## When to Use Constraints

Use constraints when:

- **Data is read-only** — external APIs, reference data, or archived datasets that shouldn't be modified
- **Access is limited** — write-protected production databases queried for reporting
- **Policy enforcement** — audit logs that must be append-only
- **Documentation** — make implicit constraints explicit for other developers

Constraints should NOT be used to manage permissions of resources outside of Flowthru. If you've got a catalog entry representing a production database that you don't want to write to, you should still take measures to prevent Flowthru's connection from allowing writes.

Constraints are to describe constraints to your pipeline, so that you can catch impossible actions — like writing to a read-only database connection — so that they won't crash your pipeline accidentally.

## Basic Usage

Call `.Constrain()` on any catalog entry to narrow its capabilities:

```csharp
public class DataCatalog : DataCatalogBase
{
    public ICatalogEntry<IEnumerable<Company>> Companies => GetOrCreateEntry(() =>
        CatalogEntries.Enumerable.Csv<Company>("companies", "data/companies.csv")
            .Constrain(traits => traits with { CanWrite = false })
    );
}
```

A constraint like this would let Flowthru know that this catalog entry can't be produced, or written to, by a node. It's a raw, immutable data source that Flowthru shouldn't be able to change.

## Common Constraint Scenarios

### Read-Only Data Sources

Mark external data sources as read-only to prevent accidental modifications:

```csharp
// API endpoint that only supports GET requests
public ICatalogEntry<WeatherData> WeatherFeed => GetOrCreateEntry(() =>
    CatalogEntries.Single.Http<WeatherData>("weather", "https://api.weather.com/current")
        .Constrain(traits => traits with { CanWrite = false })
);

// Production database view used for reporting
public ICatalogEntry<IEnumerable<SalesRecord>> ProductionSales => GetOrCreateEntry(() =>
    CatalogEntries.Enumerable.EFCore<SalesRecord>("sales", dbContext)
        .Constrain(traits => traits with { CanWrite = false })
);

// Archived historical data that must not change
public ICatalogEntry<IEnumerable<Transaction>> HistoricalTransactions => GetOrCreateEntry(() =>
    CatalogEntries.Enumerable.Parquet<Transaction>("archive", "data/2024/transactions.parquet")
        .Constrain(traits => traits with { CanWrite = false })
);
```

### Network-Dependent Sources

Mark catalog entries that require network connectivity to prevent offline execution attempts:

```csharp
public ICatalogEntry<IEnumerable<User>> RemoteUsers => GetOrCreateEntry(() =>
    CatalogEntries.Enumerable.Http<User>("users", "https://internal.api/users")
        .Constrain(traits => traits with 
        { 
            RequiresNetwork = true,
            CanWrite = false 
        })
);
```

### Non-Persistent Intermediate Data

Mark in-memory or cache-based entries as non-persistent:

```csharp
public ICatalogEntry<ProcessedData> Cache => GetOrCreateEntry(() =>
    CatalogEntries.Single.Memory<ProcessedData>("cache")
    // Memory adapter already sets IsPersistent = false
);
```

## How Constraints Fail Fast

If you try to use a constrained entry incorrectly, the pipeline fails during construction:

```csharp
// ❌ This fails at pipeline.Build() — before any data is processed
pipeline.AddStep(
    name: "WriteToReadOnly",
    transform: node,
    input: catalog.InputData,
    output: catalog.ReadOnlySource  // CanWrite = false
);
```

**Error:** `InvalidOperationException: Cannot use read-only catalog entry 'ReadOnlySource' as node output`

This is better than discovering the error after processing gigabytes of data.

## Constraint Validation

Constraints follow a **one-way ratchet**: you can only make entries *more* restrictive, never *less*:

```csharp
// ✅ Valid: narrowing constraint
csvEntry.Constrain(traits => traits with { CanWrite = false })

// ❌ Invalid: trying to grant capability adapter doesn't support
excelEntry.Constrain(traits => traits with { CanWrite = true })
// Excel adapter already has CanWrite = false

// ❌ Invalid: trying to remove constraint that adapter requires
dbEntry.Constrain(traits => traits with { RequiresNetwork = false })
// Database adapter already has RequiresNetwork = true
```

Attempts to relax constraints throw `InvalidOperationException` with a detailed error message showing which trait was incorrectly modified.

## Multiple Constraints

Apply multiple constraints in a single call:

```csharp
public ICatalogEntry<IEnumerable<AuditLog>> AuditLogs => GetOrCreateEntry(() =>
    CatalogEntries.Enumerable.Csv<AuditLog>("audit", "logs/audit.csv")
        .Constrain(traits => traits with 
        { 
            CanWrite = false,
            CanInspect = true,
            IsPersistent = true
        })
);
```

Or chain multiple constraints for readability:

```csharp
public ICatalogEntry<IEnumerable<Customer>> Customers => GetOrCreateEntry(() =>
    CatalogEntries.Enumerable.Csv<Customer>("customers", "data/customers.csv")
        .Constrain(traits => traits with { CanWrite = false })
        .Constrain(traits => traits with { RequiresNetwork = false })
);
```

Both forms produce the same result — choose based on readability preference.

## Available Constraint Properties

| Property          | Default | Meaning                                                       |
| ----------------- | ------- | ------------------------------------------------------------- |
| `CanRead`         | `true`  | Supports Load operations                                      |
| `CanWrite`        | `true`  | Supports Save operations                                      |
| `CanInspect`      | `true`  | Supports pre-flight validation                                |
| `IsPersistent`    | `true`  | Data survives process restart                                 |
| `RequiresNetwork` | `false` | Needs network connectivity                                    |
| `CanStream`       | `false` | Supports lazy streaming (memory-efficient for large datasets) |
| `CanAppend`       | `false` | Supports appending without overwriting                        |
| `IsTransactional` | `false` | Supports rollback on error                                    |

Default values represent **filesystem-file** semantics — the common case for local data pipelines.

## Practical Patterns

### Reference Data Protection

Prevent pipelines from modifying reference data used across multiple projects:

```csharp
public ICatalogEntry<IEnumerable<Country>> Countries => GetOrCreateEntry(() =>
    CatalogEntries.Enumerable.Csv<Country>("countries", "reference/countries.csv")
        .Constrain(traits => traits with { CanWrite = false })
);

public ICatalogEntry<IEnumerable<Currency>> Currencies => GetOrCreateEntry(() =>
    CatalogEntries.Enumerable.Json<Currency>("currencies", "reference/currencies.json")
        .Constrain(traits => traits with { CanWrite = false })
);
```

### Environment-Specific Constraints

Use configuration to apply different constraints in different environments:

```csharp
public ICatalogEntry<IEnumerable<Order>> Orders => GetOrCreateEntry(() =>
{
    var entry = CatalogEntries.Enumerable.EFCore<Order>("orders", dbContext, readOnly: false);
    
    // In production, make orders read-only for reporting pipelines
    if (_environment.IsProduction)
    {
        entry = entry.Constrain(traits => traits with { CanWrite = false });
    }
    
    return entry;
});
```

### Multi-Stage Pipeline Guards

In multi-stage pipelines where early stages produce data for later stages, prevent later stages from writing to early-stage outputs:

```csharp
// Stage 1: Data ingestion (writes allowed)
public ICatalogEntry<IEnumerable<RawData>> RawData => GetOrCreateEntry(() =>
    CatalogEntries.Enumerable.Csv<RawData>("raw", "data/raw.csv")
);

// Stage 2: Used as read-only input to transformations
public ICatalogEntry<IEnumerable<RawData>> RawDataReadOnly => GetOrCreateEntry(() =>
    CatalogEntries.Enumerable.Csv<RawData>("raw_readonly", "data/raw.csv")
        .Constrain(traits => traits with { CanWrite = false })
);
```

Use `RawData` as output in ingestion pipelines, `RawDataReadOnly` as input in transformation pipelines.

## See Also

- **[Storage Adapter Architecture](/docs/explanation/advanced/storage-composition.md)** — How traits propagate through storage layers (contributor documentation)
- **[Slicing Pipelines](/docs/guides/slicing-pipelines.md)** — Execute subsets of your pipeline for testing
