using Flowthru.Data.Catalog;
using Flowthru.Data.Storage.EFCore.Npgsql;
using Flowthru.Extensions.EFCore.Npgsql.Tests.Fixtures;
using Flowthru.Flow;
using Flowthru.Prelude;
using Flowthru.Validation.PreFlight;
using Microsoft.EntityFrameworkCore;

namespace Flowthru.Extensions.EFCore.Npgsql.Tests;

/// <summary>
/// Offline tests (#139) — everything here is zero-I/O by design:
/// provider feature detection, the pairing identity metadata, COPY
/// statement resolution from the EF model, and pre-flight rung
/// negotiation against real (never-connected) Npgsql-backed items.
/// Negotiation and adapter construction read model metadata only, so
/// these run on any machine, Docker or not.
/// </summary>
[TestFixture]
public class NpgsqlCopyAdapterConfigTests
{
  /// <summary>A connection string that is never opened — construction and negotiation are zero-I/O.</summary>
  private const string OfflineConnectionString =
    "Host=localhost;Port=1;Database=flowthru_offline;Username=nobody;Password=nothing";

  private static TransferDbContext CreatePgContext() =>
    new(new DbContextOptionsBuilder<TransferDbContext>()
      .UseNpgsql(OfflineConnectionString)
      .Options);

  private static NpgsqlCopyStorageAdapter<TransferRecord> CreatePgAdapter(
    NpgsqlBulkImportMode importMode = NpgsqlBulkImportMode.Replace
  ) =>
    new(CreatePgContext, importMode);

  // ===========================================================================
  // Provider feature detection
  // ===========================================================================

  [Test]
  public void Construction_OnNonNpgsqlProvider_FailsFast_NamingTheProvider()
  {
    var options = new DbContextOptionsBuilder<SqliteTransferDbContext>()
      .UseSqlite("Data Source=:memory:")
      .Options;

    var ex = Assert.Throws<InvalidOperationException>(() =>
      _ = new NpgsqlCopyStorageAdapter<TransferRecord>(
        () => new SqliteTransferDbContext(options)));

    Assert.That(ex!.Message, Does.Contain("Npgsql"),
      "The error must say what provider is required.");
    Assert.That(ex.Message, Does.Contain("Sqlite"),
      "The error must name the provider actually found.");
  }

  [Test]
  public void Construction_OnNpgsqlProvider_Succeeds_WithoutTouchingTheDatabase()
  {
    // The connection string points nowhere reachable; construction must
    // still succeed because feature detection and model resolution are
    // metadata-only.
    Assert.DoesNotThrow(() => CreatePgAdapter());
  }

  // ===========================================================================
  // Pairing identity — pure metadata
  // ===========================================================================

  [Test]
  public void CapabilityIdentity_IsPostgresqlPgcopyBinary_OnBothHalves()
  {
    var adapter = CreatePgAdapter();

    Assert.That(((Data.Storage.ISupportsBulkExport)adapter).BulkProvider, Is.EqualTo("postgresql"));
    Assert.That(((Data.Storage.ISupportsBulkExport)adapter).BulkWireFormat, Is.EqualTo("pgcopy-binary"));
    Assert.That(((Data.Storage.ISupportsBulkImport)adapter).BulkProvider, Is.EqualTo("postgresql"));
    Assert.That(((Data.Storage.ISupportsBulkImport)adapter).BulkWireFormat, Is.EqualTo("pgcopy-binary"));
  }

  // ===========================================================================
  // COPY statement resolution — from the EF model, never the CLR names
  // ===========================================================================

  [Test]
  public void CopyStatements_ResolveTableAndColumns_FromTheEFModel()
  {
    var adapter = CreatePgAdapter();
    var target = adapter.CopyTarget;

    Assert.That(target.QualifiedTable, Is.EqualTo("\"transfer_records\""),
      "The physical table name comes from ToTable(), not the CLR type name.");
    Assert.That(target.Columns, Does.Contain("amount_value"),
      "The renamed column must resolve to its physical name.");
    Assert.That(target.Columns, Does.Not.Contain("Amount"),
      "The CLR property name must never leak into the COPY statement.");
    Assert.That(target.ExportSql, Does.StartWith("COPY \"transfer_records\" ("));
    Assert.That(target.ExportSql, Does.EndWith(") TO STDOUT (FORMAT BINARY)"));
    Assert.That(target.ImportSql, Does.EndWith(") FROM STDIN (FORMAT BINARY)"));
    Assert.That(target.TruncateSql, Is.EqualTo("TRUNCATE TABLE \"transfer_records\""));
  }

  [Test]
  public void CopyStatements_QualifySchema_WhenTheEntityMapsToOne()
  {
    var options = new DbContextOptionsBuilder<SchemaQualifiedDbContext>()
      .UseNpgsql(OfflineConnectionString)
      .Options;
    var adapter = new NpgsqlCopyStorageAdapter<TransferRecord>(
      () => new SchemaQualifiedDbContext(options));

    Assert.That(adapter.CopyTarget.QualifiedTable,
      Is.EqualTo("\"analytics\".\"transfer_records\""));
    Assert.That(adapter.CopyTarget.ExportSql,
      Does.StartWith("COPY \"analytics\".\"transfer_records\" ("));
  }

  [Test]
  public void CopyStatements_UseTheSameColumnOrder_AcrossTwoContextsOfTheSameModel()
  {
    // The pairing relies on both endpoints deriving the same column list
    // from the same entity mapping — two independently built contexts
    // must agree exactly.
    var first = CreatePgAdapter().CopyTarget;
    var second = CreatePgAdapter().CopyTarget;

    Assert.That(first.Columns, Is.EqualTo(second.Columns).AsCollection);
    Assert.That(first.ImportSql, Is.EqualTo(second.ImportSql));
  }

  // ===========================================================================
  // Pre-flight negotiation with real (never-connected) adapters
  // ===========================================================================

  [Test]
  public void Negotiate_PgToPg_SelectsNativeRung_NamingThePairing()
  {
    var source = BuildPgItem("staging_records");
    var target = BuildPgItem("production_records");

    var negotiation = BulkTransferNegotiation.Negotiate(source, target);

    Assert.That(negotiation.IsValid, Is.True);
    var decision = ((Validated<PreFlightError, BulkTransferDecision>.Valid)negotiation).Value;
    Assert.That(decision.Rung, Is.EqualTo(BulkTransferRung.Native));
    Assert.That(decision.Reason, Does.Contain("postgresql/pgcopy-binary"),
      "The plan-visible reason must name the matched pairing.");
  }

  [Test]
  public void Negotiate_PgToPg_RequireNative_Passes()
  {
    var negotiation = BulkTransferNegotiation.Negotiate(
      BuildPgItem("staging_records"),
      BuildPgItem("production_records"),
      new BulkTransferOptions { RequireNative = true }
    );

    Assert.That(negotiation.IsValid, Is.True,
      "RequireNative must pass for a homogeneous Postgres pair.");
  }

  [Test]
  public void Negotiate_HeterogeneousPair_FallsBackToStreaming_Visibly()
  {
    var jsonSource = ItemFactory.Enumerable.Json<TransferRecord>(
      "orders_json", Path.Combine(Path.GetTempPath(), "flowthru-npgsql-nonexistent.json"));
    var pgTarget = BuildPgItem("production_records");

    var negotiation = BulkTransferNegotiation.Negotiate(jsonSource, pgTarget);

    Assert.That(negotiation.IsValid, Is.True,
      "A JSON→Postgres pairing must be streamable: the Npgsql item is sink-capable.");
    var decision = ((Validated<PreFlightError, BulkTransferDecision>.Valid)negotiation).Value;
    Assert.That(decision.Rung, Is.EqualTo(BulkTransferRung.Streaming));
    Assert.That(decision.Reason, Does.Contain("no native capability pair"),
      "The fallback must say why native was unavailable — never silent.");
  }

  [Test]
  public void Negotiate_HeterogeneousPair_RequireNative_FailsPreFlight()
  {
    var jsonSource = ItemFactory.Enumerable.Json<TransferRecord>(
      "orders_json", Path.Combine(Path.GetTempPath(), "flowthru-npgsql-nonexistent.json"));
    var pgTarget = BuildPgItem("production_records");

    var negotiation = BulkTransferNegotiation.Negotiate(
      jsonSource, pgTarget, new BulkTransferOptions { RequireNative = true });

    Assert.That(negotiation.IsValid, Is.False,
      "RequireNative must fail for a heterogeneous pairing.");
    var errors = ((Validated<PreFlightError, BulkTransferDecision>.Invalid)negotiation).Errors;
    Assert.That(errors.Single(), Is.InstanceOf<PreFlightError.BulkTransferRungUnavailable>());
  }

  // ===========================================================================
  // Builder surface
  // ===========================================================================

  [Test]
  public void Builder_WithoutContextFactory_FailsFast()
  {
    var ex = Assert.Throws<InvalidOperationException>(() =>
      Item.Of<IEnumerable<TransferRecord>>("records")
        .NpgsqlTable<TransferRecord, TransferDbContext>()
        .Build());

    Assert.That(ex!.Message, Does.Contain("WithContextFactory"));
  }

  private static IItem<IEnumerable<TransferRecord>> BuildPgItem(string label) =>
    Item.Of<IEnumerable<TransferRecord>>(label)
      .NpgsqlTable<TransferRecord, TransferDbContext>()
      .WithContextFactory(CreatePgContext)
      .Build();
}
