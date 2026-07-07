using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using Flowthru.Data.Catalog;
using Flowthru.Data.Storage;
using Flowthru.Data.Storage.Parquet;
using Flowthru.Data.Storage.S3;
using Flowthru.Hosting;
using Flowthru.Prelude;
using Flowthru.Tests.Kits.Prelude;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Parquet;

namespace Flowthru.Extensions.AWS.S3.Tests;

/// <summary>
/// Regression coverage for reading a <strong>multi-row-group</strong>
/// <c>s3://</c> Parquet object back through the <em>streaming</em> catalog view —
/// the ADR-0023 path that bounds peak read memory to O(one row group) instead of
/// O(whole object). Drives the production wiring end-to-end:
/// <c>ItemFactory.Enumerable.Parquet(...).AsStream()</c> →
/// <c>Load()</c> (deferred <see cref="FlowSource{T}"/>) →
/// <c>Compile().ToList()</c>/<c>Fold</c>, over a real MinIO S3 server whose
/// forward-only response body forces the Parquet reader's
/// <c>SeekableSpill</c> (temp-file spill, one row group resident at a time).
/// A wide fan-out of concurrent streaming reads mirrors the parallel scheduler
/// dispatching a whole layer of <c>s3://</c> inputs (issue #124).
/// </summary>
/// <remarks>
/// <para>
/// <strong>Self-provisioning MinIO, gated on <see cref="TestCapabilities.Docker"/>.</strong>
/// Unlike <see cref="ParquetOverS3ConcurrencyTests"/> (which targets any external
/// S3 endpoint via <c>FLOWTHRU_S3_TEST_*</c>), this tier spins up a
/// <c>Testcontainers</c>-managed MinIO container — mirroring
/// <see cref="Backends.MinioContainerBackend"/> — so it needs no AWS account and
/// no external endpoint. When Docker/podman is unavailable the fixture reports
/// <em>Inconclusive</em> (never a failure), so a runtime-less host stays green.
/// </para>
/// <para>
/// <strong>The memory ceiling is imposed externally.</strong> This test asserts
/// only <em>correctness</em> — every streamed read returns all seeded rows, in
/// order, uncorrupted. It does <em>not</em> measure its own RSS. The
/// constrained-container CI job (<c>tests/extensions/CONTRIBUTING.md</c>,
/// "Constrained-Resource Tiers") runs it under an explicit <c>--memory</c> cap;
/// because each streaming read is O(row group), the process RSS must stay flat as
/// <c>FLOWTHRU_STREAM_ROWS</c> grows — the eager path (whole-object buffering)
/// would blow the cap. Load knobs (env, with defaults):
/// <c>FLOWTHRU_STREAM_ROWS</c> (40000), <c>FLOWTHRU_STREAM_ROWGROUP</c> (4000, so
/// ~10 groups), <c>FLOWTHRU_STREAM_OBJECTS</c> (8, the fan-out width),
/// <c>FLOWTHRU_STREAM_MAXPAR</c> (= objects).
/// </para>
/// </remarks>
[TestFixture]
[Category("AwsS3")]
[Category("Integration")]
[Category("RequiresDocker")]
public class StreamingParquetOverS3Tests
{
  private const string AccessKey = "minioadmin";
  private const string SecretKey = "minioadmin";
  private const string Bucket = "flowthru-streaming";

  private IContainer? _container;
  private string _endpoint = null!;
  private int _rows;
  private int _rowGroupSize;
  private int _objects;

  private static string Env(string key, string dflt) =>
    Environment.GetEnvironmentVariable(key) is { Length: > 0 } v ? v : dflt;

  [OneTimeSetUp]
  public async Task StartMinioAndSeed()
  {
    // Capability gate: no Docker/podman ⇒ Inconclusive before the container ever
    // starts. Mirrors MinioContainerBackend / the extension-test gating convention.
    Assume.That(TestCapabilities.Docker.IsAvailable(),
      $"[{TestCapabilities.Docker.Name}] {TestCapabilities.Docker.MissingMessage}");

    _rows = int.Parse(Env("FLOWTHRU_STREAM_ROWS", "40000"));
    _rowGroupSize = int.Parse(Env("FLOWTHRU_STREAM_ROWGROUP", "4000"));
    _objects = int.Parse(Env("FLOWTHRU_STREAM_OBJECTS", "8"));

    _container = new ContainerBuilder()
      .WithImage("minio/minio:latest")
      .WithEnvironment("MINIO_ROOT_USER", AccessKey)
      .WithEnvironment("MINIO_ROOT_PASSWORD", SecretKey)
      .WithCommand("server", "/data")
      .WithPortBinding(9000, assignRandomHostPort: true)
      .WithWaitStrategy(Wait.ForUnixContainer()
        .UntilHttpRequestIsSucceeded(r => r.ForPath("/minio/health/ready").ForPort(9000)))
      .Build();
    await _container.StartAsync();
    _endpoint = $"http://{_container.Hostname}:{_container.GetMappedPublicPort(9000)}";

    using var admin = new AmazonS3Client(
      new BasicAWSCredentials(AccessKey, SecretKey),
      new AmazonS3Config
      {
        ServiceURL = _endpoint,
        ForcePathStyle = true,
        AuthenticationRegion = "us-east-1",
      });
    await admin.PutBucketAsync(Bucket);

    // A small RowGroupSize relative to the row count forces several row groups,
    // so a streaming read genuinely advances group-by-group (rather than a
    // single-group file that a reader could satisfy in one pass).
    var bytes = await BuildMultiRowGroupParquet(_rows, _rowGroupSize);

    // Honesty check: prove the seed really is multi-row-group, so the test
    // exercises the incremental-yield path it claims to.
    using (var probe = new MemoryStream(bytes, writable: false))
    using (var reader = await ParquetReader.CreateAsync(probe))
    {
      Assert.That(reader.RowGroupCount, Is.GreaterThan(1),
        "Seed object must span multiple row groups to exercise streaming; "
        + $"lower FLOWTHRU_STREAM_ROWGROUP below FLOWTHRU_STREAM_ROWS ({_rows}).");
    }

    foreach (var i in Enumerable.Range(0, _objects))
    {
      using var body = new MemoryStream(bytes, writable: false);
      await admin.PutObjectAsync(new PutObjectRequest
      {
        BucketName = Bucket,
        Key = ObjectKey(i),
        InputStream = body,
        AutoCloseStream = false,
      });
    }

    TestContext.Out.WriteLine(
      $"[s3-streaming] seeded {_objects} objects x {_rows} rows "
      + $"(rowGroupSize={_rowGroupSize}), {bytes.Length:N0} bytes/object into "
      + $"'{Bucket}' at {_endpoint}");
  }

  [OneTimeTearDown]
  public async Task StopMinio()
  {
    if (_container is not null)
    {
      await _container.DisposeAsync();
      _container = null;
    }
  }

  /// <summary>
  /// The core streaming assertion: one <c>.AsStream()</c> read of a multi-row-group
  /// object returns every seeded row, in order, with correct field values.
  /// </summary>
  [Test]
  public async Task StreamedRead_ReturnsAllSeededRows_InOrder()
  {
    using var sp = BuildProvider();
    var item = ParquetItem(sp, ObjectKey(0));

    // The path under test: the streaming view's Load() hands back a deferred
    // source, which compiles back into a FlowIO we materialise. Peak read memory
    // is one row group even though ToList collects the whole (single-object) result.
    var source = Unwrap(await item.Load().Run());
    var rows = Unwrap(await source.Compile().ToList().Run());

    Assert.That(rows, Has.Count.EqualTo(_rows), "Streamed read dropped or duplicated rows.");

    // Spot-check values at the ends and interior — order and content both.
    Assert.That(rows[0].Id, Is.EqualTo(0));
    Assert.That(rows[^1].Id, Is.EqualTo(_rows - 1));
    var mid = _rows / 2;
    Assert.That(rows[mid].Id, Is.EqualTo(mid));
    Assert.That(rows[mid].Name, Is.EqualTo($"name-{mid}"));
    Assert.That(rows[mid].Category, Is.EqualTo($"cat-{mid % 50}"));
    Assert.That(rows[mid].V2, Is.EqualTo(mid * 2.2).Within(1e-9));
  }

  /// <summary>
  /// A wide layer of concurrent streaming reads — the shape the parallel scheduler
  /// dispatches — all return correct rows. Each read counts via <c>Fold</c>
  /// (O(row group), not O(object)), so under a constrained-container cap RSS stays
  /// flat regardless of fan-out width or row count.
  /// </summary>
  [Test]
  public async Task ConcurrentStreamedReads_StayConsistent()
  {
    using var sp = BuildProvider();
    var items = Enumerable.Range(0, _objects)
      .Select(i => ParquetItem(sp, ObjectKey(i)))
      .ToList();

    var maxPar = int.Parse(Env("FLOWTHRU_STREAM_MAXPAR", _objects.ToString()));
    using var gate = new SemaphoreSlim(maxPar, maxPar);

    // The whole dataset's Id column sums to the triangular number — a checksum
    // that catches missing, duplicated, or corrupted rows without materialising
    // the stream (keeping each concurrent read O(row group)).
    var expectedIdSum = (long)_rows * (_rows - 1) / 2;

    async Task<(string Label, long Count, long IdSum, string? Error)> StreamCount(
      IReadOnlyItem<FlowSource<PqRow>> item)
    {
      await gate.WaitAsync();
      try
      {
        var loaded = await item.Load().Run();
        if (loaded is EffResult<FlowSource<PqRow>>.Failure lf)
        {
          return (item.Label, -1, -1, lf.Error.Message);
        }

        var source = ((EffResult<FlowSource<PqRow>>.Success)loaded).Value;
        var tally = await source.Compile()
          .Fold(new ReadTally(0, 0), static (t, row) => new ReadTally(t.Count + 1, t.IdSum + row.Id))
          .Run();

        if (tally is EffResult<ReadTally>.Failure tf)
        {
          return (item.Label, -1, -1, tf.Error.Message);
        }

        var value = ((EffResult<ReadTally>.Success)tally).Value;
        return (item.Label, value.Count, value.IdSum, null);
      }
      catch (Exception ex)
      {
        return (item.Label, -1, -1, $"{ex.GetType().Name}: {ex.Message}");
      }
      finally
      {
        gate.Release();
      }
    }

    var results = await Task.WhenAll(items.Select(i => Task.Run(() => StreamCount(i))));

    var failures = results.Where(r => r.Error is not null).ToList();
    var wrongCount = results.Where(r => r.Error is null && r.Count != _rows).ToList();
    var wrongSum = results.Where(r => r.Error is null && r.IdSum != expectedIdSum).ToList();
    TestContext.Out.WriteLine(
      $"[s3-streaming] maxPar={maxPar} "
      + $"ok={results.Count(r => r.Error is null && r.Count == _rows && r.IdSum == expectedIdSum)}/{_objects} "
      + $"failed={failures.Count} wrongCount={wrongCount.Count} wrongSum={wrongSum.Count}");
    foreach (var f in failures.Take(5))
    {
      TestContext.Out.WriteLine($"[s3-streaming]   FAIL {f.Label}: {f.Error}");
    }

    Assert.That(failures, Is.Empty,
      $"Concurrent streaming reads produced {failures.Count} decode/integrity failures (issue #124).");
    Assert.That(wrongCount, Is.Empty, "Some streaming reads returned the wrong row count.");
    Assert.That(wrongSum, Is.Empty, "Some streaming reads returned corrupted/reordered rows (Id checksum mismatch).");
  }

  // ── wiring ──────────────────────────────────────────────────────────────

  private ServiceProvider BuildProvider()
  {
    var services = new ServiceCollection();
    services.AddSingleton<IConfiguration>(new ConfigurationBuilder().Build());
    // Production wiring over the injected-gateway swap point: an AmazonS3Gateway
    // pointed at the MinIO container with its basic credentials. No process-env
    // mutation — mirrors MinioContainerBackend's client build.
    var client = new AmazonS3Client(
      new BasicAWSCredentials(AccessKey, SecretKey),
      new AmazonS3Config
      {
        ServiceURL = _endpoint,
        ForcePathStyle = true,
        AuthenticationRegion = "us-east-1",
      });
    services.AddFlowthru(b => b.UseS3(new AmazonS3Gateway(client)));
    return services.BuildServiceProvider();
  }

  private IReadOnlyItem<FlowSource<PqRow>> ParquetItem(ServiceProvider sp, string key)
  {
    var resolver = sp.GetRequiredService<IStorageMediumResolver>();
    return ItemFactory.Enumerable
      .Parquet<PqRow>(label: key, filePath: $"s3://{Bucket}/{key}", resolver: resolver)
      .AsStream();
  }

  private static string ObjectKey(int i) => $"flowthru-streaming/obj{i}.parquet";

  private static async Task<byte[]> BuildMultiRowGroupParquet(int rows, int rowGroupSize)
  {
    // Reuses the PqRow schema and ParquetFormatSerializer from
    // ParquetOverS3ConcurrencyTests; the only difference is an explicit small
    // RowGroupSize so the object spans multiple groups.
    var serializer = new ParquetFormatSerializer<PqRow>(
      new ParquetItemOptions<PqRow> { RowGroupSize = rowGroupSize });
    using var ms = new MemoryStream();
    await serializer.SerializeRows(ms, Generate(rows));
    return ms.ToArray();
  }

  private static async IAsyncEnumerable<PqRow> Generate(int rows)
  {
    for (var i = 0; i < rows; i++)
    {
      yield return new PqRow
      {
        Id = i,
        Name = $"name-{i}",
        Category = $"cat-{i % 50}",
        V1 = i * 1.1,
        V2 = i * 2.2,
        V3 = i * 3.3,
        Flags = i % 7,
        Payload = new string((char)('a' + (i % 26)), 64),
      };
      if ((i & 4095) == 0) await Task.Yield();
    }
  }

  private static T Unwrap<T>(EffResult<T> result) =>
    result is EffResult<T>.Success s
      ? s.Value
      : throw new AssertionException(
          $"Expected a successful effect, got failure: {((EffResult<T>.Failure)result).Error}");

  /// <summary>A streaming read's running total — row count plus an Id checksum.</summary>
  private readonly record struct ReadTally(long Count, long IdSum);
}
