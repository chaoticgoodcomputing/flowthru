using Flowthru.Data.Catalog;
using Flowthru.Extensions.EFCore.Bulk;
using Flowthru.Prelude;
using Microsoft.EntityFrameworkCore;
using StreamingBulkLoad.Data._01_Raw.Schemas;

namespace StreamingBulkLoad.Data;

public partial class Catalog
{
  /// <summary>
  /// The SQLite <c>Transactions</c> table as an <em>eager</em> bulk-write target.
  /// Its Save <see cref="Enumerable.ToList{TSource}"/>s the whole dataset — the
  /// very buffering the streaming path avoids — so peak memory is O(file), then
  /// hands the materialised list to the shipped <see cref="BulkSave.Insert{T, TContext}"/>
  /// helper inside a single transaction (so it commits once, the same way the
  /// streaming sink does — the variants differ in <em>memory grain</em>, not in
  /// how they talk to SQLite).
  /// </summary>
  public IItem<IEnumerable<TransactionRecord>> EagerTransactionsTable =>
    CreateItem(() =>
    {
      var insert = BulkSave.Insert<TransactionRecord, TransactionsDbContext>(
        new BulkSaveOptions { BatchSize = BulkBatchSize });

      return ItemFactory.Enumerable.EFCore<TransactionRecord, TransactionsDbContext>(
        "EagerTransactionsTable",
        contextFactory: NewContext,
        saveFunc: async (db, data, ct) =>
        {
          // Materialise the whole file into memory (the eager cost), then one
          // transactional bulk insert.
          var all = data as IList<TransactionRecord> ?? data.ToList();
          await using var transaction = await db.Database.BeginTransactionAsync(ct);
          await insert(db, all, ct);
          await transaction.CommitAsync(ct);
        });
    });

  /// <summary>
  /// A fresh streaming bulk-insert sink over the same <c>Transactions</c> table.
  /// Opens one transaction, writes one <see cref="BulkBatchSize"/>-row batch per
  /// arriving chunk, commits on success (rolls back on mid-stream failure) — so
  /// the write is O(batch). The streaming Flow's <c>AddBulkLoad</c> drives it.
  /// A fresh sink per call because a sink is single-use (it owns a transaction).
  /// </summary>
  public IFlowSink<TransactionRecord> NewTransactionSink() =>
    BulkSink.Insert<TransactionRecord, TransactionsDbContext>(
      NewContext,
      new BulkSaveOptions { BatchSize = BulkBatchSize });
}
