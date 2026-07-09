using Flowthru.Data.Catalog;
using Flowthru.Data.Storage;
using Flowthru.Data.Storage.EFCore.Npgsql;
using Flowthru.Extensions.EFCore.Npgsql.Tests.Fixtures;
using Flowthru.Flow;
using Flowthru.Prelude;
using Flowthru.Tests.Kits.Prelude;
using Flowthru.Validation.PreFlight;
using Flowthru.Validation.Runtime;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Testcontainers.PostgreSql;

namespace Flowthru.Extensions.EFCore.Npgsql.Tests;

/// <summary>
/// Postgres-backed integration tier for the raw binary COPY rung (#139),
/// self-provisioning one PostgreSQL container per fixture (gated on
/// <see cref="TestCapabilities.Docker"/> — Inconclusive without Docker).
/// Alternatively, point <c>FLOWTHRU_PG_TEST_CONNSTRING</c> at an existing
/// PostgreSQL server (superuser or CREATEDB role) to run the tier without
/// Docker — the external-server escape hatch the S3 gateway laws
/// established with <c>FLOWTHRU_S3_TEST_*</c>. Each test creates fresh
/// databases on the server, mirroring the cross-database promotion use
/// case (#127): both transfer rungs are exercised through the same
/// <c>AddBulkTransfer</c> wiring, and every failure case asserts the
/// target rolled back to its exact prior state.
/// </summary>
[TestFixture]
[Category("RequiresDocker")]
public class NpgsqlRawCopyTransferTests
{
  internal const string ExternalServerVariable = "FLOWTHRU_PG_TEST_CONNSTRING";

  private PostgreSqlContainer? _container;
  private string _adminConnectionString = null!;

  [OneTimeSetUp]
  public async Task StartContainer()
  {
    var external = Environment.GetEnvironmentVariable(ExternalServerVariable);
    if (!string.IsNullOrWhiteSpace(external))
    {
      _adminConnectionString = external;
      return;
    }

    Assume.That(TestCapabilities.Docker.IsAvailable(),
      $"[{TestCapabilities.Docker.Name}] {TestCapabilities.Docker.MissingMessage} "
      + $"(or point {ExternalServerVariable} at an existing PostgreSQL server)");
    _container = new PostgreSqlBuilder().WithImage("postgres:16-alpine").Build();
    await _container.StartAsync();
    _adminConnectionString = _container.GetConnectionString();
  }

  [OneTimeTearDown]
  public async Task StopContainer()
  {
    if (_container is not null) await _container.DisposeAsync();
  }

  // ===========================================================================
  // Native rung — end to end
  // ===========================================================================

  [Test]
  public async Task NativeRung_ReplacesTargetWithSourceRows_EndToEnd()
  {
    var sourceOptions = await CreateDatabaseAsync<TransferDbContext>();
    var targetOptions = await CreateDatabaseAsync<TransferDbContext>();
    await SeedAsync(sourceOptions, MakeRows(1, 500));
    await SeedAsync(targetOptions, MakeRows(9_000, 3)); // stale rows Replace must remove

    var source = PgItem("staging_records", sourceOptions);
    var target = PgItem("production_records", targetOptions);

    // Pre-flight visibility: the pairing negotiates the native rung.
    var negotiation = BulkTransferNegotiation.Negotiate(source, target);
    var decision = ((Validated<PreFlightError, BulkTransferDecision>.Valid)negotiation).Value;
    Assert.That(decision.Rung, Is.EqualTo(BulkTransferRung.Native));
    Assert.That(decision.Reason, Does.Contain("postgresql/pgcopy-binary"));

    var flow = FlowBuilder.CreateFlow("PromoteRecords", p => p.AddBulkTransfer(source, target));
    var result = await flow.RunAsync();

    Assert.That(result.IsSuccess, Is.True, FailureReport(result));
    var landed = await ReadAllAsync(targetOptions);
    Assert.That(landed, Has.Count.EqualTo(500),
      "Replace semantics: the stale target rows must be gone; only source rows remain.");
    Assert.That(landed.Select(r => r.Id), Is.EqualTo(Enumerable.Range(1, 500)),
      "Every source row must arrive, byte-identical through the binary COPY.");
    Assert.That(landed[0].Name, Is.EqualTo("name_1"));
    Assert.That(landed[0].Amount, Is.EqualTo(1.5));
  }

  [Test]
  public async Task NativeRung_AppendMode_KeepsExistingTargetRows()
  {
    var sourceOptions = await CreateDatabaseAsync<TransferDbContext>();
    var targetOptions = await CreateDatabaseAsync<TransferDbContext>();
    await SeedAsync(sourceOptions, MakeRows(1, 100));
    await SeedAsync(targetOptions, MakeRows(9_000, 10));

    var source = PgItem("staging_records", sourceOptions, NpgsqlBulkImportMode.Append);
    var target = PgItem("production_records", targetOptions, NpgsqlBulkImportMode.Append);

    var flow = FlowBuilder.CreateFlow("AppendRecords", p => p.AddBulkTransfer(source, target));
    var result = await flow.RunAsync();

    Assert.That(result.IsSuccess, Is.True, FailureReport(result));
    var landed = await ReadAllAsync(targetOptions);
    Assert.That(landed, Has.Count.EqualTo(110),
      "Append semantics: pre-existing rows stay, incoming rows land beside them.");
  }

  [Test]
  public async Task NativeRung_RequireNative_PassesAndRunsNatively()
  {
    var sourceOptions = await CreateDatabaseAsync<TransferDbContext>();
    var targetOptions = await CreateDatabaseAsync<TransferDbContext>();
    await SeedAsync(sourceOptions, MakeRows(1, 50));

    var source = PgItem("staging_records", sourceOptions);
    var target = PgItem("production_records", targetOptions);

    var flow = FlowBuilder.CreateFlow("RequireNativePromotion", p =>
      p.AddBulkTransfer(source, target, new BulkTransferOptions { RequireNative = true }));
    var result = await flow.RunAsync();

    Assert.That(result.IsSuccess, Is.True, FailureReport(result));
    Assert.That((await ReadAllAsync(targetOptions)).Count, Is.EqualTo(50));
  }

  // ===========================================================================
  // Native rung — rollback discipline (no torn partial table)
  // ===========================================================================

  [Test]
  public async Task NativeRung_IncompatibleTargetShape_RollsBackTheTruncate()
  {
    // The target maps one extra required column onto the same table, so
    // the binary payload's column count mismatches the import statement
    // and PostgreSQL rejects the COPY — after Replace already truncated
    // inside the same transaction. The pre-existing rows must survive.
    var sourceOptions = await CreateDatabaseAsync<TransferDbContext>();
    var targetOptions = await CreateDatabaseAsync<WideTransferDbContext>();
    await SeedAsync(sourceOptions, MakeRows(1, 100));
    await SeedWideAsync(targetOptions, 5);

    var source = PgItem("staging_records", sourceOptions);
    var target = Item.Of<IEnumerable<WideTransferRecord>>("production_records")
      .NpgsqlTable<WideTransferRecord, WideTransferDbContext>()
      .WithContextFactory(() => new WideTransferDbContext(targetOptions))
      .Build();

    // Same T is required by AddBulkTransfer's signature; drive the rung
    // through the capability channels directly — exactly what the target
    // endpoint's Save executes — to pair the incompatible shapes.
    var export = ((Item<IEnumerable<TransferRecord>>)source).Storage as ISupportsBulkExport;
    var import = ((Item<IEnumerable<WideTransferRecord>>)target).Storage as ISupportsBulkImport;
    var outcome = await Pump(export!, import!);

    Assert.That(outcome, Is.InstanceOf<EffResult<FlowUnit>.Failure>(),
      "A column-shape mismatch must fail the transfer.");
    await using var check = new WideTransferDbContext(targetOptions);
    var survivors = await check.Records.OrderBy(r => r.Id).ToListAsync();
    Assert.That(survivors, Has.Count.EqualTo(5),
      "The failed transfer must roll back the TRUNCATE — no torn or emptied table.");
  }

  [Test]
  public async Task NativeRung_AppendModePkCollision_RollsBackEverything()
  {
    var sourceOptions = await CreateDatabaseAsync<TransferDbContext>();
    var targetOptions = await CreateDatabaseAsync<TransferDbContext>();
    await SeedAsync(sourceOptions, MakeRows(1, 50));
    await SeedAsync(targetOptions, MakeRows(1, 1)); // Id=1 collides with the incoming payload

    var source = PgItem("staging_records", sourceOptions, NpgsqlBulkImportMode.Append);
    var target = PgItem("production_records", targetOptions, NpgsqlBulkImportMode.Append);

    var flow = FlowBuilder.CreateFlow("CollidingAppend", p => p.AddBulkTransfer(source, target));
    var result = await flow.RunAsync();

    Assert.That(result.HasFailures, Is.True,
      "A primary-key collision mid-COPY must fail the transfer.");
    var landed = await ReadAllAsync(targetOptions);
    Assert.That(landed, Has.Count.EqualTo(1),
      "Rollback must leave the target exactly as it was — no partial batch.");
    Assert.That(landed[0].Id, Is.EqualTo(1));
  }

  // ===========================================================================
  // Streaming rung — heterogeneous pairs through the same wiring
  // ===========================================================================

  [Test]
  public async Task StreamingRung_JsonToPg_LandsRows_WithReplaceSemantics()
  {
    var targetOptions = await CreateDatabaseAsync<TransferDbContext>();
    await SeedAsync(targetOptions, MakeRows(9_000, 4)); // must vanish under Replace

    var jsonPath = Path.Combine(Path.GetTempPath(), $"flowthru-npgsql-{Guid.NewGuid():N}.json");
    var jsonSource = ItemFactory.Enumerable.Json<TransferRecord>("orders_json", jsonPath);
    await jsonSource.Save(MakeRows(1, 200)).Run();
    var target = PgItem("production_records", targetOptions);

    try
    {
      var negotiation = BulkTransferNegotiation.Negotiate(jsonSource, target);
      var decision = ((Validated<PreFlightError, BulkTransferDecision>.Valid)negotiation).Value;
      Assert.That(decision.Rung, Is.EqualTo(BulkTransferRung.Streaming),
        "A heterogeneous pairing must select the streaming fallback.");
      Assert.That(decision.Reason, Does.Contain("no native capability pair"));

      var flow = FlowBuilder.CreateFlow("JsonToPg", p => p.AddBulkTransfer(jsonSource, target));
      var result = await flow.RunAsync();

      Assert.That(result.IsSuccess, Is.True, FailureReport(result));
      var landed = await ReadAllAsync(targetOptions);
      Assert.That(landed, Has.Count.EqualTo(200),
        "Replace semantics must hold on the streaming rung too.");
      Assert.That(landed.Select(r => r.Id), Is.EqualTo(Enumerable.Range(1, 200)));
    }
    finally
    {
      File.Delete(jsonPath);
    }
  }

  [Test]
  public async Task StreamingRung_PgSource_StreamsRowsIntoASinkTarget()
  {
    var sourceOptions = await CreateDatabaseAsync<TransferDbContext>();
    await SeedAsync(sourceOptions, MakeRows(1, 150));

    var source = PgItem("staging_records", sourceOptions);
    var target = new RecordingSinkItem("collector");

    var negotiation = BulkTransferNegotiation.Negotiate(source, target);
    var decision = ((Validated<PreFlightError, BulkTransferDecision>.Valid)negotiation).Value;
    Assert.That(decision.Rung, Is.EqualTo(BulkTransferRung.Streaming));

    var flow = FlowBuilder.CreateFlow("PgToSink", p => p.AddBulkTransfer(source, target));
    var result = await flow.RunAsync();

    Assert.That(result.IsSuccess, Is.True, FailureReport(result));
    Assert.That(target.Rows.Select(r => r.Id), Is.EquivalentTo(Enumerable.Range(1, 150)),
      "The Npgsql item must stream all rows out through its streaming view.");
  }

  // ===========================================================================
  // Helpers
  // ===========================================================================

  internal static IReadOnlyList<TransferRecord> MakeRows(int firstId, int count) =>
    Enumerable.Range(firstId, count)
      .Select(i => new TransferRecord
      {
        Id = i,
        Name = $"name_{i}",
        Amount = i * 1.5,
        Active = i % 2 == 0,
      })
      .ToList();

  private async Task<DbContextOptions<TContext>> CreateDatabaseAsync<TContext>()
    where TContext : DbContext
  {
    var dbName = $"transfer_{Guid.NewGuid():N}";
    var admin = new NpgsqlConnection(_adminConnectionString);
    await using (admin.ConfigureAwait(false))
    {
      await admin.OpenAsync();
      var create = new NpgsqlCommand($"CREATE DATABASE \"{dbName}\"", admin);
      await using (create.ConfigureAwait(false))
      {
        await create.ExecuteNonQueryAsync();
      }
    }

    var builder = new NpgsqlConnectionStringBuilder(_adminConnectionString)
    {
      Database = dbName,
    };
    var options = new DbContextOptionsBuilder<TContext>()
      .UseNpgsql(builder.ConnectionString)
      .Options;

    await using var context = (TContext)Activator.CreateInstance(typeof(TContext), options)!;
    await context.Database.EnsureCreatedAsync();
    return options;
  }

  private static async Task SeedAsync(
    DbContextOptions<TransferDbContext> options,
    IReadOnlyList<TransferRecord> rows
  )
  {
    await using var context = new TransferDbContext(options);
    context.Records.AddRange(rows);
    await context.SaveChangesAsync();
  }

  private static async Task SeedWideAsync(DbContextOptions<WideTransferDbContext> options, int count)
  {
    await using var context = new WideTransferDbContext(options);
    context.Records.AddRange(Enumerable.Range(1, count).Select(i => new WideTransferRecord
    {
      Id = i,
      Name = $"wide_{i}",
      Amount = i,
      Active = false,
      Extra = "keep-me",
    }));
    await context.SaveChangesAsync();
  }

  private static async Task<List<TransferRecord>> ReadAllAsync(
    DbContextOptions<TransferDbContext> options
  )
  {
    await using var context = new TransferDbContext(options);
    return await context.Records.AsNoTracking().OrderBy(r => r.Id).ToListAsync();
  }

  private static IItem<IEnumerable<TransferRecord>> PgItem(
    string label,
    DbContextOptions<TransferDbContext> options,
    NpgsqlBulkImportMode mode = NpgsqlBulkImportMode.Replace
  ) =>
    Item.Of<IEnumerable<TransferRecord>>(label)
      .NpgsqlTable<TransferRecord, TransferDbContext>()
      .WithContextFactory(() => new TransferDbContext(options))
      .WithImportMode(mode)
      .Build();

  private static async Task<EffResult<FlowUnit>> Pump(
    ISupportsBulkExport export,
    ISupportsBulkImport import
  ) =>
    await export.OpenBulkExport().Bind(exportStream =>
      FlowIO.LiftAsync(async ct =>
      {
        BulkImportChannel? channel = null;
        try
        {
          var opened = await import.OpenBulkImport().Run(ct);
          channel = ((EffResult<BulkImportChannel>.Success)opened).Value;
          await exportStream.CopyToAsync(channel, ct);
          await channel.FlushAsync(ct);
          await channel.CompleteAsync(ct);
          return FlowUnit.Default;
        }
        finally
        {
          if (channel is not null) await channel.DisposeAsync();
          await exportStream.DisposeAsync();
        }
      }, source: "test-pump")).Run();

  private static string FailureReport(FlowResult result) =>
    "Failures: " + string.Join(" | ",
      result.StepResults.OfType<StepResult.Failed>().Select(f => $"{f.StepLabel}: {f.Error.Message}"));

  /// <summary>
  /// A sink-capable, non-Postgres target double — the heterogeneous
  /// pairing that proves the Npgsql item's streaming view feeds the
  /// streaming rung.
  /// </summary>
  private sealed class RecordingSinkItem
    : IItem<IEnumerable<TransferRecord>>, ISupportsStreamingSink<TransferRecord>
  {
    private readonly Sink _sink = new();

    public RecordingSinkItem(string label) => Label = label;

    public string Label { get; }
    public NodeTraits Traits => new();
    public IReadOnlyList<TransferRecord> Rows => _sink.Rows;

    public IFlowSink<TransferRecord> OpenStreamingSink() => _sink;

    public FlowIO<IEnumerable<TransferRecord>> Load() =>
      FlowIO.Pure<IEnumerable<TransferRecord>>(_sink.Rows);
    public FlowIO<FlowUnit> Save(IEnumerable<TransferRecord> data) =>
      FlowIO.Fail<FlowUnit>(new RuntimeError.External(
        $"RecordingSinkItem[{Label}].Save", new InvalidOperationException("sink-only stub")));
    public FlowIO<bool> Exists() => FlowIO.Pure(false);
    public FlowIO<ValidationResult> InspectShallow(int sampleSize = 100) =>
      FlowIO.Pure(ValidationResult.Success());
    public FlowIO<ValidationResult> InspectDeep() => FlowIO.Pure(ValidationResult.Success());
    public FlowIO<ValidationResult> InspectTarget() => FlowIO.Pure(ValidationResult.Success());
    public FlowIO<ValidationResult> Validate() => FlowIO.Pure(ValidationResult.Success());

    private sealed class Sink : IFlowSink<TransferRecord>
    {
      public List<TransferRecord> Rows { get; } = new();
      public int BatchSize => 64;
      public ValueTask OpenAsync(CancellationToken cancellationToken) => ValueTask.CompletedTask;
      public ValueTask WriteBatchAsync(
        IReadOnlyList<TransferRecord> batch, CancellationToken cancellationToken)
      {
        Rows.AddRange(batch);
        return ValueTask.CompletedTask;
      }
      public ValueTask CompleteAsync(CancellationToken cancellationToken) => ValueTask.CompletedTask;
      public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }
  }
}
