using System.Net;
using Flowthru.Data.Storage;
using Flowthru.Data.Storage.Http;
using Flowthru.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Flowthru.Extensions.Http.Tests;

/// <summary>
/// End-to-end DI integration: <c>UseHttp()</c> registers the
/// provider; the host-resolved <see cref="IStorageMediumResolver"/>
/// dispatches HTTP-scheme URIs through it; bare paths still resolve
/// via the built-in <see cref="FileStorageMediumProvider"/>.
/// </summary>
[TestFixture]
[Category("Http")]
public class UseHttpDispatchTests
{
  [Test]
  public void UseHttp_RegistersProvider_ResolverDispatchesHttpUri()
  {
    var services = new ServiceCollection();
    services.AddSingleton<IConfiguration>(new ConfigurationBuilder().Build());
    services.AddFlowthru(b => b.UseHttp());

    var sp = services.BuildServiceProvider();
    var resolver = sp.GetRequiredService<IStorageMediumResolver>();

    var medium = resolver.Resolve("https://example.com/data.csv");
    Assert.That(medium, Is.InstanceOf<HttpStorageMedium>(),
      "Resolver should dispatch https URIs to HttpStorageMedium after UseHttp.");
  }

  [Test]
  public void UseHttp_HttpScheme_AlsoDispatched()
  {
    var services = new ServiceCollection();
    services.AddSingleton<IConfiguration>(new ConfigurationBuilder().Build());
    services.AddFlowthru(b => b.UseHttp());
    var sp = services.BuildServiceProvider();
    var resolver = sp.GetRequiredService<IStorageMediumResolver>();

    var medium = resolver.Resolve("http://example.com/data.csv");
    Assert.That(medium, Is.InstanceOf<HttpStorageMedium>());
  }

  [Test]
  public void UseHttp_BarePath_StillResolvesToFileStorageMedium()
  {
    var services = new ServiceCollection();
    services.AddSingleton<IConfiguration>(new ConfigurationBuilder().Build());
    services.AddFlowthru(b => b.UseHttp());
    var sp = services.BuildServiceProvider();
    var resolver = sp.GetRequiredService<IStorageMediumResolver>();

    var medium = resolver.Resolve("/tmp/data.csv");
    Assert.That(medium, Is.InstanceOf<FileStorageMedium>(),
      "UseHttp should not affect bare-path or file:// dispatch.");
  }

  [Test]
  public void UseHttp_WithCacheConfigured_DispatchesToCachedMedium()
  {
    var cacheDir = Path.Combine(Path.GetTempPath(), $"flowthru-http-cache-cfg-{Guid.NewGuid():N}");
    try
    {
      var services = new ServiceCollection();
      services.AddSingleton<IConfiguration>(new ConfigurationBuilder().Build());
      services.AddFlowthru(b => b.UseHttp(http =>
      {
        http.Cache = new HttpCacheOptions { Directory = cacheDir };
      }));
      var sp = services.BuildServiceProvider();
      var resolver = sp.GetRequiredService<IStorageMediumResolver>();

      var medium = resolver.Resolve("https://example.com/data.csv");
      Assert.That(medium, Is.InstanceOf<CachedHttpStorageMedium>(),
        "When HttpOptions.Cache is set, resolver should yield a CachedHttpStorageMedium.");
    }
    finally
    {
      if (Directory.Exists(cacheDir))
      {
        try { Directory.Delete(cacheDir, recursive: true); } catch { /* best effort */ }
      }
    }
  }
}
