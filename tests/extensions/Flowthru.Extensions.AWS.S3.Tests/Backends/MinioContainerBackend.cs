using Amazon.Runtime;
using Amazon.S3;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using Flowthru.Data.Storage.S3;
using Flowthru.Extensions.AWS.S3.Tests.Support;
using Flowthru.Tests.Kits.Prelude;

namespace Flowthru.Extensions.AWS.S3.Tests.Backends;

/// <summary>
/// Backend for <see cref="Contract.S3GatewayLaws{TBackend}"/> targeting a
/// <c>Testcontainers</c>-managed MinIO container — a real S3-API server, closer
/// to production than the offline <see cref="LocalFileS3Backend"/> stub and
/// requiring no external bucket like <see cref="LiveS3Backend"/>. The container
/// starts in <see cref="InitializeAsync"/> (after the capability gate clears) and
/// is disposed at fixture teardown; each <see cref="CreateResource"/> hands out a
/// unique key prefix on the shared bucket so tests never observe each other.
/// </summary>
/// <remarks>
/// <para>
/// Declares <see cref="TestCapabilities.Docker"/> as required — when Docker is
/// unavailable the laws kit's <c>OneTimeSetUp</c> yields Inconclusive and the
/// container is never started. Tagged <c>[Category("RequiresDocker")]</c> so a
/// Docker-equipped CI tier can target it explicitly; the capability gate is the
/// load-bearing check. Mirrors <c>PostgresContainerBackend</c>.
/// </para>
/// </remarks>
[Category("RequiresDocker")]
public sealed class MinioContainerBackend : IS3GatewayBackend
{
  private const string AccessKey = "minioadmin";
  private const string SecretKey = "minioadmin";
  private const string Bucket = "flowthru-laws";

  private IContainer? _container;
  private IAmazonS3? _client;
  private int _counter;

  public IReadOnlyList<TestCapability> RequiredCapabilities { get; } = [TestCapabilities.Docker];

  public async Task InitializeAsync()
  {
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

    var endpoint = $"http://{_container.Hostname}:{_container.GetMappedPublicPort(9000)}";
    _client = new AmazonS3Client(
      new BasicAWSCredentials(AccessKey, SecretKey),
      new AmazonS3Config
      {
        ServiceURL = endpoint,
        ForcePathStyle = true,
        AuthenticationRegion = "us-east-1",
      });
    await _client.PutBucketAsync(Bucket);
  }

  public S3GatewayContext CreateResource()
  {
    if (_client is null)
    {
      throw new InvalidOperationException(
        "MinioContainerBackend.CreateResource() called before InitializeAsync(). "
          + "The laws kit's OneTimeSetUp wires this automatically.");
    }

    var n = Interlocked.Increment(ref _counter);
    return new S3GatewayContext(
      Gateway: new AmazonS3Gateway(_client),
      Bucket: Bucket,
      KeyPrefix: $"laws/{n}/{Guid.NewGuid():N}/");
  }

  public async Task Cleanup()
  {
    _client?.Dispose();
    _client = null;
    if (_container is not null)
    {
      await _container.DisposeAsync();
      _container = null;
    }
  }
}
