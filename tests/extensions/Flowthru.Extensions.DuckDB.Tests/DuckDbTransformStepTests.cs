using Flowthru.Data.Catalog;
using Flowthru.Data.Storage;
using Flowthru.Data.Storage.Parquet;
using Flowthru.Extensions.DuckDB.Tests.Fixtures;
using Flowthru.Flow;
using Flowthru.Prelude;
using Flowthru.Step.DuckDb;
using Flowthru.Step.DuckDb.Internal;
using Flowthru.Validation.Runtime;
using Flowthru.Validation.Runtime.DuckDb;
using SysIO = System.IO;

namespace Flowthru.Extensions.DuckDB.Tests;

/// <summary>
/// End-to-end behaviour of the DuckDB transform step: Parquet in, SQL
/// inside the engine, Parquet out — plus the typed failure modes
/// (result-schema mismatch, remote bytes, engine errors) and the
/// wire-up-time rejections.
/// </summary>
[TestFixture]
[Category("DuckDB")]
public class DuckDbTransformStepTests
{
  private string _root = null!;
  private IDuckDbEngine _engine = null!;

  [SetUp]
  public void SetUp()
  {
    _root = SysIO.Path.Combine(SysIO.Path.GetTempPath(), $"flowthru-duckdb-{Guid.NewGuid():N}");
    SysIO.Directory.CreateDirectory(_root);
    _engine = new InProcessDuckDbEngine();
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

  // ── Happy paths ─────────────────────────────────────────────────────────

  [Test]
  public async Task SingleInput_CompositeKeySort_RunsInsideEngine()
  {
    var events = SeedEvents("events", "events.parquet", new[]
    {
      MakeEvent(3, "NZ", 30.0),
      MakeEvent(1, "AU", 10.0),
      MakeEvent(4, "AU", 40.0),
      MakeEvent(2, "NZ", 20.0),
    });
    var sorted = ItemFactory.Enumerable.Parquet<EventRow>(
      "sorted_events", Path("sorted.parquet"));

    var flow = FlowBuilder.CreateFlow("duckdb-sort", f => f.AddDuckDbTransform(
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
    }), "The engine-side ORDER BY must produce the composite-key order.");
    Assert.That(rows.Single(r => r.Id == 4).Value, Is.EqualTo(40.0),
      "Non-key columns must round-trip through the engine unchanged.");
  }

  [Test]
  public async Task MultiInput_JoinWithRenamedRelations_RunsInsideEngine()
  {
    var events = SeedEvents("events", "events.parquet", new[]
    {
      MakeEvent(1, "AU", 10.0),
      MakeEvent(2, "NZ", 20.0),
    });
    var regions = ItemFactory.Enumerable.Parquet<CountryRegionRow>(
      "country_regions", Path("regions.parquet"));
    await Save(regions, new[]
    {
      new CountryRegionRow { Country = "AU", Region = "Oceania" },
      new CountryRegionRow { Country = "NZ", Region = "Oceania" },
    });
    var enriched = ItemFactory.Enumerable.Parquet<EnrichedEventRow>(
      "enriched_events", Path("enriched.parquet"));

    var flow = FlowBuilder.CreateFlow("duckdb-join", f => f.AddDuckDbTransform(
      label: "enrich_events",
      inputs: new[]
      {
        DuckDbInputRelation.From(events, "ev"),
        DuckDbInputRelation.From(regions, "region_lookup"),
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
    }));
  }

  [Test]
  public async Task Aggregate_WithExplicitCasts_SatisfiesDeclaredSchema()
  {
    var events = SeedEvents("events", "events.parquet", new[]
    {
      MakeEvent(1, "AU", 10.0),
      MakeEvent(2, "AU", 5.0),
      MakeEvent(3, "NZ", 1.0),
    });
    var totals = ItemFactory.Enumerable.Parquet<CountryTotalRow>(
      "country_totals", Path("totals.parquet"));

    var flow = FlowBuilder.CreateFlow("duckdb-agg", f => f.AddDuckDbTransform(
      label: "totals_by_country",
      input: events,
      output: totals,
      sql: """
        SELECT
          Country,
          COUNT(*)              AS EventCount,
          CAST(SUM(Value) AS DOUBLE) AS TotalValue
        FROM events
        GROUP BY Country
        ORDER BY Country
        """,
      engine: _engine
    ));

    var result = await flow.RunAsync();
    Assert.That(result.IsSuccess, Is.True, Describe(result));

    var rows = await LoadRows(totals);
    Assert.That(rows.Select(r => (r.Country, r.EventCount, r.TotalValue)), Is.EqualTo(new[]
    {
      ("AU", 2L, 15.0), ("NZ", 1L, 1.0),
    }));
  }

  [Test]
  public void Step_IsVisibleOnDag_LikeAnyOtherStep()
  {
    var events = ItemFactory.Enumerable.Parquet<EventRow>("events", Path("e.parquet"));
    var sorted = ItemFactory.Enumerable.Parquet<EventRow>("sorted", Path("s.parquet"));

    var flow = FlowBuilder.CreateFlow("duckdb-dag", f => f.AddDuckDbTransform(
      "sort_events", events, sorted, "SELECT * FROM events", _engine));

    var step = flow.Steps.Single();
    Assert.Multiple(() =>
    {
      Assert.That(step.Label, Is.EqualTo("sort_events"));
      Assert.That(step.Inputs.Single().Label, Is.EqualTo("events"));
      Assert.That(step.Outputs.Single().Label, Is.EqualTo("sorted"));
      Assert.That(step.SourceLanguage, Is.EqualTo("sql"),
        "DAG renderers tag the step by SourceLanguage, like Python steps.");
      Assert.That(step.FlowLabel, Is.EqualTo("duckdb-dag"),
        "FlowBuilder.Add must stamp the defining flow's label.");
    });
  }

  // ── Result-schema mismatch (typed error value, no partial write) ────────

  [Test]
  public async Task SchemaMismatch_MissingAndExtraColumns_SurfacesTypedErrorValue()
  {
    var events = SeedEvents("events", "events.parquet", new[] { MakeEvent(1, "AU", 1.0) });
    var outputPath = Path("bad.parquet");
    var sorted = ItemFactory.Enumerable.Parquet<EventRow>("sorted", outputPath);

    var flow = FlowBuilder.CreateFlow("duckdb-mismatch", f => f.AddDuckDbTransform(
      label: "bad_projection",
      input: events,
      output: sorted,
      // Drops OccurredAt/Value and invents Extra — the declared EventRow
      // schema must reject both directions.
      sql: "SELECT Id, Country, 1 AS Extra FROM events",
      engine: _engine
    ));

    var result = await flow.RunAsync();
    Assert.That(result.IsSuccess, Is.False);

    var error = Unwrap(SingleFailure(result).Error);
    Assert.That(error, Is.InstanceOf<RuntimeError.SchemaMismatch>(),
      $"Expected the typed SchemaMismatch value, got: {error}");
    var mismatch = (RuntimeError.SchemaMismatch)error;
    Assert.Multiple(() =>
    {
      Assert.That(mismatch.Detail, Does.Contain("OccurredAt"), "names the missing column");
      Assert.That(mismatch.Detail, Does.Contain("Extra"), "names the extra column");
    });
    Assert.That(SysIO.File.Exists(outputPath), Is.False,
      "A mismatched transform must fail before writing the output file.");
  }

  [Test]
  public async Task SchemaMismatch_IncompatibleColumnType_PointsAtCast()
  {
    var events = SeedEvents("events", "events.parquet", new[] { MakeEvent(1, "AU", 1.0) });
    var sorted = ItemFactory.Enumerable.Parquet<EventRow>("sorted", Path("bad.parquet"));

    var flow = FlowBuilder.CreateFlow("duckdb-badtype", f => f.AddDuckDbTransform(
      label: "bad_types",
      input: events,
      output: sorted,
      // Id becomes VARCHAR — incompatible with the declared long.
      sql: "SELECT Country AS Id, Country, OccurredAt, Value FROM events",
      engine: _engine
    ));

    var result = await flow.RunAsync();
    var error = Unwrap(SingleFailure(result).Error);
    Assert.That(error, Is.InstanceOf<RuntimeError.SchemaMismatch>());
    Assert.That(((RuntimeError.SchemaMismatch)error).Detail, Does.Contain("CAST"),
      "The mismatch message should point at the fix (an explicit CAST).");
  }

  // ── Remote bytes (typed error value for non-s3 schemes) ─────────────────

  [Test]
  public async Task NonS3RemoteInput_FailsWithTypedRemoteBytesError()
  {
    var remote = RemoteItem("remote_events", new Uri("https://example.com/events.parquet"));
    var sorted = ItemFactory.Enumerable.Parquet<EventRow>("sorted", Path("s.parquet"));

    var step = new DuckDbTransformStep<EventRow>(
      label: "remote_sort",
      sql: "SELECT * FROM remote_events",
      inputs: new[] { DuckDbInputRelation.From(remote) },
      output: sorted,
      engine: _engine
    );

    var outcome = await step.Execute().Run();
    Assert.That(outcome, Is.InstanceOf<EffResult<FlowUnit>.Failure>());
    var error = Unwrap(((EffResult<FlowUnit>.Failure)outcome).Error);
    Assert.That(error, Is.InstanceOf<RuntimeError.ExtensionError>());
    var cause = ((RuntimeError.ExtensionError)error).Cause;
    Assert.That(cause, Is.InstanceOf<DuckDbRuntimeError.RemoteBytesUnsupported>(),
      $"Expected the typed remote-bytes error, got: {cause}");
    Assert.Multiple(() =>
    {
      Assert.That(cause.Message, Does.Contain("https://example.com/events.parquet"));
      Assert.That(cause.Message, Does.Contain("'https://'"),
        "The error must name the unsupported scheme honestly.");
      Assert.That(cause.Message, Does.Contain("s3://"),
        "The error must say which remote scheme IS supported.");
    });
  }

  [Test]
  public async Task NonS3RemoteOutput_FailsWithTypedRemoteBytesError()
  {
    var events = SeedEvents("events", "events.parquet", new[] { MakeEvent(1, "AU", 1.0) });
    var remoteOut = RemoteItem("remote_sorted", new Uri("ftp://example.com/sorted.parquet"));

    var step = new DuckDbTransformStep<EventRow>(
      label: "remote_out",
      sql: "SELECT * FROM events",
      inputs: new[] { DuckDbInputRelation.From(events) },
      output: remoteOut,
      engine: _engine
    );

    var outcome = await step.Execute().Run();
    Assert.That(outcome, Is.InstanceOf<EffResult<FlowUnit>.Failure>());
    var error = Unwrap(((EffResult<FlowUnit>.Failure)outcome).Error);
    Assert.That(error, Is.InstanceOf<RuntimeError.ExtensionError>());
    var cause = ((RuntimeError.ExtensionError)error).Cause;
    Assert.That(cause, Is.InstanceOf<DuckDbRuntimeError.RemoteBytesUnsupported>());
    Assert.That(cause.Message, Does.Contain("remote_sorted"),
      "The error must attribute the unsupported location to the output item.");
  }

  // ── S3 endpoints pass through to the engine (request planning) ──────────

  [Test]
  public async Task S3Endpoints_PassThroughToEngine_AsRemoteLocations()
  {
    var s3Access = new Dictionary<string, string>
    {
      ["region"] = "us-east-1",
      ["access_key_id"] = "AKIAEXAMPLE",
      ["secret_access_key"] = "supersecret",
    };
    var s3Input = RemoteItem(
      "s3_events", new Uri("s3://bucket/in/events.parquet"), s3Access);
    var localOutput = ItemFactory.Enumerable.Parquet<EventRow>("sorted", Path("s.parquet"));
    var recorder = new RequestRecordingEngine();

    var step = new DuckDbTransformStep<EventRow>(
      label: "s3_sort",
      sql: "SELECT * FROM s3_events",
      inputs: new[] { DuckDbInputRelation.From(s3Input) },
      output: localOutput,
      engine: recorder
    );

    var outcome = await step.Execute().Run();
    Assert.That(outcome, Is.InstanceOf<EffResult<FlowUnit>.Success>(),
      "An s3:// input must not be rejected by the step — the engine reaches it via httpfs.");

    var request = recorder.LastRequest!;
    Assert.Multiple(() =>
    {
      Assert.That(request.Relations.Single().Location,
        Is.InstanceOf<ByteLocation.RemoteUri>(),
        "The s3 location must reach the engine un-collapsed — no local staging.");
      var remote = (ByteLocation.RemoteUri)request.Relations.Single().Location;
      Assert.That(remote.Uri, Is.EqualTo(new Uri("s3://bucket/in/events.parquet")));
      Assert.That(remote.Access, Is.EqualTo(s3Access),
        "The gateway-minted access handoff must ride along for the engine's secret.");
      Assert.That(request.OutputLocation, Is.InstanceOf<ByteLocation.LocalFile>(),
        "The local output must stay a local file — endpoints resolve independently.");
    });
  }

  // ── Engine failure (typed error value) ──────────────────────────────────

  [Test]
  public async Task InvalidSql_FailsWithTypedEngineError()
  {
    var events = SeedEvents("events", "events.parquet", new[] { MakeEvent(1, "AU", 1.0) });
    var sorted = ItemFactory.Enumerable.Parquet<EventRow>("sorted", Path("s.parquet"));

    var flow = FlowBuilder.CreateFlow("duckdb-badsql", f => f.AddDuckDbTransform(
      "bad_sql", events, sorted, "SELECT FROM WHERE", _engine));

    var result = await flow.RunAsync();
    var error = Unwrap(SingleFailure(result).Error);
    Assert.That(error, Is.InstanceOf<RuntimeError.ExtensionError>());
    Assert.That(((RuntimeError.ExtensionError)error).Cause,
      Is.InstanceOf<DuckDbRuntimeError.EngineFailed>(),
      "A DuckDB parse/bind failure surfaces as the typed engine-failed value.");
  }

  // ── Wire-up rejections ──────────────────────────────────────────────────

  [Test]
  public void NonAddressableInput_IsRejectedAtWireUp()
  {
    var memory = ItemFactory.Enumerable.Memory<EventRow>("in_memory");

    Assert.That(
      () => DuckDbInputRelation.From(memory),
      Throws.ArgumentException.With.Message.Contains("byte-addressable"),
      "Memory-backed items can never feed an engine transform — that's a wiring bug."
    );
  }

  [Test]
  public void DuplicateRelationNames_AreRejectedAtWireUp()
  {
    var a = ItemFactory.Enumerable.Parquet<EventRow>("events_a", Path("a.parquet"));
    var b = ItemFactory.Enumerable.Parquet<EventRow>("events_b", Path("b.parquet"));
    var output = ItemFactory.Enumerable.Parquet<EventRow>("out", Path("o.parquet"));

    Assert.That(
      () => new DuckDbTransformStep<EventRow>(
        "dup", "SELECT 1", new[]
        {
          DuckDbInputRelation.From(a, "events"),
          DuckDbInputRelation.From(b, "events"),
        },
        output, _engine),
      Throws.ArgumentException.With.Message.Contains("events")
    );
  }

  [Test]
  public void EmptySqlOrInputs_AreRejectedAtWireUp()
  {
    var events = ItemFactory.Enumerable.Parquet<EventRow>("events", Path("e.parquet"));
    var output = ItemFactory.Enumerable.Parquet<EventRow>("out", Path("o.parquet"));

    Assert.Multiple(() =>
    {
      Assert.That(
        () => new DuckDbTransformStep<EventRow>(
          "s", "   ", new[] { DuckDbInputRelation.From(events) }, output, _engine),
        Throws.ArgumentException);
      Assert.That(
        () => new DuckDbTransformStep<EventRow>(
          "s", "SELECT 1", Array.Empty<DuckDbInputRelation>(), output, _engine),
        Throws.ArgumentException);
    });
  }

  // ── Harness ─────────────────────────────────────────────────────────────

  private string Path(string fileName) => SysIO.Path.Combine(_root, fileName);

  private static EventRow MakeEvent(long id, string country, double value) => new()
  {
    Id = id,
    Country = country,
    OccurredAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddDays(id),
    Value = value,
  };

  private IItem<IEnumerable<EventRow>> SeedEvents(
    string label, string fileName, IReadOnlyList<EventRow> rows
  )
  {
    var item = ItemFactory.Enumerable.Parquet<EventRow>(label, Path(fileName));
    Save(item, rows).GetAwaiter().GetResult();
    return item;
  }

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

  private static StepResult.Failed SingleFailure(FlowResult result) =>
    result.StepResults.OfType<StepResult.Failed>().Single();

  /// <summary>Peel <see cref="RuntimeError.StepFailed"/> attribution wrappers.</summary>
  private static RuntimeError Unwrap(RuntimeError error) =>
    error is RuntimeError.StepFailed stepFailed ? Unwrap(stepFailed.Cause) : error;

  private static string Describe(FlowResult result) =>
    string.Join("; ", result.StepResults.Select(r => r switch
    {
      StepResult.Failed f => $"{f.StepLabel}: FAILED {f.Error.Message}",
      StepResult.Skipped s => $"{s.StepLabel}: skipped ({s.Reason})",
      _ => $"{r.StepLabel}: ok",
    }));

  /// <summary>
  /// A Parquet item whose bytes locate behind a remote URI (plus optional
  /// access handoff) — simulates an object-store-backed item without an
  /// S3 dependency.
  /// </summary>
  private static IItem<IEnumerable<EventRow>> RemoteItem(
    string label, Uri uri, IReadOnlyDictionary<string, string>? access = null
  ) =>
    new Item<IEnumerable<EventRow>>(
      label,
      new ComposedStorageAdapter<IEnumerable<EventRow>, EventRow>(
        new FakeRemoteMedium(uri, access),
        new ParquetFormatSerializer<EventRow>(),
        new EnumerableContainerAdapter<EventRow>()
      )
    );

  /// <summary>
  /// Engine double that records the request it was handed and reports
  /// success — for asserting what the step plans, without a real engine.
  /// </summary>
  private sealed class RequestRecordingEngine : IDuckDbEngine
  {
    public DuckDbTransformRequest? LastRequest { get; private set; }

    public int MaxConcurrency => 1;

    public FlowIO<DuckDbTransformResult> ExecuteTransform(DuckDbTransformRequest request)
    {
      LastRequest = request;
      return FlowIO.Pure(
        new DuckDbTransformResult(0, Array.Empty<(string, string)>()));
    }
  }

  /// <summary>
  /// Byte-addressable medium whose bytes live behind a remote URI —
  /// simulates an object-store medium without an S3 dependency.
  /// </summary>
  private sealed class FakeRemoteMedium : IStorageMedium, ISupportsByteLocation
  {
    private readonly Uri _uri;
    private readonly IReadOnlyDictionary<string, string> _access;

    public FakeRemoteMedium(Uri uri, IReadOnlyDictionary<string, string>? access = null)
    {
      _uri = uri;
      _access = access ?? new Dictionary<string, string>();
    }

    public StorageTraits Traits => new() { CanRead = true, CanWrite = true, IsPersistent = true };

    public bool IsAddressable => true;

    public FlowIO<ByteLocation> LocateBytes() =>
      FlowIO.Pure<ByteLocation>(new ByteLocation.RemoteUri(_uri, _access));

    public FlowIO<Stream> ReadStream() =>
      FlowIO.Fail<Stream>(new RuntimeError.External(
        "FakeRemoteMedium", new InvalidOperationException("not readable in this test")));

    public FlowIO<FlowUnit> WriteStream(Stream stream) =>
      FlowIO.Fail<FlowUnit>(new RuntimeError.External(
        "FakeRemoteMedium", new InvalidOperationException("not writable in this test")));

    public FlowIO<bool> Exists() => FlowIO.Pure(true);
  }
}
