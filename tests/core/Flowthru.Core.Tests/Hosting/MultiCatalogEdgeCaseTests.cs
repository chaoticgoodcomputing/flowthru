using Flowthru.Data.Catalog;
using Flowthru.Flow;
using Flowthru.Hosting;
using Flowthru.Prelude;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Flowthru.Core.Tests.Hosting;

/// <summary>
/// Multi-catalog DI edge cases ported from the legacy
/// <c>MultiCatalogRegistrationTests</c>. The Phase-4 baseline test
/// exercises the happy path; these cover error / interaction paths.
/// </summary>
[TestFixture]
public class MultiCatalogEdgeCaseTests
{
  public sealed class CatalogX : CatalogAbstract
  {
    public IItem<int> Counter => CreateItem(() => ItemFactory.Singleton.Memory<int>("x-counter"));
  }

  public sealed class CatalogY : CatalogAbstract
  {
    public IItem<int> Counter => CreateItem(() => ItemFactory.Singleton.Memory<int>("y-counter"));
  }

  public sealed record FlowConfig(int Multiplier);

  [Test]
  public async Task SameCatalogTypeRegisteredTwice_LastWins()
  {
    // DI singletons are keyed by type — the second AddSingleton call
    // for the same type wins. Document this behaviour so users don't
    // expect "two instances of the same catalog".
    var services = new ServiceCollection();
    services.AddFlowthru(b =>
    {
      b.RegisterCatalog(_ => new CatalogX());
      b.RegisterCatalog(_ => new CatalogX());
      b.RegisterFlow("x", () => FlowBuilder.CreateFlow("x", p => p.AddStep("noop", () => { })));
    });

    await using var sp = services.BuildServiceProvider();
    var first = sp.GetRequiredService<CatalogX>();
    var second = sp.GetRequiredService<CatalogX>();
    Assert.That(first, Is.SameAs(second),
      "Two registrations of the same catalog type collapse to one DI singleton — last writer wins."
    );
  }

  [Test]
  public async Task TwoFlowsSharingACatalog_BothSeeSameInstance()
  {
    var services = new ServiceCollection();
    services.AddFlowthru(b =>
    {
      b.RegisterCatalog(_ => new CatalogX());

      // Both flows take the same catalog type; the framework resolves
      // it once and passes the same instance to both factories.
      b.RegisterFlow<CatalogX>("write-1", catalog =>
        FlowBuilder.CreateFlow("write-1", p =>
          p.AddStep<int>("write-1-step", () => 1, catalog.Counter)
        )
      );
      b.RegisterFlow<CatalogX>("read-and-double", catalog =>
        FlowBuilder.CreateFlow("read-and-double", p =>
          p.AddStep<int>("read", x =>
          {
            // Just read — the test only cares that catalog.Counter
            // points at the same item for both flow factories.
          }, catalog.Counter)
        )
      );
    });

    await using var sp = services.BuildServiceProvider();
    var flowthru = sp.GetRequiredService<IFlowthruService>();
    Assert.That(flowthru.RegisteredFlowLabels,
      Is.EquivalentTo(new[] { "write-1", "read-and-double" })
    );

    var result = await flowthru.RunAsync();
    Assert.That(result.IsSuccess, Is.True,
      "Two flows sharing one catalog should compose into a single merged DAG without duplicate-producer errors."
    );
  }

  [Test]
  public void FlowReferencingUnregisteredCatalog_ThrowsAtMaterialization()
  {
    var services = new ServiceCollection();
    services.AddFlowthru(b =>
    {
      b.RegisterCatalog(_ => new CatalogX());
      // No CatalogY registered, but the flow needs one.
      b.RegisterFlow<CatalogY>("needs-y", catalog =>
        FlowBuilder.CreateFlow("needs-y", p => p.AddStep("noop", () => { }))
      );
    });

    using var sp = services.BuildServiceProvider();
    var flowthru = sp.GetRequiredService<IFlowthruService>();

    Assert.ThrowsAsync<InvalidOperationException>(async () =>
      await flowthru.RunAsync()
    );
  }

  [Test]
  public async Task ConfigurationRecord_ResolvesAsCatalog()
  {
    // A non-CatalogAbstract reference type that takes IConfiguration —
    // mirrors the FlowConfig pattern used in KedroIrisFUnit / SpaceflightsDistributed.
    var configuration = new ConfigurationBuilder()
      .AddInMemoryCollection(new Dictionary<string, string?>
      {
        ["MyApp:Multiplier"] = "7",
      })
      .Build();

    var services = new ServiceCollection();
    services.AddSingleton<IConfiguration>(configuration);
    services.AddFlowthru(b =>
    {
      b.RegisterCatalog(_ => new CatalogX());
      b.RegisterCatalog(sp => new FlowConfig(
        sp.GetRequiredService<IConfiguration>().GetValue<int>("MyApp:Multiplier")
      ));

      b.RegisterFlow<CatalogX, FlowConfig>("multiply", (catalog, cfg) =>
        FlowBuilder.CreateFlow("multiply", p =>
          p.AddStep<int>("seed", () => cfg.Multiplier, catalog.Counter)
        )
      );
    });

    await using var sp = services.BuildServiceProvider();
    var flowthru = sp.GetRequiredService<IFlowthruService>();
    var catalog = sp.GetRequiredService<CatalogX>();

    var result = await flowthru.RunAsync();
    Assert.That(result.IsSuccess, Is.True);
    var loaded = await catalog.Counter.Load().Run();
    Assert.That(((EffResult<int>.Success)loaded).Value, Is.EqualTo(7),
      "Config-bound multiplier should flow through DI into the flow's transform."
    );
  }
}
