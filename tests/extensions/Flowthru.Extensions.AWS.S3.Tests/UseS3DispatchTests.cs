using Flowthru.Data.Storage;
using Flowthru.Data.Storage.S3;
using Flowthru.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Flowthru.Extensions.AWS.S3.Tests;

/// <summary>
/// End-to-end DI integration: a registered S3 provider makes the host-resolved
/// <see cref="IStorageMediumResolver"/> dispatch <c>s3://</c> URIs to an
/// <see cref="S3StorageMedium"/>; bare paths and <c>file://</c> still resolve via
/// the built-in <see cref="FileStorageMedium"/>.
/// </summary>
[TestFixture]
[Category("AwsS3")]
public class UseS3DispatchTests
{
  private static ServiceProvider BuildProvider(Action<IFlowthruBuilder> configure)
  {
    var services = new ServiceCollection();
    services.AddSingleton<IConfiguration>(new ConfigurationBuilder().Build());
    services.AddFlowthru(configure);
    return services.BuildServiceProvider();
  }

  [Test]
  public void UseLocalS3_RegistersProvider_ResolverDispatchesS3Uri()
  {
    using var sp = BuildProvider(b => b.UseLocalS3(Path.GetTempPath()));
    var resolver = sp.GetRequiredService<IStorageMediumResolver>();

    var medium = resolver.Resolve("s3://my-bucket/data.csv");
    Assert.That(medium, Is.InstanceOf<S3StorageMedium>(),
      "Resolver should dispatch s3:// URIs to S3StorageMedium after UseLocalS3.");
  }

  [Test]
  public void UseS3_ProductionGateway_ResolverDispatchesS3Uri()
  {
    using var sp = BuildProvider(b => b.UseS3(s3 => s3.Region = "us-east-1"));
    var resolver = sp.GetRequiredService<IStorageMediumResolver>();

    var medium = resolver.Resolve("s3://my-bucket/data.parquet");
    Assert.That(medium, Is.InstanceOf<S3StorageMedium>(),
      "The AWS-backed UseS3() path should also dispatch s3:// URIs to S3StorageMedium.");
  }

  [Test]
  public void UseLocalS3_BarePath_StillResolvesToFileStorageMedium()
  {
    using var sp = BuildProvider(b => b.UseLocalS3(Path.GetTempPath()));
    var resolver = sp.GetRequiredService<IStorageMediumResolver>();

    var medium = resolver.Resolve("/tmp/data.csv");
    Assert.That(medium, Is.InstanceOf<FileStorageMedium>(),
      "UseLocalS3 should not affect bare-path dispatch.");
  }

  [Test]
  public void UseLocalS3_FileScheme_StillResolvesToFileStorageMedium()
  {
    using var sp = BuildProvider(b => b.UseLocalS3(Path.GetTempPath()));
    var resolver = sp.GetRequiredService<IStorageMediumResolver>();

    var medium = resolver.Resolve("file:///tmp/data.csv");
    Assert.That(medium, Is.InstanceOf<FileStorageMedium>(),
      "file:// URIs should still resolve to the file medium.");
  }
}
