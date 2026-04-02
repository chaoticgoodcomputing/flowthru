# Flowthru.Extensions.EFCore

Entity Framework Core integration for Flowthru data catalogs.

## Installation

```bash
dotnet add package Flowthru.Extensions.EFCore
```

## Quick Start

```csharp
using Flowthru.Data;
using Flowthru.Extensions.EFCore.Data;
using Microsoft.EntityFrameworkCore;

// 1. Configure DbContext — declare a key for each entity in OnModelCreating.
//    FlowthruSchema records work as EF entities without modification.
public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Company>().HasKey(e => e.Id);
    }
}

// 2. Register IDbContextFactory in DI — this is the idiomatic pattern for
//    concurrent pipeline execution. Do not use AddDbContext.
services.AddDbContextFactory<AppDbContext>(opts =>
    opts.UseSqlite("Data Source=pipeline.db"));

// 3. Accept the factory in your catalog and create entries with the typed overload.
public partial class Catalog : DataCatalogBase
{
    private readonly IDbContextFactory<AppDbContext> _contextFactory;

    public Catalog(IDbContextFactory<AppDbContext> contextFactory)
    {
        _contextFactory = contextFactory;
        using var ctx = contextFactory.CreateDbContext();
        ctx.Database.EnsureCreated();
        InitializeCatalogProperties();
    }

    public ICatalogEntry<IEnumerable<Company>> Companies =>
        GetOrCreateEntry(() =>
            EFCoreCatalogEntries.Enumerable.EFCore<Company, AppDbContext>(
                label: "Companies",
                contextFactory: _contextFactory
            )
        );
}
```

## Features

### ✅ Works with FlowthruSchemas

FlowthruSchemas work unchanged as EF entities. EF requires a primary key; declare it in `OnModelCreating` or via `HasKey()` — record types do not trigger EF's key convention automatically:

```csharp
[FlowthruSchema]
public partial record Product(
    int Id,
    string Name,
    decimal Price
);

// In OnModelCreating:
modelBuilder.Entity<Product>().HasKey(e => e.Id);
```

### ✅ Typed Context Overloads

Use `EFCore<T, TContext>` to preserve the concrete context type all the way to save delegates. No casting inside callbacks:

```csharp
// IDbContextFactory<TContext> overload — recommended for concurrent pipelines
EFCoreCatalogEntries.Enumerable.EFCore<Company, AppDbContext>(
    label: "Companies",
    contextFactory: dbContextFactory   // IDbContextFactory<AppDbContext>
)

// Func<TContext> overload — useful when constructing contexts manually
EFCoreCatalogEntries.Enumerable.EFCore<Company, AppDbContext>(
    label: "Companies",
    contextFactory: () => new AppDbContext(options)
)
```

### ✅ Query Customization

The `queryCustomizer` parameter shapes the query before execution. Use it for navigation property includes, filtering, or ordering:

```csharp
EFCoreCatalogEntries.Enumerable.EFCore<Person, AppDbContext>(
    label: "Persons",
    contextFactory: _contextFactory,
    queryCustomizer: q => q.Include(p => p.Address).AsNoTracking()
)

EFCoreCatalogEntries.Enumerable.EFCore<Shuttle, AppDbContext>(
    label: "Shuttles",
    contextFactory: _contextFactory,
    queryCustomizer: q => q.OrderBy(s => s.Id)
)
```

### ✅ Pluggable Save Delegates

Override the default `RemoveRange + AddRange` write strategy via `saveFunc`. The typed context is passed directly — no cast needed:

```csharp
EFCoreCatalogEntries.Enumerable.EFCore<Company, AppDbContext>(
    label: "Companies",
    contextFactory: _contextFactory,
    saveFunc: async (ctx, data, ct) =>
    {
        // ctx is AppDbContext — no cast
        await ctx.Database.ExecuteSqlRawAsync("TRUNCATE TABLE companies", ct);
        await ctx.Set<Company>().AddRangeAsync(data, ct);
        await ctx.SaveChangesAsync(ct);
    }
)
```

To reference the default save in a composition scenario:

```csharp
saveFunc: async (ctx, data, ct) =>
{
    // wrap the default
    await EFCoreStorageAdapter<Company>.DefaultSave(ctx, data, ct);
}
```

### ✅ Single-Entity Storage

For tables that store exactly one row (trained models, configuration records, aggregated metrics):

```csharp
EFCoreCatalogEntries.Single.EFCore<ModelMetrics, AppDbContext>(
    label: "ModelMetrics",
    contextFactory: _contextFactory
)
```

The adapter validates "exactly one row" during pre-flight. Use `allowEmptyData: true` for tables that may be empty on first run.

### ✅ Read-Only Entries

Use `.Constrain()` to prevent writes. The pipeline fails at build time — not at runtime — if a node attempts to write to a constrained entry:

```csharp
EFCoreCatalogEntries.Enumerable.EFCore<SourceRecord, SourceDbContext>(
    label: "SourceData",
    contextFactory: _sourceFactory)
.Constrain(traits => traits with { CanWrite = false })
```

### ✅ Optional Tables

By default, an empty table fails pre-flight validation. Set `allowEmptyData: true` for tables that are legitimately empty on first run:

```csharp
EFCoreCatalogEntries.Enumerable.EFCore<AuditEvent, AppDbContext>(
    label: "AuditEvents",
    contextFactory: _contextFactory,
    allowEmptyData: true
)
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
var pipeline = new FlowBuilder("Setup")
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
