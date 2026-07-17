---
name: flowthru-efcore
description: Deep skill for the Flowthru EFCore extension — backing Catalog Items with a relational database through an EF Core DbContext in a Flowthru (.NET) pipeline. Use when a project reads or writes a SQL database, declares .EFCoreTable/.EFCoreEntity/.EFCoreQuery items, or wires a DbContext into a Flow. Pairs with the umbrella `flowthru` skill.
metadata:
  flowthru:
    extension: Flowthru.Extensions.EFCore
    surface: database
    capability: Back Catalog Items with a relational DB via an EF Core DbContext — as a table, single entity, or deferred query.
    register: AddDbContextFactory(…) + b.UseEFCore()
---

# flowthru-efcore

Bridges an EF Core `DbContext` into the Catalog. Declare an Item as a table, a single-row entity, or a deferred query, point it at your `DbContext`, and a Flow loads typed rows from — and saves typed rows to — the database with the same one-line declaration any other Item uses.

Bring everything you know from EF Core: `DbSet<T>` per table, an entity with a key, `IDbContextFactory<TContext>` for a fresh context per operation, LINQ `Include`/`Where`/`OrderBy` to shape a read. An Item is just a named handle on a table.

## Three item shapes — match the SQL you mean

| Builder | Shape | Use when |
|---------|-------|----------|
| `.EFCoreTable<TSchema, TContext>()` | Multi-row, eager | You want all rows (optionally ordered/filtered) materialized. |
| `.EFCoreEntity<TSchema, TContext>()` | Single row | The Item is one record. |
| `.EFCoreQuery<TSchema, TContext>()` | Multi-row, deferred | The consuming step decides when to materialize. |

## Register

```bash
dotnet add package Flowthru.Extensions.EFCore
```

```csharp
// A fresh DbContext per Load/Save is the idiomatic concurrent pattern.
services.AddDbContextFactory<SpaceflightsDbContext>(options =>
    options.UseSqlite($"Data Source={dbPath}"));

services.AddFlowthru(b =>
{
    b.RegisterCatalog(sp => new Catalog(
        sp.GetRequiredService<IDbContextFactory<SpaceflightsDbContext>>()));

    // Optional pre-flight hooks: surface a bad connection string or a
    // misconfigured model at host startup, not at first flow run.
    b.VerifyEFCoreConnection<SpaceflightsDbContext>();
    b.VerifyEFCoreConfiguration<SpaceflightsDbContext>();
});
```

## Declare an item

<!-- flowthru:snippet:docs:item-efcore-table:start -->
```csharp
public IItem<IEnumerable<TrainingData>> TrainSplit =>
  CreateItem(() => Item.Of<IEnumerable<TrainingData>>("XTrain")
    .EFCoreTable<TrainingData, SpaceflightsDbContext>()
    .WithContextFactory(_contextFactory)
    .Build());
```
_(source: [`SpaceflightsEFCore/Catalog.ModelInput.cs`](https://github.com/chaoticgoodcomputing/flowthru/blob/main/examples/starter/SpaceflightsEFCore/Data/_05_ModelInput/Catalog.ModelInput.cs))_
<!-- flowthru:snippet:docs:item-efcore-table:end -->

Add `.WithQuery(q => q.OrderBy(…))` before `.Build()` to shape or order the read.

## Notes

- **`b.UseEFCore()`** is the opt-in scheduler gate that serializes concurrent writes to a single-writer database (e.g. SQLite) when running at `Parallelism > 1`. Reads and pooled-server writes stay parallel — add it only when you write to a single-writer DB concurrently.
- **Sub-packages:** `Flowthru.Extensions.EFCore.Bulk` (high-throughput bulk saves) and `Flowthru.Extensions.EFCore.Npgsql` (PostgreSQL) build on this — pull `flowthru-efcore-bulk` / `flowthru-efcore-npgsql` if the project uses them.
