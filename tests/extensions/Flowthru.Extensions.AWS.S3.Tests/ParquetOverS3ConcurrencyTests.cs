using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;
using Flowthru.Data.Catalog;
using Flowthru.Data.Schema;
using Flowthru.Data.Storage;
using Flowthru.Data.Storage.Parquet;
using Flowthru.Hosting;
using Flowthru.Prelude;
using Flowthru.Tests.Kits.Prelude;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Parquet;

namespace Flowthru.Extensions.AWS.S3.Tests;

/// <summary>
/// Integration coverage for reading many <c>s3://</c> Parquet Items concurrently —
/// the shape that crash-loops in production under a memory-constrained host
/// (issue #111). Drives the full production path: <c>UseS3()</c> →
/// <see cref="Flowthru.Data.Storage.S3.S3StorageMedium"/> → the AWS gateway's
/// forward-only response body → the Parquet buffering path → the runtime DTO,
/// fanning out N concurrent <c>Item.Load()</c>s exactly as the parallel scheduler
/// dispatches a wide layer of parallel-safe s3:// inputs.
/// </summary>
/// <remarks>
/// <para>
/// <strong>Gated on <see cref="TestCapabilities.AwsS3"/>.</strong> Runs against any
/// S3-compatible endpoint supplied via the environment
/// (<c>FLOWTHRU_S3_TEST_BUCKET</c> plus optional
/// <c>FLOWTHRU_S3_TEST_SERVICE_URL</c> / <c>FLOWTHRU_S3_TEST_REGION</c>;
/// credentials via the standard AWS chain). Reports Inconclusive when no endpoint
/// is configured, so the default CI flow stays green. A containerized MinIO tier is
/// available via <see cref="Backends.MinioContainerBackend"/>.
/// </para>
/// <para>
/// <strong>The memory ceiling is where #111 actually reproduces.</strong> On an
/// unconstrained runner this test passes — the failure needs a real total-memory
/// cap (managed + native) enforced on the <em>test process</em>. Run it inside a
/// memory-limited container to catch the regression; see
/// <c>tests/extensions/CONTRIBUTING.md</c> ("Constrained-container tier"). Load
/// knobs (env, with defaults): <c>FLOWTHRU_REPRO_OBJECTS</c> (34),
/// <c>FLOWTHRU_REPRO_ROWS</c> (50000), <c>FLOWTHRU_REPRO_CODEC</c> (Snappy),
/// <c>FLOWTHRU_REPRO_MAXPAR</c> (= objects).
/// </para>
/// </remarks>
[TestFixture]
[Category("AwsS3")]
[Category("Integration")]
public class ParquetOverS3ConcurrencyTests
{
  private string _bucket = null!;
  private string? _serviceUrl;
  private string? _region;
  private int _objects;
  private int _rows;
  private CompressionMethod _codec;

  private static string Env(string key, string dflt) =>
    Environment.GetEnvironmentVariable(key) is { Length: > 0 } v ? v : dflt;

  [OneTimeSetUp]
  public async Task SeedEndpoint()
  {
    Assume.That(TestCapabilities.AwsS3.IsAvailable(),
      $"[{TestCapabilities.AwsS3.Name}] {TestCapabilities.AwsS3.MissingMessage}");

    _bucket = Environment.GetEnvironmentVariable("FLOWTHRU_S3_TEST_BUCKET")!;
    _serviceUrl = Environment.GetEnvironmentVariable("FLOWTHRU_S3_TEST_SERVICE_URL");
    _region = Environment.GetEnvironmentVariable("FLOWTHRU_S3_TEST_REGION");
    _objects = int.Parse(Env("FLOWTHRU_REPRO_OBJECTS", "34"));
    _rows = int.Parse(Env("FLOWTHRU_REPRO_ROWS", "50000"));
    _codec = Enum.Parse<CompressionMethod>(Env("FLOWTHRU_REPRO_CODEC", "Snappy"), ignoreCase: true);

    using var admin = new AmazonS3Client(BuildConfig());
    try { await admin.PutBucketAsync(_bucket); }
    catch (AmazonS3Exception) { /* already exists (or a real AWS bucket) */ }

    var bytes = await BuildParquetBytes(_rows, _codec);
    foreach (var i in Enumerable.Range(0, _objects))
    {
      using var body = new MemoryStream(bytes, writable: false);
      await admin.PutObjectAsync(new PutObjectRequest
      {
        BucketName = _bucket,
        Key = ObjectKey(i),
        InputStream = body,
        AutoCloseStream = false,
      });
    }

    TestContext.Out.WriteLine(
      $"[s3-concurrency] seeded {_objects} objects x {_rows} rows, codec={_codec}, "
      + $"{bytes.Length:N0} bytes/object into '{_bucket}'"
      + (_serviceUrl is null ? " (AWS)" : $" ({_serviceUrl})"));
  }

  [Test]
  public async Task ConcurrentParquetLoads_StayConsistent()
  {
    var services = new ServiceCollection();
    services.AddSingleton<IConfiguration>(new ConfigurationBuilder().Build());
    // The real production wiring: gateway from S3Options, credentials via the AWS
    // chain — the path a deployed flow takes, not a hand-built client.
    services.AddFlowthru(b => b.UseS3(s3 =>
    {
      if (!string.IsNullOrWhiteSpace(_serviceUrl)) { s3.ServiceUrl = _serviceUrl; s3.ForcePathStyle = true; }
      if (!string.IsNullOrWhiteSpace(_region)) s3.Region = _region;
    }));
    using var sp = services.BuildServiceProvider();
    var resolver = sp.GetRequiredService<IStorageMediumResolver>();

    var items = Enumerable.Range(0, _objects)
      .Select(i => ItemFactory.Enumerable.Parquet<PqRow>(
        label: $"obj{i}",
        filePath: $"s3://{_bucket}/{ObjectKey(i)}",
        resolver: resolver))
      .ToList();

    var maxPar = int.Parse(Env("FLOWTHRU_REPRO_MAXPAR", _objects.ToString()));
    using var gate = new SemaphoreSlim(maxPar, maxPar);

    async Task<(string Label, int Count, string? Error)> LoadCount(IItem<IEnumerable<PqRow>> item)
    {
      await gate.WaitAsync();
      try
      {
        var result = await item.Load().Run();
        if (result is EffResult<IEnumerable<PqRow>>.Failure f)
        {
          return (item.Label, -1, f.Error.Message);
        }
        return (item.Label, ((EffResult<IEnumerable<PqRow>>.Success)result).Value.Count(), null);
      }
      catch (Exception ex)
      {
        return (item.Label, -1, $"{ex.GetType().Name}: {ex.Message}");
      }
      finally
      {
        gate.Release();
      }
    }

    var results = await Task.WhenAll(items.Select(i => Task.Run(() => LoadCount(i))));

    var failures = results.Where(r => r.Error is not null).ToList();
    var wrongCount = results.Where(r => r.Error is null && r.Count != _rows).ToList();
    TestContext.Out.WriteLine(
      $"[s3-concurrency] maxPar={maxPar} ok={results.Count(r => r.Error is null && r.Count == _rows)}/{_objects} "
      + $"failed={failures.Count} wrongCount={wrongCount.Count}");
    foreach (var f in failures.Take(5))
    {
      TestContext.Out.WriteLine($"[s3-concurrency]   FAIL {f.Label}: {f.Error}");
    }

    Assert.That(failures, Is.Empty,
      $"Concurrent S3 Parquet loads produced {failures.Count} integrity/decode/OOM failures (issue #111).");
    Assert.That(wrongCount, Is.Empty, "Some loads returned the wrong row count (silent corruption).");
  }

  private AmazonS3Config BuildConfig()
  {
    var config = new AmazonS3Config { AuthenticationRegion = _region ?? "us-east-1" };
    if (!string.IsNullOrWhiteSpace(_serviceUrl))
    {
      config.ServiceURL = _serviceUrl;
      config.ForcePathStyle = true;
    }
    return config;
  }

  private static string ObjectKey(int i) => $"flowthru-concurrency/obj{i}.parquet";

  private static async Task<byte[]> BuildParquetBytes(int rows, CompressionMethod codec)
  {
    var serializer = new ParquetFormatSerializer<PqRow>(
      new ParquetItemOptions<PqRow> { CompressionMethod = codec });
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
}

/// <summary>Wide-ish flat schema giving each object real bytes to decode.</summary>
[FlowthruSchema]
public partial record PqRow
{
  public required int Id { get; init; }
  public required string Name { get; init; }
  public required string Category { get; init; }
  public required double V1 { get; init; }
  public required double V2 { get; init; }
  public required double V3 { get; init; }
  public required int Flags { get; init; }
  public required string Payload { get; init; }
}
