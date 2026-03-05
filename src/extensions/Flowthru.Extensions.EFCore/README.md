# Flowthru.Extensions.EFCore

Entity Framework Core integration for Flowthru data catalogs.

## Installation

```bash
dotnet add package Flowthru.Extensions.EFCore
```

## Quick Start

```csharp
using Flowthru.Data;
using Microsoft.EntityFrameworkCore;

// Define your entity (FlowthruSchema works as EF entity)
[FlowthruSchema]
public record Company(int Id, string Name, string Industry);

// Configure DbContext
public class AppDbContext : DbContext
{
    public DbSet<Company> Companies { get; set; }
    
    protected override void OnConfiguring(DbContextOptionsBuilder options)
        => options.UseSqlServer("Server=localhost;Database=MyApp;...");
}

// Create catalog entry
public static partial class DataCatalog
{
    public static ICatalogEntry<IEnumerable<Company>> Companies(DbContext db) =>
        CatalogEntries.Enumerable.EFCore<Company>("companies", db);
}

// Use in pipeline
var pipeline = new PipelineBuilder("MyPipeline")
    .AddNode("load", catalog => new LoadNode(
        outputs: catalog.Companies(db)
    ))
    .Build();
```

## Features

### ✅ Works with FlowthruSchemas

FlowthruSchemas work unchanged as EF entities. No additional attributes required:

```csharp
[FlowthruSchema]
public record Product(
    int Id,
    string Name,
    decimal Price,
    DateTime CreatedAt
);

// Automatically implements IFlatSchema, IStructuredSerializable
// Works with both EFCore and file-based catalogs (CSV, JSON, etc.)
```

### ✅ Hybrid DbContext Management

**Injected DbContext** (caller owns lifecycle):

```csharp
// From DI container
var entry = CatalogEntries.Enumerable.EFCore<Company>("companies", dbContext);

// Adapter does NOT dispose DbContext
// Useful for shared DbContext across multiple catalog entries
```

**Factory-based DbContext** (adapter owns lifecycle):

```csharp
// Fresh DbContext per operation
var entry = CatalogEntries.Enumerable.EFCore<Company>(
    "companies",
    () => new AppDbContext(options)
);

// Adapter creates and disposes DbContext after each Load/Save
// Useful for scoped DbContext patterns
```

### ✅ Read-Only Mode

```csharp
// Prevent writes to production database
var readOnly = CatalogEntries.Enumerable.EFCore<Company>(
    "companies",
    dbContext,
    readOnly: true
);

// Save operations will fail with InvalidOperationException
```

### ✅ Seedable Support

Database catalog entries can be seeds (Layer 0 inputs) if table exists:

```csharp
// Pipeline automatically detects existing tables as seeds
var pipeline = new PipelineBuilder("ETL")
    .AddNode("extract", catalog => new ExtractNode(
        inputs: catalog.SourceData(sourceDb),  // Seed from source database
        outputs: catalog.RawData()
    ))
    .Build();
```

## Architecture

This is a **specialized adapter** that directly implements `IStorageAdapter<IEnumerable<T>>` rather than using the Medium→Format→Container composition pattern.

**Why?** EFCore inherently couples:
- **WHERE:** Connection string + database engine
- **HOW:** Entity mapping + LINQ-to-SQL translation  
- **WHAT:** DbSet<T> query interface

Attempting to decompose these concerns would fight EFCore's architecture.

## Save Semantics

The `Save()` operation follows **replace semantics** (matching file-based storage):

1. Remove all existing rows from table
2. Insert new rows from provided collection
3. Commit transaction

For append or upsert semantics, implement custom node logic:

```csharp
public class UpsertNode : PipelineNode
{
    protected override async Task<FlowUnit> ExecuteAsync()
    {
        var data = await Inputs.NewData.Load();
        
        // Custom upsert logic
        foreach (var item in data)
        {
            var existing = await db.Companies.FindAsync(item.Id);
            if (existing != null)
                db.Entry(existing).CurrentValues.SetValues(item);
            else
                db.Companies.Add(item);
        }
        
        await db.SaveChangesAsync();
        return FlowUnit.Default;
    }
}
```

## Migrations

Run migrations in a dedicated setup step before pipeline execution:

```csharp
// Option 1: Manual migration before pipeline
await db.Database.MigrateAsync();
await pipeline.ExecuteAsync();

// Option 2: Migration node in pipeline
var pipeline = new PipelineBuilder("Setup")
    .AddNode("migrate", catalog => new MigrationNode(db))
    .AddNode("load_data", catalog => new LoadNode(...))
    .Build();
```

## Transactions

Each `Load()` and `Save()` operation is an independent transaction. For multi-catalog transactional consistency, share a DbContext and manage transactions explicitly:

```csharp
using var transaction = await db.Database.BeginTransactionAsync();
try
{
    await companiesEntry.Save(companies);
    await productsEntry.Save(products);
    await transaction.CommitAsync();
}
catch
{
    await transaction.RollbackAsync();
    throw;
}
```

## Pre-flight Validation

The `Exists()` operation checks if the table exists and contains data. For empty-table scenarios, this may need refinement based on your use case.

## Comparison with Format Extensions

| Aspect        | EFCore Extension                           | CSV/Parquet/Excel Extensions               |
| ------------- | ------------------------------------------ | ------------------------------------------ |
| Pattern       | Specialized adapter                        | Composed adapter (Medium→Format→Container) |
| Why           | EFCore couples connection+mapping+querying | File I/O has orthogonal concerns           |
| Abstraction   | Direct `IStorageAdapter<T>`                | `ComposedStorageAdapter<TContainer, TRow>` |
| Extensibility | Custom adapter per database concern        | Mix-and-match Medium/Format/Container      |

## License

Apache-2.0
