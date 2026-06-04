# Flowthru.Extensions.EFCore

Read and write Flowthru Catalog Items against a relational database through an EF Core
`DbContext`. Declare an Item as an EF Core table, a single-row entity, or a deferred query,
point it at your `DbContext`, and a Flow loads typed rows from — and saves typed rows to —
the database with the same one-line declaration any other Item uses.

[![coverage](https://codecov.io/gh/chaoticgoodcomputing/flowthru/branch/main/graph/badge.svg?component=flowthru_extensions_efcore)](https://codecov.io/gh/chaoticgoodcomputing/flowthru)

## Mental model

This bridges your EF Core `DbContext` into the Catalog. Bring everything you already know —
`DbSet<T>` per table, an entity type with a key, `IDbContextFactory<TContext>` for a fresh
context per operation, LINQ `Include`/`Where`/`OrderBy` for shaping a read. The Item is just a
named handle on a table: the read is your `DbContext` query, the write is a save you can
customize. Three shapes match the SQL you mean — `EFCoreTable` (multi-row, eager),
`EFCoreEntity` (single row), and `EFCoreQuery` (multi-row, deferred — the step decides when to
materialise).

## Install

```bash
dotnet add package Flowthru.Extensions.EFCore
```

Register your `DbContext` factory, then declare an EF Core-backed Item in your Catalog:

```csharp
// Host wiring — a fresh DbContext per Load/Save is the idiomatic concurrent pattern.
services.AddDbContextFactory<SpaceflightsDbContext>(options =>
    options.UseSqlite($"Data Source={dbPath}"));

services.AddFlowthru(b =>
{
    b.RegisterCatalog(sp => new Catalog(
        sp.GetRequiredService<IDbContextFactory<SpaceflightsDbContext>>()));

    // Optional pre-flight hooks — surface a bad connection string or a
    // misconfigured model at host startup, not at first flow run.
    b.VerifyEFCoreConnection<SpaceflightsDbContext>();
    b.VerifyEFCoreConfiguration<SpaceflightsDbContext>();
});

// In the Catalog — a multi-row table, ordered on read:
public IItem<IEnumerable<ModelInputTableSchema>> ModelInputTable =>
    CreateItem(() => Item.Of<IEnumerable<ModelInputTableSchema>>("ModelInputTable")
        .EFCoreTable<ModelInputTableSchema, SpaceflightsDbContext>()
        .WithContextFactory(_contextFactory)
        .WithQuery(q => q.OrderBy(r => r.ShuttleId))
        .Build());
```

`UseEFCore()` is the opt-in scheduler gate that serializes concurrent writes to a single-writer
database (e.g. SQLite) when running at `Parallelism > 1`; reads and pooled-server writes stay
parallel.
