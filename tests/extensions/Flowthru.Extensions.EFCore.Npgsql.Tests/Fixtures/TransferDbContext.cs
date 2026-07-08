using Flowthru.Data.Schema;
using Microsoft.EntityFrameworkCore;

namespace Flowthru.Extensions.EFCore.Npgsql.Tests.Fixtures;

/// <summary>
/// Flat entity moved by the transfer tests. <c>Amount</c> maps to a
/// custom column name so the COPY column resolution provably reads the
/// EF model rather than guessing from CLR member names. Column types are
/// deliberately timezone-free (int/text/double/bool) to keep the raw
/// binary COPY payload independent of Npgsql timestamp mapping rules.
/// </summary>
[FlowthruSchema]
public partial record TransferRecord
{
  public required int Id { get; init; }
  public required string Name { get; init; }
  public required double Amount { get; init; }
  public required bool Active { get; init; }
}

/// <summary>
/// The context both transfer endpoints use — one CLR model, two physical
/// databases — mirroring the cross-database promotion use case (#127).
/// </summary>
public sealed class TransferDbContext : DbContext
{
  public TransferDbContext(DbContextOptions<TransferDbContext> options) : base(options) { }

  public DbSet<TransferRecord> Records => Set<TransferRecord>();

  protected override void OnModelCreating(ModelBuilder modelBuilder)
  {
    modelBuilder.Entity<TransferRecord>(e =>
    {
      e.HasKey(r => r.Id);
      e.ToTable("transfer_records");
      e.Property(r => r.Amount).HasColumnName("amount_value");
    });
  }
}

/// <summary>
/// A context that maps <see cref="TransferRecord"/> into an explicit
/// PostgreSQL schema — exercises schema-qualified COPY statements.
/// </summary>
public sealed class SchemaQualifiedDbContext : DbContext
{
  public SchemaQualifiedDbContext(DbContextOptions<SchemaQualifiedDbContext> options) : base(options) { }

  public DbSet<TransferRecord> Records => Set<TransferRecord>();

  protected override void OnModelCreating(ModelBuilder modelBuilder)
  {
    modelBuilder.Entity<TransferRecord>(e =>
    {
      e.HasKey(r => r.Id);
      e.ToTable("transfer_records", "analytics");
      e.Property(r => r.Amount).HasColumnName("amount_value");
    });
  }
}

/// <summary>
/// Entity whose table carries an extra required column relative to
/// <see cref="TransferRecord"/> — a deliberately incompatible COPY
/// pairing used to prove a failed transfer rolls the target back.
/// </summary>
[FlowthruSchema]
public partial record WideTransferRecord
{
  public required int Id { get; init; }
  public required string Name { get; init; }
  public required double Amount { get; init; }
  public required bool Active { get; init; }
  public required string Extra { get; init; }
}

/// <summary>
/// Maps <see cref="WideTransferRecord"/> onto the same physical table
/// name as <see cref="TransferRecord"/>, with one extra column — the
/// export payload then carries fewer columns than the import statement
/// expects, and PostgreSQL rejects the COPY mid-transaction.
/// </summary>
public sealed class WideTransferDbContext : DbContext
{
  public WideTransferDbContext(DbContextOptions<WideTransferDbContext> options) : base(options) { }

  public DbSet<WideTransferRecord> Records => Set<WideTransferRecord>();

  protected override void OnModelCreating(ModelBuilder modelBuilder)
  {
    modelBuilder.Entity<WideTransferRecord>(e =>
    {
      e.HasKey(r => r.Id);
      e.ToTable("transfer_records");
      e.Property(r => r.Amount).HasColumnName("amount_value");
    });
  }
}

/// <summary>
/// SQLite twin of <see cref="TransferDbContext"/> — the negative case
/// for provider feature detection.
/// </summary>
public sealed class SqliteTransferDbContext : DbContext
{
  public SqliteTransferDbContext(DbContextOptions<SqliteTransferDbContext> options) : base(options) { }

  public DbSet<TransferRecord> Records => Set<TransferRecord>();

  protected override void OnModelCreating(ModelBuilder modelBuilder)
  {
    modelBuilder.Entity<TransferRecord>().HasKey(r => r.Id);
  }
}
