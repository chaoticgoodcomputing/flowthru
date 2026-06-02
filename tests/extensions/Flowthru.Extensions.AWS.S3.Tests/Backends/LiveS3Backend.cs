using Amazon;
using Amazon.S3;
using Amazon.S3.Model;
using Flowthru.Data.Storage.S3;
using Flowthru.Extensions.AWS.S3.Tests.Support;
using Flowthru.Tests.Kits.Prelude;

namespace Flowthru.Extensions.AWS.S3.Tests.Backends;

/// <summary>
/// Live backend for <see cref="Contract.S3GatewayLaws{TBackend}"/>: drives a real
/// <see cref="AmazonS3Gateway"/> against an S3 (or S3-compatible) bucket supplied
/// via the environment. Gated on <see cref="TestCapabilities.AwsS3"/>, so it
/// reports Inconclusive — never fails — when no test bucket is configured, keeping
/// the default CI flow green. This is the "or a real bucket" tier the issue's
/// acceptance criteria call for; the offline <see cref="LocalFileS3Backend"/>
/// runs the same laws on every PR and is what makes the shipped stub a verified
/// S3 stand-in.
/// </summary>
/// <remarks>
/// Reads <c>FLOWTHRU_S3_TEST_BUCKET</c> (required) plus optional
/// <c>FLOWTHRU_S3_TEST_SERVICE_URL</c> (LocalStack/MinIO endpoint) and
/// <c>FLOWTHRU_S3_TEST_REGION</c>. Credentials resolve through the standard AWS
/// chain. Each <see cref="CreateResource"/> hands out a unique key prefix on the
/// shared bucket; <see cref="Cleanup"/> lists and deletes every object under the
/// prefixes this fixture used (best effort) and disposes the client.
/// </remarks>
[Category("RequiresAwsS3")]
public sealed class LiveS3Backend : IS3GatewayBackend
{
  private IAmazonS3? _client;
  private string _bucket = null!;
  private readonly List<string> _prefixes = new();
  private readonly object _gate = new();
  private int _counter;

  public IReadOnlyList<TestCapability> RequiredCapabilities { get; } = [TestCapabilities.AwsS3];

  public Task InitializeAsync()
  {
    _bucket = Environment.GetEnvironmentVariable("FLOWTHRU_S3_TEST_BUCKET")
      ?? throw new InvalidOperationException("FLOWTHRU_S3_TEST_BUCKET must be set for the live S3 backend.");

    var serviceUrl = Environment.GetEnvironmentVariable("FLOWTHRU_S3_TEST_SERVICE_URL");
    var region = Environment.GetEnvironmentVariable("FLOWTHRU_S3_TEST_REGION");

    var config = new AmazonS3Config();
    if (!string.IsNullOrWhiteSpace(region))
    {
      config.RegionEndpoint = RegionEndpoint.GetBySystemName(region);
    }
    if (!string.IsNullOrWhiteSpace(serviceUrl))
    {
      config.ServiceURL = serviceUrl;
      config.ForcePathStyle = true; // LocalStack / MinIO
    }

    _client = new AmazonS3Client(config);
    return Task.CompletedTask;
  }

  public S3GatewayContext CreateResource()
  {
    if (_client is null)
    {
      throw new InvalidOperationException(
        "LiveS3Backend.CreateResource() called before InitializeAsync().");
    }

    var n = Interlocked.Increment(ref _counter);
    var prefix = $"flowthru-laws/{n}/{Guid.NewGuid():N}/";
    lock (_gate)
    {
      _prefixes.Add(prefix);
    }

    return new S3GatewayContext(
      Gateway: new AmazonS3Gateway(_client),
      Bucket: _bucket,
      KeyPrefix: prefix);
  }

  public async Task Cleanup()
  {
    if (_client is null) return;

    string[] prefixes;
    lock (_gate)
    {
      prefixes = _prefixes.ToArray();
      _prefixes.Clear();
    }

    foreach (var prefix in prefixes)
    {
      try
      {
        var listed = await _client.ListObjectsV2Async(
          new ListObjectsV2Request { BucketName = _bucket, Prefix = prefix });
        foreach (var obj in listed.S3Objects ?? [])
        {
          try { await _client.DeleteObjectAsync(_bucket, obj.Key); }
          catch { /* best effort */ }
        }
      }
      catch { /* best effort */ }
    }

    _client.Dispose();
    _client = null;
  }
}
