---
name: flowthru-efcore-npgsql
description: Deep skill for the Flowthru EFCore.Npgsql sub-package — move PostgreSQL-to-PostgreSQL data as raw bytes via binary COPY instead of marshalling rows through .NET. Use when both ends of an AddBulkTransfer are PostgreSQL tables mapped from the same entity type and you want the native byte-copy rung. Pairs with the umbrella `flowthru` skill.
metadata:
  flowthru:
    extension: Flowthru.Extensions.EFCore.Npgsql
    surface: database
    capability: Declare a table Item with .NpgsqlTable and AddBulkTransfer promotes Postgres-to-Postgres as a binary COPY passthrough — no row materialised in .NET.
    register: .NpgsqlTable(…) + flow.AddBulkTransfer(…)
---

# flowthru-efcore-npgsql

Sub-package that builds on `Flowthru.Extensions.EFCore` — pull `flowthru-efcore` first for the base model (`.EFCoreTable`, `DbContext`, context factory). Parent shard: `--skill flowthru-efcore` ([SKILL.md](https://github.com/chaoticgoodcomputing/flowthru/blob/main/src/extensions/Flowthru.Extensions.EFCore/SKILL.md)).

## What it adds over base EFCore

One PostgreSQL idea: `COPY` in binary format is Postgres's own bulk wire format, and two tables mapped from the *same* entity type can speak it directly. Declare a table with `.NpgsqlTable<TRow, TContext>()` instead of `.EFCoreTable<TRow, TContext>()`. When *both* ends of an `AddBulkTransfer(...)` are Npgsql tables, pre-flight negotiation picks the **native rung** — the source's `COPY ... TO STDOUT (FORMAT BINARY)` pumped straight into the target's `COPY ... FROM STDIN (FORMAT BINARY)`. No row is ever materialised in .NET (measured ~20× faster than the streaming rung on a 200k-row table). For ordinary reads, writes, and inspection the item behaves exactly like an `EFCoreTable`; the COPY channel exists only for `AddBulkTransfer`. Built directly on [Npgsql](https://www.npgsql.org/)'s raw binary COPY API — no EFCore.BulkExtensions dependency.

## Register

```bash
dotnet add package Flowthru.Extensions.EFCore.Npgsql
```

Requires the Npgsql EF Core provider (`UseNpgsql`) on each `DbContext` factory — the extension fails at `Build()` on any non-Postgres provider, naming it:

<!-- flowthru:snippet:docs:register-efcore-npgsql:start -->
```csharp
services.AddDbContextFactory<StagingDbContext>(options =>
  options.UseNpgsql(connectionString)
);
```
<!-- flowthru:snippet:docs:register-efcore-npgsql:end -->

_(real source: [Program.cs](https://github.com/chaoticgoodcomputing/flowthru/blob/main/examples/advanced/SpaceflightsStagingSchema/Program.cs))_

Declare both transfer endpoints as Npgsql tables (each needs `WithContextFactory` — every channel opens a dedicated connection), then write the transfer as intent:

```csharp
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

## Gotchas

- **Native rung needs a matched pair.** Both endpoints must be `.NpgsqlTable`, mapping the same entity to the same column set with the same PostgreSQL types (columns resolved from the EF model — physical names, model order — never guessed from CLR members). Pair an Npgsql table with anything else (JSON, CSV, a non-Postgres DB) and `AddBulkTransfer` visibly falls back to the streaming rung. A column mismatch fails at runtime and rolls back. Set `new BulkTransferOptions { RequireNative = true }` to turn an unavailable native path into a pre-flight error.
- **Replace by default, transactionally.** Default import mode is **Replace** — a `TRUNCATE` then load, all in one transaction; a failed transfer rolls the target back to its exact prior state (no torn/half-loaded table). `TRUNCATE` needs the privilege and fails if other tables hold FKs into the target. Use `.WithImportMode(NpgsqlBulkImportMode.Append)` to keep existing rows — but raw byte passthrough has no upsert, so key collisions fail and roll back.
- **Downgrades are never silent** — the run output names the rung (`transfer rung: Native` / capability-pair line).
- **Bulk *saves* are a different package.** For high-throughput saves of in-.NET rows to any provider (not Postgres-to-Postgres transfers), reach for `flowthru-efcore-bulk` and `.WithSave(BulkSave.*)` instead.
