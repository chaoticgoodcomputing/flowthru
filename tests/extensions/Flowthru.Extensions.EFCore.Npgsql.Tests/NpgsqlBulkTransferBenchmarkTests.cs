using System.Diagnostics;
using Flowthru.Data.Catalog;
using Flowthru.Data.Storage;
using Flowthru.Data.Storage.EFCore.Npgsql;
using Flowthru.Extensions.EFCore.Npgsql.Tests.Fixtures;
using Flowthru.Flow;
using Flowthru.Prelude;
using Flowthru.Tests.Kits.Prelude;
using Flowthru.Validation.PreFlight;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Testcontainers.PostgreSql;

namespace Flowthru.Extensions.EFCore.Npgsql.Tests;

/// <summary>
/// Category-gated benchmark (#139): the native raw binary COPY rung vs
/// the streaming rung over the <em>same</em> <c>AddBulkTransfer</c>
/// wiring and the same large table. The streaming leg hides the target's
/// bulk-import capability, so negotiation itself takes the fallback —
/// both legs measure exactly what a Flow developer would get. Asserts
/// correctness only (both rungs land every row); the timing comparison
/// is reported through the test output for a human (or a CI trend job)
/// to read — a pass/fail speed ratio would be flaky across hardware.
/// </summary>
/// <remarks>
/// Run with Docker available (or point
/// <c>FLOWTHRU_PG_TEST_CONNSTRING</c> at an existing PostgreSQL server):
/// <code>
/// dotnet test tests/extensions/Flowthru.Extensions.EFCore.Npgsql.Tests \
///   --filter "TestCategory=Benchmark"
/// </code>
/// Scale the table with <c>FLOWTHRU_BENCH_ROWS</c> (default 200000).
/// </remarks>
[TestFixture]
[Category("Benchmark")]
[Category("RequiresDocker")]
[Explicit("Benchmark — run explicitly or via --filter TestCategory=Benchmark (requires Docker).")]
public class NpgsqlBulkTransferBenchmarkTests
{
  private PostgreSqlContainer? _container;
  private string _adminConnectionString = null!;

  private static int Rows =>
    int.TryParse(Environment.GetEnvironmentVariable("FLOWTHRU_BENCH_ROWS"), out var n) && n > 0
      ? n
      : 200_000;

  [OneTimeSetUp]
  public async Task StartContainer()
  {
    var external = Environment.GetEnvironmentVariable(
      NpgsqlRawCopyTransferTests.ExternalServerVariable);
    if (!string.IsNullOrWhiteSpace(external))
    {
      _adminConnectionString = external;
      return;
    }

    Assume.That(TestCapabilities.Docker.IsAvailable(),
      $"[{TestCapabilities.Docker.Name}] {TestCapabilities.Docker.MissingMessage}");
    _container = new PostgreSqlBuilder().WithImage("postgres:16-alpine").Build();
    await _container.StartAsync();
    _adminConnectionString = _container.GetConnectionString();
  }

  [OneTimeTearDown]
  public async Task StopContainer()
  {
    if (_container is not null) await _container.DisposeAsync();
  }

  [Test]
  public async Task NativeRung_Vs_StreamingRung_OnTheSameLargeTable()
  {
    var rows = Rows;
    var sourceOptions = await CreateDatabaseAsync();
    var nativeTargetOptions = await CreateDatabaseAsync();
    var streamingTargetOptions = await CreateDatabaseAsync();
    await SeedServerSideAsync(sourceOptions, rows);

    var source = PgItem("staging_records", sourceOptions);
    var nativeTarget = PgItem("native_target", nativeTargetOptions);
    var streamingTarget = new HiddenImportItem(PgItem("streaming_target", streamingTargetOptions));

    // Negotiation picks a different rung per leg — same verb, same source.
    AssertRung(source, nativeTarget, BulkTransferRung.Native);
    AssertRung(source, streamingTarget, BulkTransferRung.Streaming);

    var nativeElapsed = await TimeTransfer("NativeLeg", source, nativeTarget);
    var streamingElapsed = await TimeTransfer("StreamingLeg", source, streamingTarget);

    Assert.That(await CountAsync(nativeTargetOptions), Is.EqualTo(rows),
      "The native rung must land every row.");
    Assert.That(await CountAsync(streamingTargetOptions), Is.EqualTo(rows),
      "The streaming rung must land every row.");

    TestContext.Out.WriteLine($"Rows:           {rows:N0}");
    TestContext.Out.WriteLine($"Native rung:    {nativeElapsed.TotalMilliseconds:F0} ms "
      + $"({rows / Math.Max(nativeElapsed.TotalSeconds, 0.001):N0} rows/s)");
    TestContext.Out.WriteLine($"Streaming rung: {streamingElapsed.TotalMilliseconds:F0} ms "
      + $"({rows / Math.Max(streamingElapsed.TotalSeconds, 0.001):N0} rows/s)");
    TestContext.Out.WriteLine($"Speedup:        {streamingElapsed.TotalMilliseconds / Math.Max(nativeElapsed.TotalMilliseconds, 0.001):F1}x");
  }

  // ===========================================================================
  // Helpers
  // ===========================================================================

  private static void AssertRung(
    IItem<IEnumerable<TransferRecord>> source,
    IItem<IEnumerable<TransferRecord>> target,
    BulkTransferRung expected
  )
  {
    var negotiation = BulkTransferNegotiation.Negotiate(source, target);
    var decision = ((Validated<PreFlightError, BulkTransferDecision>.Valid)negotiation).Value;
    Assert.That(decision.Rung, Is.EqualTo(expected), decision.Reason);
  }

  private static async Task<TimeSpan> TimeTransfer(
    string label,
    IItem<IEnumerable<TransferRecord>> source,
    IItem<IEnumerable<TransferRecord>> target
  )
  {
    var flow = FlowBuilder.CreateFlow(label, p => p.AddBulkTransfer(source, target));
    var stopwatch = Stopwatch.StartNew();
    var result = await flow.RunAsync();
    stopwatch.Stop();
    Assert.That(result.IsSuccess, Is.True, "Failures: " + string.Join(" | ",
      result.StepResults.OfType<StepResult.Failed>().Select(f => f.Error.Message)));
    return stopwatch.Elapsed;
  }

  private async Task<DbContextOptions<TransferDbContext>> CreateDatabaseAsync()
  {
    var dbName = $"bench_{Guid.NewGuid():N}";
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
    var options = new DbContextOptionsBuilder<TransferDbContext>()
      .UseNpgsql(builder.ConnectionString)
      .Options;

    await using var context = new TransferDbContext(options);
    await context.Database.EnsureCreatedAsync();
    return options;
  }

  /// <summary>
  /// Seed the source table server-side (<c>generate_series</c>) so the
  /// benchmark measures the transfer, not the CLR-side seeding.
  /// </summary>
  private static async Task SeedServerSideAsync(
    DbContextOptions<TransferDbContext> options, int rows
  )
  {
    await using var context = new TransferDbContext(options);
    await context.Database.ExecuteSqlRawAsync(
      "INSERT INTO transfer_records (\"Id\", \"Name\", amount_value, \"Active\") "
      + $"SELECT g, 'name_' || g, g * 1.5, g % 2 = 0 FROM generate_series(1, {rows}) g");
  }

  private static async Task<int> CountAsync(DbContextOptions<TransferDbContext> options)
  {
    await using var context = new TransferDbContext(options);
    return await context.Records.CountAsync();
  }

  private static IItem<IEnumerable<TransferRecord>> PgItem(
    string label, DbContextOptions<TransferDbContext> options
  ) =>
    Item.Of<IEnumerable<TransferRecord>>(label)
      .NpgsqlTable<TransferRecord, TransferDbContext>()
      .WithContextFactory(() => new TransferDbContext(options))
      .WithStreamingBatchSize(5_000)
      .Build();

  /// <summary>
  /// Delegating item that hides the bulk-import capability, so the same
  /// Postgres-backed target negotiates the streaming rung — the honest
  /// way to A/B the rungs through identical <c>AddBulkTransfer</c>
  /// wiring.
  /// </summary>
  private sealed class HiddenImportItem
    : IItem<IEnumerable<TransferRecord>>, ISupportsStreamingSink<TransferRecord>
  {
    private readonly IItem<IEnumerable<TransferRecord>> _inner;
    private readonly ISupportsStreamingSink<TransferRecord> _sinkable;

    public HiddenImportItem(IItem<IEnumerable<TransferRecord>> inner)
    {
      _inner = inner;
      _sinkable = (ISupportsStreamingSink<TransferRecord>)((Item<IEnumerable<TransferRecord>>)inner).Storage;
    }

    public string Label => _inner.Label;
    public NodeTraits Traits => _inner.Traits;

    public IFlowSink<TransferRecord> OpenStreamingSink() => _sinkable.OpenStreamingSink();

    public FlowIO<IEnumerable<TransferRecord>> Load() => _inner.Load();
    public FlowIO<FlowUnit> Save(IEnumerable<TransferRecord> data) => _inner.Save(data);
    public FlowIO<bool> Exists() => _inner.Exists();
    public FlowIO<ValidationResult> InspectShallow(int sampleSize = 100) => _inner.InspectShallow(sampleSize);
    public FlowIO<ValidationResult> InspectDeep() => _inner.InspectDeep();
    public FlowIO<ValidationResult> InspectTarget() => _inner.InspectTarget();
    public FlowIO<ValidationResult> Validate() => _inner.Validate();

    public ISupportsBulkImport? TryGetBulkImport() => null; // the deliberate hiding
  }
}
