using Flowthru.Data.Catalog;
using Microsoft.EntityFrameworkCore;

namespace StreamingBulkLoad.Data;

/// <summary>
/// Catalog for the StreamingBulkLoad example. Binds the one raw Parquet dataset,
/// the SQLite table both variants load into, and the self-measurement artefacts
/// (the memory-sample CSV, its comparison summary, and the Markdown report).
/// </summary>
/// <remarks>
/// The catalog owns the EF Core <see cref="DbContextOptions{TContext}"/> so every
/// context — the eager item's, the streaming sink's, and the harness's
/// clear/count contexts — targets the same SQLite file. It exposes
/// <see cref="NewContext"/> for the factory-per-operation pattern rather than a
/// shared instance.
/// </remarks>
public partial class Catalog : CatalogAbstract
{
  /// <summary>
  /// Rows per Parquet row group on write. Deliberately small so a modest dataset
  /// still spans many row groups — the streaming reader yields one group at a
  /// time, so this is the knob that makes streaming's peak O(row group) rather
  /// than O(file). Production defaults are 1,000,000.
  /// </summary>
  public const int WriteRowGroupSize = 10_000;

  /// <summary>Rows per bulk-insert batch — the write-side batch for both variants.</summary>
  public const int BulkBatchSize = 4_000;

  private readonly string _basePath;
  private readonly DbContextOptions<TransactionsDbContext> _dbOptions;

  public Catalog(string basePath, string dbPath)
  {
    _basePath = basePath ?? throw new ArgumentNullException(nameof(basePath));
    _dbOptions = new DbContextOptionsBuilder<TransactionsDbContext>()
      .UseSqlite($"Data Source={dbPath}")
      .Options;
  }

  /// <summary>A fresh context over the shared SQLite file — one per Load/Save/clear operation.</summary>
  public TransactionsDbContext NewContext() => new(_dbOptions);
}
