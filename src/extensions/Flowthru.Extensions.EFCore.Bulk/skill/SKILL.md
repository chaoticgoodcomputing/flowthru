---
name: flowthru-efcore-bulk
description: Deep skill for the Flowthru EFCore.Bulk sub-package — swap an EF Core Catalog Item's default per-row save for a provider-native bulk-copy path. Use when a step writes high volume (tens of thousands of rows or more) to a SQL database and change-tracking overhead dominates. Pairs with the umbrella `flowthru` skill.
metadata:
  flowthru:
    extension: Flowthru.Extensions.EFCore.Bulk
    surface: database
    capability: Swap an EF Core Item's per-row save for a provider-native bulk-copy — Insert, TruncateAndInsert, InsertOrUpdate, or InsertOrUpdateOrDelete.
    register: .WithSave(BulkSave.…) on an EF Core item
---

# flowthru-efcore-bulk

Sub-package that builds on `Flowthru.Extensions.EFCore` — pull `flowthru-efcore` first for the base model (`.EFCoreTable`/`.EFCoreEntity`/`.EFCoreQuery`, `DbContext`, context factory). Parent shard: `--skill flowthru-efcore` ([SKILL.md](https://github.com/chaoticgoodcomputing/flowthru/blob/main/src/extensions/Flowthru.Extensions.EFCore/SKILL.md)).

## What it adds over base EFCore

Base EFCore saves via `SaveChanges` — change tracking, one round-trip per batch of tracked entities. Fine for the modest writes most flows produce. This package replaces only the *save*: each `BulkSave.*` factory returns a `saveFunc` you hand to `.WithSave(...)`, and the write becomes a single provider-native bulk-copy (e.g. Npgsql binary `COPY`). Reads, keys, entity types, and the rest of the item are unchanged.

Reach for it when a step writes high volume — tens of thousands of rows or more — where per-row tracking overhead dominates. Built on [EFCore.BulkExtensions](https://github.com/borisdj/EFCore.BulkExtensions), so its provider support and caveats apply.

## Register

```bash
dotnet add package Flowthru.Extensions.EFCore.Bulk
```

Pick the strategy by the SQL semantics you want, then hand it to the item's `.WithSave(...)`:

| Strategy | SQL semantics |
|----------|---------------|
| `BulkSave.Insert` | Append rows. |
| `BulkSave.TruncateAndInsert` | Full replace — truncate, then insert. |
| `BulkSave.InsertOrUpdate` | Upsert by primary key. |
| `BulkSave.InsertOrUpdateOrDelete` | Full sync — also deletes rows absent from the input. |

<!-- flowthru:snippet:docs:item-efcore-bulk:start -->
```csharp
public IItem<IEnumerable<ModelPredictions>> ModelPredictions =>
  CreateItem(() => Item.Of<IEnumerable<ModelPredictions>>("ModelPredictions")
    .EFCoreQuery<ModelPredictions, ProductionDbContext>()
    .WithContextFactory(_contextFactory)
    .WithSave(BulkSave.Insert<ModelPredictions, ProductionDbContext>())
    .WithScope(DbScope.Explicit(StagingCatalog.SharedScope))
    .Build());
```
_(source: [`SpaceflightsStagingSchema/Catalog.ModelOutput.cs`](https://github.com/chaoticgoodcomputing/flowthru/blob/main/examples/advanced/SpaceflightsStagingSchema/Data/_07_ModelOutput/Catalog.ModelOutput.cs))_
<!-- flowthru:snippet:docs:item-efcore-bulk:end -->

Tune batch size, timeout, or identity behaviour with `BulkSaveOptions`:

```csharp
.WithSave(BulkSave.TruncateAndInsert<MyEntity, MyDbContext>(
    new BulkSaveOptions { BatchSize = 5000, SetOutputIdentity = true }))
```

## Gotchas

- **Only the save path changes.** `BulkSave.*` overrides how the item *writes*; loading, ordering, and the item shape stay whatever the base `.EFCoreTable`/`.EFCoreQuery` declared. Both type parameters (`<TSchema, TContext>`) are required and must match the item's own.
- **No change tracking means no tracked-entity side effects.** The bulk path issues the provider's bulk-load command directly — cascades, interceptors, and computed-in-.NET values that rely on the change tracker do not run. Model that state in SQL or in the step producing the rows.
- **Provider support follows EFCore.BulkExtensions.** If your database provider isn't supported there, the strategy won't work — check its provider matrix before choosing this over the base save.
- **Cross-Postgres bulk *transfers* are a different package.** For byte-level PostgreSQL-to-PostgreSQL promotion (no rows in .NET), reach for `flowthru-efcore-npgsql` and `AddBulkTransfer` instead — Bulk is for high-throughput *saves* of in-.NET rows.
