using Flowthru.Data.Catalog;
using Flowthru.Data.Configuration;
using Flowthru.Flow;
using Flowthru.Hosting;
using Flowthru.Prelude;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Flowthru.Core.Tests.Hosting;

/// <summary>
/// Tests for the <c>FlowthruServiceBuilder.UseConfiguration(IConfiguration)</c>
/// host integration surface. Per Phase 5 RFC: registering an
/// <see cref="IConfiguration"/> through this method makes it resolvable
/// from DI so <see cref="ConfigurationItem{T}"/> can be constructed inside
/// catalog factories that take the configuration as a constructor
/// parameter.
/// </summary>
[TestFixture]
[Category("Hosting")]
[Category("Configuration")]
public class UseConfigurationTests
{
  public sealed class FlowConfigPayload
  {
    public string? Region { get; set; }
    public int MaxConcurrency { get; set; }
  }

  public sealed class ConfigCatalog : CatalogAbstract
  {
    private readonly IConfiguration _configuration;

    public ConfigCatalog(IConfiguration configuration)
    {
      _configuration = configuration;
    }

    public IItem<FlowConfigPayload> FlowConfig =>
      CreateItem(() =>
        Item.Of<FlowConfigPayload>("flow-config")
          .FromConfiguration(_configuration)
          .AtSection("FlowConfig")
          .Build());
  }

  // ── UseConfiguration core wiring ──────────────────────────────────────

  [Test]
  public void UseConfiguration_RegistersIConfigurationAsSingleton()
  {
    // The host calls UseConfiguration so downstream catalogs can take
    // IConfiguration as a constructor parameter and build
    // ConfigurationItems from it. DI must surface the same instance
    // (singleton) every time.
    var config = new ConfigurationBuilder()
      .AddInMemoryCollection(new Dictionary<string, string?>
      {
        ["FlowConfig:Region"] = "eu-west-1",
      })
      .Build();

    var services = new ServiceCollection();
    services.AddFlowthru(b =>
    {
      b.UseConfiguration(config);
      b.RegisterCatalog(sp => new ConfigCatalog(sp.GetRequiredService<IConfiguration>()));
      b.RegisterFlow("noop", () => FlowBuilder.CreateFlow("noop", _ => { }));
    });

    using var sp = services.BuildServiceProvider();
    var resolved = sp.GetService<IConfiguration>();
    Assert.That(resolved, Is.SameAs(config),
      "UseConfiguration must register the supplied IConfiguration as a singleton, "
      + "resolvable by DI without wrapping.");
  }

  [Test]
  public async Task UseConfiguration_ConfigurationItemUsableInCatalog()
  {
    var config = new ConfigurationBuilder()
      .AddInMemoryCollection(new Dictionary<string, string?>
      {
        ["FlowConfig:Region"] = "eu-west-1",
        ["FlowConfig:MaxConcurrency"] = "4",
      })
      .Build();

    var services = new ServiceCollection();
    services.AddFlowthru(b =>
    {
      b.UseConfiguration(config);
      b.RegisterCatalog(sp => new ConfigCatalog(sp.GetRequiredService<IConfiguration>()));
      b.RegisterFlow("noop", () => FlowBuilder.CreateFlow("noop", _ => { }));
    });

    using var sp = services.BuildServiceProvider();
    var catalog = sp.GetRequiredService<ConfigCatalog>();

    var configItem = catalog.FlowConfig;
    Assert.That(configItem, Is.InstanceOf<IReadOnlyItem<FlowConfigPayload>>(),
      "The catalog-built ConfigurationItem must surface the read-only marker.");

    var loaded = await configItem.Load().Run();
    Assert.That(loaded, Is.InstanceOf<EffResult<FlowConfigPayload>.Success>());
    var value = ((EffResult<FlowConfigPayload>.Success)loaded).Value;
    Assert.That(value.Region, Is.EqualTo("eu-west-1"));
    Assert.That(value.MaxConcurrency, Is.EqualTo(4));
  }

  [Test]
  public void UseConfiguration_NullConfiguration_Throws()
  {
    var services = new ServiceCollection();
    Assert.Throws<ArgumentNullException>(() =>
      services.AddFlowthru(b => b.UseConfiguration(null!))
    );
  }

  [Test]
  public void UseConfiguration_LastCallWins()
  {
    // Subsequent calls override earlier registrations — matches the
    // ServiceCollection.AddSingleton replace-semantics expected for
    // a singleton DI registration. Authors who layer environments
    // should compose them into a single IConfiguration before calling.
    var first = new ConfigurationBuilder().Build();
    var second = new ConfigurationBuilder()
      .AddInMemoryCollection(new Dictionary<string, string?>
      {
        ["x"] = "1",
      })
      .Build();

    var services = new ServiceCollection();
    services.AddFlowthru(b =>
    {
      b.UseConfiguration(first);
      b.UseConfiguration(second);
      b.RegisterFlow("noop", () => FlowBuilder.CreateFlow("noop", _ => { }));
    });

    using var sp = services.BuildServiceProvider();
    var resolved = sp.GetService<IConfiguration>();
    Assert.That(resolved, Is.SameAs(second),
      "The most-recent UseConfiguration call must win — standard DI replace semantics.");
  }
}
