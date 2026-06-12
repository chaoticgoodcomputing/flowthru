# Flowthru.Extensions.EFCore.Bulk

Swap the default per-row save on an EF Core Catalog Item for a provider-native bulk path.
This complements `Flowthru.Extensions.EFCore`: base EFCore saves via `SaveChanges` (change
tracking, one round-trip per batch of tracked entities), which is fine for the modest writes
most flows produce. Reach for this package when a step writes high volume — tens of thousands
of rows or more — where the per-row tracking overhead dominates. `BulkSave.Insert`,
`TruncateAndInsert`, `InsertOrUpdate`, and `InsertOrUpdateOrDelete` each return a `saveFunc` you
hand to `.WithSave(...)`, and the write becomes a single bulk-copy (e.g. Npgsql binary COPY).

[![coverage](https://codecov.io/gh/chaoticgoodcomputing/flowthru/branch/main/graph/badge.svg?component=flowthru_extensions_efcore_bulk)](https://codecov.io/gh/chaoticgoodcomputing/flowthru)

## Mental model

Same EF Core mental model as the base extension — your `DbContext`, your entity types, your
keys — but the save no longer goes through change tracking. It maps to the bulk-load command
your database provider exposes. Pick the strategy by the SQL semantics you want:
`Insert` (append), `TruncateAndInsert` (full replace), `InsertOrUpdate` (upsert by primary key),
`InsertOrUpdateOrDelete` (full sync — also deletes rows absent from the input). It is built on
[EFCore.BulkExtensions](https://github.com/borisdj/EFCore.BulkExtensions), so its provider
support and caveats apply.

## Install

```bash
dotnet add package Flowthru.Extensions.EFCore.Bulk
```

Hand a `BulkSave` strategy to the EF Core Item's `.WithSave(...)`:

```csharp
public IItem<IEnumerable<PreprocessedCompanySchema>> Companies =>
    CreateItem(() => Item.Of<IEnumerable<PreprocessedCompanySchema>>("StagingCompanies")
        .EFCoreQuery<PreprocessedCompanySchema, StagingDbContext>()
        .WithContextFactory(_contextFactory)
        .WithSave(BulkSave.Insert<PreprocessedCompanySchema, StagingDbContext>())
        .Build());
```

Tune batch size, timeout, or identity behaviour with `BulkSaveOptions`:

```csharp
.WithSave(BulkSave.TruncateAndInsert<MyEntity, MyDbContext>(
    new BulkSaveOptions { BatchSize = 5000, SetOutputIdentity = true }))
```
