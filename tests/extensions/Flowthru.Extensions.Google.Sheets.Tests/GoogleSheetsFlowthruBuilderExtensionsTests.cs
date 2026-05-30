using Flowthru.Data.Storage.Sheets;
using Flowthru.Data.Storage.Sheets.InMemory;
using Flowthru.Hosting;
using Flowthru.Prelude;
using Google.Apis.Sheets.v4;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Flowthru.Extensions.Google.Sheets.Tests;

/// <summary>
/// DI integration for <c>AddGoogleSheets</c> / <c>UseGoogleSheets</c>: the
/// registered <see cref="ISheetsGateway"/> resolves from the host provider, the
/// gateway choice is swappable, the retry decorator wraps it by default, and the
/// factory-mode production gateway is wired into the engine's resource lifecycle
/// as an <see cref="IFlowResourceProvider"/>.
/// </summary>
[TestFixture]
public sealed class GoogleSheetsFlowthruBuilderExtensionsTests
{
  private static IServiceProvider BuildHost(Action<IFlowthruBuilder> configure)
  {
    var services = new ServiceCollection();
    services.AddSingleton<IConfiguration>(new ConfigurationBuilder().Build());
    services.AddFlowthru(configure);
    return services.BuildServiceProvider();
  }

  [Test]
  public void AddGoogleSheets_WrapsGatewayInRetryDecorator_ByDefault()
  {
    var gateway = new InMemorySheetsGateway();

    var sp = BuildHost(b => b.AddGoogleSheets(gateway));

    var resolved = sp.GetRequiredService<ISheetsGateway>();
    Assert.That(resolved, Is.InstanceOf<RetryingSheetsGateway>(),
      "production gets backoff automatically: the gateway is wrapped by default");
    Assert.That(((RetryingSheetsGateway)resolved).Inner, Is.SameAs(gateway),
      "the wrapped decorator delegates to the supplied gateway");
  }

  [Test]
  public void AddGoogleSheetsWithoutRetry_ResolvesRawGateway()
  {
    var gateway = new InMemorySheetsGateway();

    var sp = BuildHost(b => b.AddGoogleSheetsWithoutRetry(gateway));

    var resolved = sp.GetRequiredService<ISheetsGateway>();
    Assert.That(resolved, Is.SameAs(gateway),
      "the opt-out path registers the gateway directly, no decorator");
  }

  [Test]
  public void AddGoogleSheets_GatewayChoiceIsSwappable()
  {
    // Register the offline gateway in place of the production one — the swap
    // point that backs the example and tests with no catalog change. It is
    // reachable through the retry decorator that wraps it.
    var sp = BuildHost(b => b.AddGoogleSheets(new InMemorySheetsGateway()));

    var resolved = (RetryingSheetsGateway)sp.GetRequiredService<ISheetsGateway>();
    Assert.That(resolved.Inner, Is.InstanceOf<InMemorySheetsGateway>());
  }

  [Test]
  public void AddGoogleSheets_WithInjectedService_WrapsServiceGateway()
  {
    using var service = new SheetsService();

    var sp = BuildHost(b => b.AddGoogleSheets(service));

    var resolved = (RetryingSheetsGateway)sp.GetRequiredService<ISheetsGateway>();
    Assert.That(resolved.Inner, Is.InstanceOf<SheetsServiceGateway>());
  }

  [Test]
  public void AddGoogleSheets_WithServiceFactory_WiresFlowResourceProvider()
  {
    // Factory mode: the gateway owns a per-run client lifecycle. The retry
    // decorator forwards the inner gateway's FlowResource, so the engine still
    // discovers an IFlowResourceProvider to bracket the run.
    var sp = BuildHost(b => b.AddGoogleSheets(() => new SheetsService()));

    var providers = sp.GetServices<IFlowResourceProvider>().ToList();
    Assert.That(providers, Has.Some.InstanceOf<RetryingSheetsGateway>(),
      "the wrapping decorator is registered as IFlowResourceProvider");

    var gateway = (RetryingSheetsGateway)sp.GetRequiredService<ISheetsGateway>();
    Assert.That(gateway.Inner, Is.InstanceOf<SheetsServiceGateway>());
    Assert.That(gateway.FlowResource, Is.Not.Null,
      "factory-mode FlowResource is forwarded through the decorator to bracket per run");
  }

  [Test]
  public void AddGoogleSheets_WithInjectedService_ExposesNoFlowResource()
  {
    using var service = new SheetsService();

    var sp = BuildHost(b => b.AddGoogleSheets(service));

    // Injected mode: the container owns the client; nothing to bracket. The
    // decorator forwards the inner gateway's null FlowResource (a no-op).
    var gateway = (RetryingSheetsGateway)sp.GetRequiredService<ISheetsGateway>();
    Assert.That(gateway.FlowResource, Is.Null);
  }

  [Test]
  public void UseGoogleSheets_IsAliasForAddGoogleSheets()
  {
    var gateway = new InMemorySheetsGateway();

    var sp = BuildHost(b => b.UseGoogleSheets(gateway));

    var resolved = (RetryingSheetsGateway)sp.GetRequiredService<ISheetsGateway>();
    Assert.That(resolved.Inner, Is.SameAs(gateway));
  }
}
