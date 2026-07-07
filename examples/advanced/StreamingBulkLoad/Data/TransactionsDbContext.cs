using Microsoft.EntityFrameworkCore;
using StreamingBulkLoad.Data._01_Raw.Schemas;

namespace StreamingBulkLoad.Data;

/// <summary>
/// SQLite-backed EF Core context holding the single <c>Transactions</c> table
/// that both ingest variants load into. SQLite means the example runs with no
/// external database — <c>EFCore.BulkExtensions</c> supports it — and the file
/// is inspectable between runs.
/// </summary>
/// <remarks>
/// The entity is <see cref="TransactionRecord"/>, the very same record used as
/// the Parquet row schema. The key is assigned by the generator (dense 0..N-1)
/// so it is value-generated-never: bulk insert writes the ids verbatim.
/// </remarks>
public class TransactionsDbContext : DbContext
{
  public TransactionsDbContext(DbContextOptions<TransactionsDbContext> options)
    : base(options) { }

  public DbSet<TransactionRecord> Transactions => Set<TransactionRecord>();

  protected override void OnModelCreating(ModelBuilder modelBuilder)
  {
    modelBuilder.Entity<TransactionRecord>(entity =>
    {
      entity.ToTable("Transactions");
      entity.HasKey(e => e.Id);
      entity.Property(e => e.Id).ValueGeneratedNever();
    });
  }
}
