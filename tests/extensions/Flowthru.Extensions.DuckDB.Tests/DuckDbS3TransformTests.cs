using Amazon.S3;
using Amazon.S3.Model;
using Flowthru.Data.Catalog;
using Flowthru.Data.Schema;
using Flowthru.Data.Storage;
using Flowthru.Extensions.DuckDB.Tests.Fixtures;
using Flowthru.Flow;
using Flowthru.Hosting;
using Flowthru.Prelude;
using Flowthru.Step.DuckDb;
using Flowthru.Step.DuckDb.Internal;
using Flowthru.Tests.Kits.Prelude;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SysIO = System.IO;

namespace Flowthru.Extensions.DuckDB.Tests;

/// <summary>
/// Integration coverage for engine transforms over <c>s3://</c>
/// endpoints: the engine loads <c>httpfs</c>, turns each endpoint's
/// gateway-minted access handoff into a connection-scoped DuckDB
/// secret, reads inputs with <c>read_parquet('s3://…')</c>, and writes
/// the output with <c>COPY … TO 's3://…'</c> — no object body ever
/// buffered in the CLR.
/// </summary>
/// <remarks>
/// <para>
/// <strong>Gated on <see cref="TestCapabilities.AwsS3"/>.</strong> Runs
/// against any S3-compatible endpoint supplied via the environment
/// (<c>FLOWTHRU_S3_TEST_BUCKET</c> plus optional
/// <c>FLOWTHRU_S3_TEST_SERVICE_URL</c> / <c>FLOWTHRU_S3_TEST_REGION</c>;
/// credentials via the standard AWS chain, e.g.
/// <c>AWS_ACCESS_KEY_ID</c> / <c>AWS_SECRET_ACCESS_KEY</c> for MinIO).
/// Reports Inconclusive when no endpoint is configured, so the default
/// CI flow stays green. Example against a local MinIO:
/// </para>
/// <code>
/// podman run -d --name minio -p 9000:9000 \
///   -e MINIO_ROOT_USER=minioadmin -e MINIO_ROOT_PASSWORD=minioadmin \
///   quay.io/minio/minio server /data
/// FLOWTHRU_S3_TEST_BUCKET=flowthru-duckdb \
///   FLOWTHRU_S3_TEST_SERVICE_URL=http://localhost:9000 \
///   AWS_ACCESS_KEY_ID=minioadmin AWS_SECRET_ACCESS_KEY=minioadmin \
///   dotnet test tests/extensions/Flowthru.Extensions.DuckDB.Tests \
///   --filter FullyQualifiedName~DuckDbS3TransformTests
/// </code>
/// <para>
/// <strong>Network note.</strong> The bundled DuckDB does not
/// statically link <c>httpfs</c>; the first s3 transform on a machine
/// runs <c>INSTALL httpfs</c> (one download into <c>~/.duckdb</c>)
/// unless the extension was pre-provisioned. See the extension README's
/// S3 section.
/// </para>
/// </remarks>
[TestFixture]
[Category("DuckDB")]
[Category("AwsS3")]
[Category("Integration")]
public class DuckDbS3TransformTests
{
  private string _bucket = null!;
  private string? _serviceUrl;
  private string? _region;
  private string _prefix = null!;
  private ServiceProvider _provider = null!;
  private IStorageMediumResolver _resolver = null!;
  private IDuckDbEngine _engine = null!;
  private string _localRoot = null!;

  [OneTimeSetUp]
  public async Task ConfigureEndpoint()
  {
    Assume.That(TestCapabilities.AwsS3.IsAvailable(),
      $"[{TestCapabilities.AwsS3.Name}] {TestCapabilities.AwsS3.MissingMessage}");

    _bucket = Environment.GetEnvironmentVariable("FLOWTHRU_S3_TEST_BUCKET")!;
    _serviceUrl = Environment.GetEnvironmentVariable("FLOWTHRU_S3_TEST_SERVICE_URL");
    _region = Environment.GetEnvironmentVariable("FLOWTHRU_S3_TEST_REGION");
    _prefix = $"flowthru-duckdb-e2e/{Guid.NewGuid():N}";

    // The real production wiring: gateway from S3Options, credentials via
    // the AWS chain — the exact path a deployed flow's LocateBytes takes.
    var services = new ServiceCollection();
    services.AddSingleton<IConfiguration>(new ConfigurationBuilder().Build());
    services.AddFlowthru(b => b.UseS3(s3 =>
    {
      if (!string.IsNullOrWhiteSpace(_serviceUrl))
      {
        s3.ServiceUrl = _serviceUrl;
        s3.ForcePathStyle = true;
      }
      if (!string.IsNullOrWhiteSpace(_region)) s3.Region = _region;
    }));
    _provider = services.BuildServiceProvider();
    _resolver = _provider.GetRequiredService<IStorageMediumResolver>();
    _engine = new InProcessDuckDbEngine();

    _localRoot = SysIO.Path.Combine(
      SysIO.Path.GetTempPath(), $"flowthru-duckdb-s3-{Guid.NewGuid():N}");
    SysIO.Directory.CreateDirectory(_localRoot);

    using var admin = BuildClient();
    try { await admin.PutBucketAsync(_bucket); }
    catch (AmazonS3Exception) { /* already exists (or a real AWS bucket) */ }
  }

  [OneTimeTearDown]
  public async Task CleanupEndpoint()
  {
    if (_provider is null) return; // gate never cleared

    try
    {
      using var admin = BuildClient();
      var listed = await admin.ListObjectsV2Async(
        new ListObjectsV2Request { BucketName = _bucket, Prefix = _prefix });
      foreach (var obj in listed.S3Objects ?? [])
      {
        try { await admin.DeleteObjectAsync(_bucket, obj.Key); }
        catch { /* best effort */ }
      }
    }
    catch { /* best effort */ }

    _provider.Dispose();
    if (SysIO.Directory.Exists(_localRoot))
    {
      try { SysIO.Directory.Delete(_localRoot, recursive: true); }
      catch { /* best effort */ }
    }
  }

  // ── S3 → S3, entirely in-engine ─────────────────────────────────────────

  [Test]
  public async Task S3ToS3_Transform_SortsInsideEngine()
  {
    var events = S3Parquet<EventRow>("events", "in/events.parquet");
    await Save(events, new[]
    {
      MakeEvent(3, "NZ", 30.0),
      MakeEvent(1, "AU", 10.0),
      MakeEvent(4, "AU", 40.0),
      MakeEvent(2, "NZ", 20.0),
    });
    var sorted = S3Parquet<EventRow>("sorted_events", "out/sorted.parquet");

    var flow = FlowBuilder.CreateFlow("duckdb-s3-sort", f => f.AddDuckDbTransform(
      label: "sort_events",
      input: events,
      output: sorted,
      sql: "SELECT * FROM events ORDER BY Country, Id",
      engine: _engine
    ));

    var result = await flow.RunAsync();
    Assert.That(result.IsSuccess, Is.True, Describe(result));

    var rows = await LoadRows(sorted);
    Assert.That(rows.Select(r => (r.Country, r.Id)), Is.EqualTo(new[]
    {
      ("AU", 1L), ("AU", 4L), ("NZ", 2L), ("NZ", 3L),
    }), "The engine-side ORDER BY over s3:// endpoints must produce the sort.");
  }

  // ── Mixed local + S3 inputs ─────────────────────────────────────────────

  [Test]
  public async Task MixedLocalAndS3Inputs_JoinIntoS3Output()
  {
    var s3Events = S3Parquet<EventRow>("events", "in/mixed-events.parquet");
    await Save(s3Events, new[] { MakeEvent(1, "AU", 10.0), MakeEvent(2, "NZ", 20.0) });

    var localRegions = ItemFactory.Enumerable.Parquet<CountryRegionRow>(
      "country_regions", SysIO.Path.Combine(_localRoot, "regions.parquet"));
    await Save(localRegions, new[]
    {
      new CountryRegionRow { Country = "AU", Region = "Oceania" },
      new CountryRegionRow { Country = "NZ", Region = "Oceania" },
    });

    var enriched = S3Parquet<EnrichedEventRow>("enriched", "out/enriched.parquet");

    var flow = FlowBuilder.CreateFlow("duckdb-s3-mixed", f => f.AddDuckDbTransform(
      label: "enrich_events",
      inputs: new[]
      {
        DuckDbInputRelation.From(s3Events, "ev"),
        DuckDbInputRelation.From(localRegions, "region_lookup"),
      },
      output: enriched,
      sql: """
        SELECT ev.Id, ev.Country, region_lookup.Region
        FROM ev JOIN region_lookup USING (Country)
        ORDER BY ev.Id
        """,
      engine: _engine
    ));

    var result = await flow.RunAsync();
    Assert.That(result.IsSuccess, Is.True, Describe(result));

    var rows = await LoadRows(enriched);
    Assert.That(rows.Select(r => (r.Id, r.Region)), Is.EqualTo(new[]
    {
      (1L, "Oceania"), (2L, "Oceania"),
    }), "A local file and an s3 object must be joinable in one transform body.");
  }

  // ── Per-endpoint secret scoping ─────────────────────────────────────────

  [Test]
  public async Task MultipleS3Endpoints_EachWithItsOwnScopedSecret_ResolveIndependently()
  {
    // Three s3 endpoints in one transform (two inputs + output) mint three
    // temporary secrets, each SCOPEd to exactly its object. If scoping bled
    // (one secret shadowing another, or a clash on creation) the transform
    // would fail or read the wrong object.
    var current = S3Parquet<EventRow>("current_events", "in/scope-current.parquet");
    await Save(current, new[] { MakeEvent(1, "AU", 10.0) });
    var historic = S3Parquet<EventRow>("historic_events", "in/scope-historic.parquet");
    await Save(historic, new[] { MakeEvent(2, "NZ", 20.0) });

    var combined = S3Parquet<EventRow>("combined_events", "out/scope-combined.parquet");

    var flow = FlowBuilder.CreateFlow("duckdb-s3-scopes", f => f.AddDuckDbTransform(
      label: "union_events",
      inputs: new[]
      {
        DuckDbInputRelation.From(current, "current"),
        DuckDbInputRelation.From(historic, "historic"),
      },
      output: combined,
      sql: """
        SELECT * FROM current
        UNION ALL
        SELECT * FROM historic
        ORDER BY Id
        """,
      engine: _engine
    ));

    var result = await flow.RunAsync();
    Assert.That(result.IsSuccess, Is.True, Describe(result));

    var rows = await LoadRows(combined);
    Assert.That(rows.Select(r => (r.Id, r.Country)), Is.EqualTo(new[]
    {
      (1L, "AU"), (2L, "NZ"),
    }), "Every endpoint must have read/written through its own scoped secret.");
  }

  // ── The structural guarantee extends to S3 ──────────────────────────────

  [Test]
  public async Task S3Transform_MaterializesZeroRowsInTheClr()
  {
    // Seed through the uninstrumented twin so the counter stays clean.
    var seed = S3Parquet<PlainRow>("seed", "in/norows.parquet");
    var seedRows = Enumerable.Range(1, 2_000)
      .Select(i => new PlainRow { Id = i, Country = i % 2 == 0 ? "AU" : "NZ", Value = i * 0.5 })
      .ToList();
    await Save(seed, seedRows);

    // The transform's endpoints are typed with the INSTRUMENTED schema.
    var input = S3Parquet<InstrumentedRow>("rows", "in/norows.parquet");
    var output = S3Parquet<InstrumentedRow>("sorted_rows", "out/norows-sorted.parquet");

    RowMaterializationCounter.Reset();

    var flow = FlowBuilder.CreateFlow("duckdb-s3-norows", f => f.AddDuckDbTransform(
      label: "sort_rows",
      input: input,
      output: output,
      sql: "SELECT * FROM rows ORDER BY Country, Id",
      engine: _engine
    ));
    var result = await flow.RunAsync();

    Assert.That(result.IsSuccess, Is.True, Describe(result));
    Assert.That(RowMaterializationCounter.Count, Is.Zero,
      "An s3://→s3:// engine transform must not materialize a single row in the "
      + "CLR — a non-zero count means some path loaded or constructed "
      + "InstrumentedRow values.");

    // Verify correctness through the uninstrumented twin: the counter
    // assertion above must not be satisfied by an empty or wrong output.
    var verify = S3Parquet<PlainRow>("verify", "out/norows-sorted.parquet");
    var rows = await LoadRows(verify);
    Assert.That(rows, Has.Count.EqualTo(seedRows.Count));
    Assert.That(
      rows.Select(r => (r.Country, r.Id)),
      Is.EqualTo(seedRows.Select(r => (r.Country, r.Id))
        .OrderBy(t => t.Country, StringComparer.Ordinal).ThenBy(t => t.Id)),
      "The transform must have produced the composite-key sort."
    );
    Assert.That(RowMaterializationCounter.Count, Is.Zero,
      "Verification used the PlainRow twin — the instrumented counter must still be zero.");
  }

  // ── Harness ─────────────────────────────────────────────────────────────

  private IItem<IEnumerable<TRow>> S3Parquet<TRow>(string label, string key)
    where TRow : notnull, IFlatSchema, IBinarySerializable
    =>
    ItemFactory.Enumerable.Parquet<TRow>(
      label, $"s3://{_bucket}/{_prefix}/{key}", resolver: _resolver);

  private AmazonS3Client BuildClient()
  {
    var config = new AmazonS3Config { AuthenticationRegion = _region ?? "us-east-1" };
    if (!string.IsNullOrWhiteSpace(_serviceUrl))
    {
      config.ServiceURL = _serviceUrl;
      config.ForcePathStyle = true;
    }
    return new AmazonS3Client(config);
  }

  private static EventRow MakeEvent(long id, string country, double value) => new()
  {
    Id = id,
    Country = country,
    OccurredAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddDays(id),
    Value = value,
  };

  private static async Task Save<TRow>(IItem<IEnumerable<TRow>> item, IReadOnlyList<TRow> rows)
  {
    var outcome = await item.Save(rows).Run();
    Assert.That(outcome, Is.InstanceOf<EffResult<FlowUnit>.Success>(),
      $"Seeding item '{item.Label}' failed: {outcome}");
  }

  private static async Task<List<TRow>> LoadRows<TRow>(IItem<IEnumerable<TRow>> item)
  {
    var outcome = await item.Load().Run();
    Assert.That(outcome, Is.InstanceOf<EffResult<IEnumerable<TRow>>.Success>(),
      $"Loading item '{item.Label}' failed: {outcome}");
    return ((EffResult<IEnumerable<TRow>>.Success)outcome).Value.ToList();
  }

  private static string Describe(FlowResult result) =>
    string.Join("; ", result.StepResults.Select(r => r switch
    {
      StepResult.Failed f => $"{f.StepLabel}: FAILED {f.Error.Message}",
      StepResult.Skipped s => $"{s.StepLabel}: skipped ({s.Reason})",
      _ => $"{r.StepLabel}: ok",
    }));
}

/// <summary>
/// The always-on offline tier of the same wiring: <c>s3://</c> items
/// over the shipped file-backed gateway stub. The stub's
/// <c>LocateObject</c> honestly answers with the backing file
/// (<see cref="ByteLocation.LocalFile"/>, no access handoff), so the
/// transform runs the local path — proving the whole
/// resolver → medium → gateway → engine handoff seam end-to-end with no
/// AWS account, network, or httpfs. The <c>RemoteUri</c>/httpfs half
/// lives in <see cref="DuckDbS3TransformTests"/> above.
/// </summary>
[TestFixture]
[Category("DuckDB")]
public class DuckDbLocalS3StubTransformTests
{
  private string _root = null!;

  [SetUp]
  public void SetUp()
  {
    _root = SysIO.Path.Combine(
      SysIO.Path.GetTempPath(), $"flowthru-duckdb-s3stub-{Guid.NewGuid():N}");
    SysIO.Directory.CreateDirectory(_root);
  }

  [TearDown]
  public void TearDown()
  {
    if (SysIO.Directory.Exists(_root))
    {
      try { SysIO.Directory.Delete(_root, recursive: true); }
      catch { /* best effort */ }
    }
  }

  [Test]
  public async Task S3StubbedEndpoints_TransformRunsFullyOffline()
  {
    var services = new ServiceCollection();
    services.AddSingleton<IConfiguration>(new ConfigurationBuilder().Build());
    services.AddFlowthru(b => b.UseLocalS3(_root));
    using var provider = services.BuildServiceProvider();
    var resolver = provider.GetRequiredService<IStorageMediumResolver>();

    var events = ItemFactory.Enumerable.Parquet<EventRow>(
      "events", "s3://stub-bucket/in/events.parquet", resolver: resolver);
    var saved = await events.Save(new[]
    {
      new EventRow
      {
        Id = 2, Country = "NZ",
        OccurredAt = new DateTime(2026, 1, 2, 0, 0, 0, DateTimeKind.Utc), Value = 2.0,
      },
      new EventRow
      {
        Id = 1, Country = "AU",
        OccurredAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc), Value = 1.0,
      },
    }).Run();
    Assert.That(saved, Is.InstanceOf<EffResult<FlowUnit>.Success>());

    var sorted = ItemFactory.Enumerable.Parquet<EventRow>(
      "sorted", "s3://stub-bucket/out/sorted.parquet", resolver: resolver);

    var flow = FlowBuilder.CreateFlow("duckdb-s3stub", f => f.AddDuckDbTransform(
      label: "sort_events",
      input: events,
      output: sorted,
      sql: "SELECT * FROM events ORDER BY Id",
      engine: new InProcessDuckDbEngine()
    ));
    var result = await flow.RunAsync();

    Assert.That(result.IsSuccess, Is.True,
      string.Join("; ", result.StepResults.Select(r => r.ToString())));

    var loaded = await sorted.Load().Run();
    Assert.That(loaded, Is.InstanceOf<EffResult<IEnumerable<EventRow>>.Success>());
    var rows = ((EffResult<IEnumerable<EventRow>>.Success)loaded).Value.ToList();
    Assert.That(rows.Select(r => r.Id), Is.EqualTo(new[] { 1L, 2L }),
      "The stub-backed s3 items must transform exactly like local files.");
  }
}
