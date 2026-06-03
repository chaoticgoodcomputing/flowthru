using System.Net;
using System.Text;
using Flowthru.Data.Catalog;
using Flowthru.Data.Storage;
using Flowthru.Data.Storage.Http;
using Flowthru.Flow;
using Flowthru.Hosting;
using Flowthru.Prelude;
using Flowthru.Step;
using Flowthru.Validation.Runtime;
using Flowthru.Validation.Runtime.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Flowthru.Extensions.Http.Tests;

/// <summary>
/// #104 acceptance (HTTP half): HTTP mediums are unbounded by default, but
/// an opt-in <c>HttpOptions.MaxConcurrentRequestsPerHost</c> cap declares
/// the endpoint as a conflict resource. The medium surfaces it through
/// <c>ComposedStorageAdapter</c> (the S3-style medium pattern), so a flow
/// can throttle concurrent calls to one host. (ADR-0019.)
/// </summary>
[TestFixture]
[Category("Http")]
public sealed class HttpConflictGatingTests
{
  private static readonly Uri Endpoint = new("https://throttled.example/data.json");

  private static readonly IServiceProfileProvider Gated =
    new CompositeServiceProfileProvider(new IServiceProfileContributor[]
    {
      new HttpEndpointProfileContributor(),
    });

  private static readonly IServiceProfileProvider Ungated =
    new CompositeServiceProfileProvider(Array.Empty<IServiceProfileContributor>());

  // ── Medium declares the dependency only when capped ──────────────────────

  [Test]
  public void Medium_WithPerHostCap_DeclaresEndpointDependency()
  {
    using var client = new HttpClient(new FakeHandler(HttpStatusCode.OK, "[]"));
    var medium = new HttpStorageMedium(Endpoint, client, maxConcurrentRequestsPerHost: 2);

    var dep = medium.ServiceDependencies
      .OfType<ServiceDependency.External>()
      .Select(e => e.Cause)
      .OfType<HttpEndpointDependency>()
      .SingleOrDefault();

    Assert.That(dep, Is.Not.Null, "A capped HTTP medium declares its endpoint as a conflict resource.");
    Assert.That(dep!.Authority, Is.EqualTo("https://throttled.example"),
      "The conflict key is the scheme + host + port — shared by every item on that host.");
    Assert.That(dep.MaxConcurrency, Is.EqualTo(2));
    Assert.That(dep.Category, Is.EqualTo("http"));
  }

  [Test]
  public void Medium_Unbounded_DeclaresNoDependency()
  {
    using var client = new HttpClient(new FakeHandler(HttpStatusCode.OK, "[]"));
    var medium = new HttpStorageMedium(Endpoint, client);
    Assert.That(medium.ServiceDependencies, Is.Empty,
      "Default HTTP is unbounded and parallel-safe — no conflict declaration.");
  }

  [Test]
  public void CachedMedium_WithPerHostCap_DeclaresEndpointDependency()
  {
    using var client = new HttpClient(new FakeHandler(HttpStatusCode.OK, "[]"));
    var dir = Path.Combine(Path.GetTempPath(), $"flowthru-http-cache-{Guid.NewGuid():N}");
    try
    {
      var medium = new CachedHttpStorageMedium(
        Endpoint, client, dir, TimeSpan.FromMinutes(5), maxConcurrentRequestsPerHost: 1);
      Assert.That(
        medium.ServiceDependencies.OfType<ServiceDependency.External>()
          .Any(e => e.Cause is HttpEndpointDependency),
        Is.True,
        "The cached medium gates identically to the plain one.");
    }
    finally
    {
      if (Directory.Exists(dir)) try { Directory.Delete(dir, true); } catch { /* best effort */ }
    }
  }

  // ── Contributor / registration ───────────────────────────────────────────

  [Test]
  public void Contributor_MapsEndpointDependency_AndStaysSilentOtherwise()
  {
    var dep = new ServiceDependency.External(new HttpEndpointDependency("https://h", 3));
    var profile = new HttpEndpointProfileContributor().Contribute(dep);

    Assert.That(profile, Is.Not.Null);
    Assert.That(profile!.Capacity, Is.EqualTo(3));
    Assert.That(profile.ReadCapacity, Is.EqualTo(3), "A rate limit caps every call — reads and writes alike.");

    Assert.That(new HttpEndpointProfileContributor().Contribute(ServiceDependency.Of<IDisposable>()), Is.Null);
  }

  [Test]
  public void UseHttp_RegistersProfileContributor()
  {
    var services = new ServiceCollection();
    services.AddSingleton<IConfiguration>(new ConfigurationBuilder().Build());
    new FlowthruServiceBuilder(services).UseHttp();

    Assert.That(
      services.Any(d => d.ServiceType == typeof(IServiceProfileContributor)
        && d.ImplementationType == typeof(HttpEndpointProfileContributor)),
      Is.True,
      "UseHttp() must register the endpoint profile contributor.");
  }

  // ── Composed item surfaces the dependency through the real resolver path ──

  [Test]
  public async Task HttpBackedItem_WithCap_SurfacesEndpointDependency()
  {
    using var sp = BuildHost(maxConcurrentRequestsPerHost: 1, new FakeHandler(HttpStatusCode.OK, "[]"));
    var resolver = sp.GetRequiredService<IStorageMediumResolver>();
    var catalog = new ThrottledHttpCatalog(resolver);

    var dep = catalog.RowsA.ServiceDependencies
      .OfType<ServiceDependency.External>()
      .Select(e => e.Cause)
      .OfType<HttpEndpointDependency>()
      .SingleOrDefault();

    Assert.That(dep, Is.Not.Null,
      "A JSON-over-HTTP item must surface the medium's endpoint dependency through ComposedStorageAdapter.");
    Assert.That(dep!.MaxConcurrency, Is.EqualTo(1));
    await Task.CompletedTask;
  }

  // ── End-to-end: capped host serializes; uncapped parallelizes ────────────

  [Test]
  public async Task ConcurrentReadsFromOneHost_WithCap_Serialize()
  {
    var maxConcurrent = await RunTwoReadsAsync(maxConcurrentRequestsPerHost: 1, provider: Gated);
    Assert.That(maxConcurrent, Is.EqualTo(1),
      "Two items reading one host capped to 1 must serialize their HTTP calls at Parallelism=4.");
  }

  [Test]
  public async Task ConcurrentReadsFromOneHost_Uncapped_RunConcurrently()
  {
    var maxConcurrent = await RunTwoReadsAsync(maxConcurrentRequestsPerHost: int.MaxValue, provider: Gated);
    Assert.That(maxConcurrent, Is.EqualTo(2),
      "Uncapped (default) HTTP reads parallelize — confirms the cap is opt-in and the harness sees overlap.");
  }

  // ── Harness ──────────────────────────────────────────────────────────────

  private static ServiceProvider BuildHost(int maxConcurrentRequestsPerHost, HttpMessageHandler handler)
  {
    var services = new ServiceCollection();
    services.AddSingleton<IConfiguration>(new ConfigurationBuilder().Build());
    services.AddSingleton<IStorageMediumProvider>(
      new HttpStorageMediumProvider(new HttpClient(handler), cache: null, maxConcurrentRequestsPerHost));
    services.AddFlowthru(b =>
      b.RegisterCatalog(sp => new ThrottledHttpCatalog(sp.GetRequiredService<IStorageMediumResolver>())));
    return services.BuildServiceProvider();
  }

  private static async Task<int> RunTwoReadsAsync(int maxConcurrentRequestsPerHost, IServiceProfileProvider provider)
  {
    var (recordEntry, recordExit, max) = MakeConcurrencyMeter();
    using var sp = BuildHost(maxConcurrentRequestsPerHost, new ConcurrencyRecordingHandler(recordEntry, recordExit));
    var catalog = new ThrottledHttpCatalog(sp.GetRequiredService<IStorageMediumResolver>());

    var outA = ItemFactory.Singleton.Memory<int>($"http-out-a-{Guid.NewGuid():N}");
    var outB = ItemFactory.Singleton.Memory<int>($"http-out-b-{Guid.NewGuid():N}");

    Func<IEnumerable<HttpBackedRow>, FlowIO<int>> transform = _ => FlowIO.Pure(0);

    IStepNode Step(string label, IItem<IEnumerable<HttpBackedRow>> input, IItem<int> output) =>
      new Step<IEnumerable<HttpBackedRow>, int>(
        label, transform, new IItem[] { input }, new IItem[] { output },
        loadInputs: () => input.Load(), saveOutputs: v => output.Save(v));

    var flow = FlowBuilder.CreateFlow("http-conflict", b =>
    {
      b.Add(Step("http-step-a", catalog.RowsA, outA));
      b.Add(Step("http-step-b", catalog.RowsB, outB));
    });

    var result = await new ParallelFlowScheduler(profiles: provider)
      .ExecuteAsync(flow, new ExecutionOptions { Parallelism = 4 });

    Assert.That(result.IsSuccess, Is.True, "both read steps should succeed");
    return max();
  }

  private static (Action Entry, Action Exit, Func<int> Max) MakeConcurrencyMeter()
  {
    var running = 0;
    var max = 0;
    var gate = new object();
    void Entry()
    {
      var now = Interlocked.Increment(ref running);
      lock (gate) max = Math.Max(max, now);
    }
    void Exit() => Interlocked.Decrement(ref running);
    int Max() { lock (gate) return max; }
    return (Entry, Exit, Max);
  }

  /// <summary>Catalog with two JSON-over-HTTP items on the same host (one conflict key).</summary>
  private sealed class ThrottledHttpCatalog : CatalogAbstract
  {
    public ThrottledHttpCatalog(IStorageMediumResolver resolver) : base(resolver: resolver) { }

    public IItem<IEnumerable<HttpBackedRow>> RowsA =>
      CreateItem(() => Item.Of<IEnumerable<HttpBackedRow>>("RowsA")
        .Json().AtPath("https://throttled.example/a.json").Build());

    public IItem<IEnumerable<HttpBackedRow>> RowsB =>
      CreateItem(() => Item.Of<IEnumerable<HttpBackedRow>>("RowsB")
        .Json().AtPath("https://throttled.example/b.json").Build());
  }

  /// <summary>Handler that records peak concurrent in-flight requests and delays so overlap is observable.</summary>
  private sealed class ConcurrencyRecordingHandler : HttpMessageHandler
  {
    private readonly Action _entry;
    private readonly Action _exit;
    public ConcurrencyRecordingHandler(Action entry, Action exit) { _entry = entry; _exit = exit; }

    protected override async Task<HttpResponseMessage> SendAsync(
      HttpRequestMessage request, CancellationToken cancellationToken)
    {
      _entry();
      try
      {
        await Task.Delay(60, cancellationToken).ConfigureAwait(false);
        return new HttpResponseMessage(HttpStatusCode.OK)
        {
          Content = new StringContent("[]", Encoding.UTF8),
        };
      }
      finally
      {
        _exit();
      }
    }
  }
}
