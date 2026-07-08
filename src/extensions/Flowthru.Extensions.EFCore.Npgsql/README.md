# Flowthru.Extensions.EFCore.Npgsql

Move PostgreSQL-to-PostgreSQL data as raw bytes instead of rows. This complements
`Flowthru.Extensions.EFCore`: declare a table Item with `.NpgsqlTable<TRow, TContext>()` instead
of `.EFCoreTable<TRow, TContext>()`, and when *both* ends of an `AddBulkTransfer(...)` are
declared this way, Flowthru's pre-flight negotiation selects the **native rung** — the source's
`COPY ... TO STDOUT (FORMAT BINARY)` pumped straight into the target's
`COPY ... FROM STDIN (FORMAT BINARY)`. No row is ever materialised in .NET, which turns the
10–20× row-marshalling tax on large cross-database promotions into a byte copy (measured ~20×
on a 200k-row table against the streaming rung).

[![coverage](https://codecov.io/gh/chaoticgoodcomputing/flowthru/branch/main/graph/badge.svg?component=flowthru_extensions_efcore_npgsql)](https://codecov.io/gh/chaoticgoodcomputing/flowthru)

## Mental model

Same EF Core mental model as the base extension — your `DbContext`, your entity types, your
keys — plus one PostgreSQL idea: `COPY` in binary format is Postgres's own bulk wire format, and
two tables mapped from the *same entity type* speak it to each other directly. The Item behaves
exactly like an `EFCoreTable` for ordinary reads, writes, and inspection; the COPY channel only
exists for `AddBulkTransfer`. Built on [Npgsql](https://www.npgsql.org/)'s raw binary COPY API
directly — no dependency on EFCore.BulkExtensions.

## Install

```bash
dotnet add package Flowthru.Extensions.EFCore.Npgsql
```

Declare both transfer endpoints as Npgsql tables, then write the transfer as intent:

```csharp
// In the Catalog — same shape as EFCoreTable, Npgsql provider required:
public IItem<IEnumerable<Order>> StagingOrders =>
    CreateItem(() => Item.Of<IEnumerable<Order>>("StagingOrders")
        .NpgsqlTable<Order, StagingDbContext>()
        .WithContextFactory(_stagingFactory)
        .Build());

public IItem<IEnumerable<Order>> ProductionOrders =>
    CreateItem(() => Item.Of<IEnumerable<Order>>("ProductionOrders")
        .NpgsqlTable<Order, ProductionDbContext>()
        .WithContextFactory(_productionFactory)
        .Build());

// In the Flow — pre-flight reports the selected rung in the plan output:
flow.AddBulkTransfer(catalog.StagingOrders, catalog.ProductionOrders);
```

The run output names the decision — a downgrade is never silent:

```
⇄ BulkTransfer_StagingOrders_to_ProductionOrders transfer rung: Native
  — native rung selected — capability pair matched (postgresql/pgcopy-binary)
```

Pair an Npgsql table with anything else (a JSON file, a CSV, a non-Postgres database) and the
same `AddBulkTransfer` visibly falls back to the streaming rung — the Npgsql item can both
stream its rows out and receive rows through a transactional batch sink. Set
`new BulkTransferOptions { RequireNative = true }` to turn an unavailable native path into a
pre-flight error instead.

## Load semantics: Replace by default, explicitly

Raw `COPY FROM` appends by nature. Flowthru makes the choice explicit instead of inheriting
that accident: the default import mode is **Replace** — a transactional `TRUNCATE` followed by
the load, so the target ends up an exact copy of the source. This matches the motivating use
case (cross-database promotion) and the base EF Core item's default save semantics.

```csharp
.NpgsqlTable<Order, ProductionDbContext>()
.WithContextFactory(_factory)
.WithImportMode(NpgsqlBulkImportMode.Append) // keep existing rows instead
.Build()
```

Both modes run inside a single transaction with the load itself — including the `TRUNCATE`.
**A failed transfer rolls the target back to exactly its prior state**; there is no torn,
half-loaded, or emptied table. The mode applies identically to the native and streaming rungs.

`Replace` notes: `TRUNCATE` requires the `TRUNCATE` privilege and fails when other tables hold
foreign keys into the target. `Append` notes: key collisions fail (and roll back) the transfer —
a raw byte passthrough has no upsert.

## Pairing requirements

The COPY statements name their columns explicitly, resolved from the EF model (physical column
names, model property order) — never guessed from CLR member names. Both endpoints must
therefore map the entity to the same column set with the same PostgreSQL types; store-computed
columns are excluded automatically. A mismatch fails the transfer at runtime — and rolls back —
rather than corrupting data. Ordinary `Save` writes are unaffected by the import mode; they
keep the base adapter's semantics (default replace, or your `WithSave(...)` delegate).

`.NpgsqlTable` requires a context factory (`WithContextFactory`) — each transfer channel opens
a dedicated connection for its duration. A non-Npgsql provider fails at `Build()` with the
provider named: only PostgreSQL can honour the `postgresql/pgcopy-binary` pairing the item
claims.
