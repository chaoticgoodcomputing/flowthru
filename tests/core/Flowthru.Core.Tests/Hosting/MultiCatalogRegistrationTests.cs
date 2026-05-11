using Flowthru.Data.Catalog;
using Flowthru.Flow;
using Flowthru.Hosting;
using Flowthru.Prelude;
using Microsoft.Extensions.DependencyInjection;

namespace Flowthru.Core.Tests.Hosting;

/// <summary>
/// Tests verifying that multiple catalogs compose, resolve, and merge correctly
/// across flows in a single Flowthru host — the distributed-library composition
/// pattern. Ported from the legacy <c>MultiCatalogRegistrationTests</c> (gap #12
/// in the FP-rewrite test-coverage gap analysis), focused on the cases beyond
/// the edge cases already covered by <see cref="MultiCatalogEdgeCaseTests"/>:
/// <list type="bullet">
///   <item>Iteratively-registered catalogs (instance + factory shapes)</item>
///   <item>Iterative mixed with static registration</item>
///   <item>2-catalog and 3-catalog cross-edge DAG resolution</item>
///   <item>Shared-entry object identity across flows</item>
///   <item>Fluent <c>WithDescription</c> after a multi-catalog
///   <c>RegisterFlow</c></item>
/// </list>
/// </summary>
[TestFixture]
[Category("Hosting")]
[Category("MultiCatalog")]
public class MultiCatalogRegistrationTests
{
  // ── Test catalogs ─────────────────────────────────────────────────────

  public sealed class UpstreamCatalog : CatalogAbstract
  {
    public IItem<int> UpstreamInput =>
      CreateItem(() => ItemFactory.Singleton.Memory<int>("up.input"));
    public IItem<int> UpstreamOutput =>
      CreateItem(() => ItemFactory.Singleton.Memory<int>("up.output"));
  }

  public sealed class DownstreamCatalog : CatalogAbstract
  {
    public IItem<int> DownstreamOutput =>
      CreateItem(() => ItemFactory.Singleton.Memory<int>("down.output"));
  }

  public sealed class ThirdCatalog : CatalogAbstract
  {
    public IItem<int> FinalOutput =>
      CreateItem(() => ItemFactory.Singleton.Memory<int>("third.final"));
  }

  // ── Iterative catalog registration (RegisterCatalogs analogue) ───────
  // The legacy `RegisterCatalogs(IEnumerable<CatalogAbstract>)` overload
  // is subsumed in the current API by repeated `RegisterCatalog<T>(factory)`
  // calls — the cases below pin the distinct shapes of that iteration:
  // heterogeneous-type bulk registration, factories that resolve via the
  // host's IServiceProvider, and the mixed static-plus-iterative shape.

  [Test]
  public void RegisterCatalog_Iterative_HeterogeneousTypes_AllResolvable()
  {
    // The "RegisterCatalogs(IEnumerable<T>)" intent: iterate a list of
    // distinct catalog *types* and register each. All three must be
    // independently resolvable by their concrete type after build.
    var upstream = new UpstreamCatalog();
    var downstream = new DownstreamCatalog();
    var third = new ThirdCatalog();
    var catalogs = new CatalogAbstract[] { upstream, downstream, third };

    var services = new ServiceCollection();
    services.AddFlowthru(b =>
    {
      // Loop-registration is the current-API equivalent of the legacy
      // RegisterCatalogs(IEnumerable<>) overload.
      foreach (var catalog in catalogs)
      {
        // Closure over the concrete type — Type.GetType-style dispatch
        // would defeat compile-time wiring, so the test deliberately
        // pattern-matches each instance to its typed factory call.
        switch (catalog)
        {
          case UpstreamCatalog up: b.RegisterCatalog(_ => up); break;
          case DownstreamCatalog down: b.RegisterCatalog(_ => down); break;
          case ThirdCatalog t: b.RegisterCatalog(_ => t); break;
        }
      }
      b.RegisterFlow("noop", () => FlowBuilder.CreateFlow("noop", _ => { }));
    });

    using var sp = services.BuildServiceProvider();

    Assert.That(sp.GetRequiredService<UpstreamCatalog>(), Is.SameAs(upstream));
    Assert.That(sp.GetRequiredService<DownstreamCatalog>(), Is.SameAs(downstream));
    Assert.That(sp.GetRequiredService<ThirdCatalog>(), Is.SameAs(third));
  }

  [Test]
  public void RegisterCatalog_FactoryOverload_ResolvesDependenciesFromServiceProvider()
  {
    // The factory-overload guarantee: a catalog factory receives the
    // host's fully-built IServiceProvider, so the catalog can pull any
    // host-registered dependency at construction time. This is the
    // distributed-library shape (catalogs that depend on connection
    // strings, IConfiguration sections, options patterns, etc.).
    var services = new ServiceCollection();
    services.AddSingleton<ICatalogConfig>(new CatalogConfig(Multiplier: 11));
    services.AddFlowthru(b =>
    {
      b.RegisterCatalog(sp =>
      {
        var config = sp.GetRequiredService<ICatalogConfig>();
        return new ConfigurableCatalog(config);
      });
      b.RegisterFlow("noop", () => FlowBuilder.CreateFlow("noop", _ => { }));
    });

    using var sp = services.BuildServiceProvider();
    var resolved = sp.GetRequiredService<ConfigurableCatalog>();

    Assert.That(resolved.Multiplier, Is.EqualTo(11),
      "Factory-registered catalog must receive the host's IServiceProvider "
      + "and resolve dependencies through it.");
  }

  [Test]
  public void RegisterCatalog_MixedDirectAndFactoryShapes_BothResolvable()
  {
    // The mixed shape: one catalog registered with a closing-over
    // factory (no SP usage) plus another whose factory pulls a host
    // dependency. Both must be independently resolvable by concrete
    // type — registration shape doesn't change the resolution contract.
    var direct = new UpstreamCatalog();

    var services = new ServiceCollection();
    services.AddSingleton<ICatalogConfig>(new CatalogConfig(Multiplier: 3));
    services.AddFlowthru(b =>
    {
      b.RegisterCatalog(_ => direct);
      b.RegisterCatalog(sp => new ConfigurableCatalog(sp.GetRequiredService<ICatalogConfig>()));
      b.RegisterFlow("noop", () => FlowBuilder.CreateFlow("noop", _ => { }));
    });

    using var sp = services.BuildServiceProvider();

    Assert.That(sp.GetRequiredService<UpstreamCatalog>(), Is.SameAs(direct),
      "Direct-factory registration must produce the captured instance.");
    Assert.That(sp.GetRequiredService<ConfigurableCatalog>().Multiplier, Is.EqualTo(3),
      "SP-resolving factory must be invoked with the host's provider.");
  }

  // Test helpers for the factory-overload + mixed-registration tests.
  public interface ICatalogConfig
  {
    int Multiplier { get; }
  }

  public sealed record CatalogConfig(int Multiplier) : ICatalogConfig;

  public sealed class ConfigurableCatalog : CatalogAbstract
  {
    public int Multiplier { get; }
    public ConfigurableCatalog(ICatalogConfig config)
    {
      Multiplier = config.Multiplier;
    }
  }

  // ── 2-catalog DAG cross-edge resolution ───────────────────────────────

  [Test]
  public async Task RegisterFlow_TwoCatalogs_DagResolvesCrossCatalogEdge()
  {
    // The Bridge flow reads up.UpstreamOutput, which is written by the
    // Upstream flow. The merged DAG must see a single item instance
    // (object identity from the shared catalog) and schedule Bridge
    // after Upstream.
    var upstream = new UpstreamCatalog();
    var downstream = new DownstreamCatalog();

    await upstream.UpstreamInput.Save(7).Run();

    var services = new ServiceCollection();
    services.AddFlowthru(b =>
    {
      b.RegisterCatalog(_ => upstream);
      b.RegisterCatalog(_ => downstream);

      b.RegisterFlow<UpstreamCatalog>("Upstream", up =>
        FlowBuilder.CreateFlow("Upstream", p =>
          p.AddStep<int, int>("Process", x => x + 1, up.UpstreamInput, up.UpstreamOutput)
        )
      );

      b.RegisterFlow<UpstreamCatalog, DownstreamCatalog>("Bridge", (up, down) =>
        FlowBuilder.CreateFlow("Bridge", p =>
          p.AddStep<int, int>("Bridge", x => x * 2, up.UpstreamOutput, down.DownstreamOutput)
        )
      );
    });

    using var sp = services.BuildServiceProvider();
    var service = sp.GetRequiredService<IFlowthruService>();

    // Run the entire merged DAG — both flows must execute and the
    // cross-flow edge must carry data end-to-end.
    var result = await service.RunAsync();
    Assert.That(result.IsSuccess, Is.True,
      "Merged DAG across two catalogs must execute cleanly when the producing flow runs first.");
    Assert.That(result.StepResults.Select(r => r.StepLabel),
      Is.EquivalentTo(new[] { "Process", "Bridge" }));

    var final = await downstream.DownstreamOutput.Load().Run();
    Assert.That(((EffResult<int>.Success)final).Value, Is.EqualTo(16),
      "End-to-end: (7 + 1) * 2 = 16 — proves the cross-catalog edge carried data through the merged DAG.");
  }

  // ── 3-catalog DAG cross-edge resolution ───────────────────────────────

  [Test]
  public async Task RegisterFlow_ThreeCatalogs_DagResolvesFullChain()
  {
    // Three catalogs chained: Upstream → Downstream → Third. The merged
    // DAG must topologically sort all three flows in dependency order.
    var upstream = new UpstreamCatalog();
    var downstream = new DownstreamCatalog();
    var third = new ThirdCatalog();

    await upstream.UpstreamInput.Save(3).Run();

    var services = new ServiceCollection();
    services.AddFlowthru(b =>
    {
      b.RegisterCatalog(_ => upstream);
      b.RegisterCatalog(_ => downstream);
      b.RegisterCatalog(_ => third);

      b.RegisterFlow<UpstreamCatalog>("Upstream", up =>
        FlowBuilder.CreateFlow("Upstream", p =>
          p.AddStep<int, int>("Process", x => x + 1, up.UpstreamInput, up.UpstreamOutput)
        )
      );

      b.RegisterFlow<UpstreamCatalog, DownstreamCatalog>("Bridge", (up, down) =>
        FlowBuilder.CreateFlow("Bridge", p =>
          p.AddStep<int, int>("Bridge", x => x * 2, up.UpstreamOutput, down.DownstreamOutput)
        )
      );

      // Three-catalog flow: takes Upstream + Downstream + Third, but
      // reads from downstream (which transitively depends on upstream)
      // and writes to third.
      b.RegisterFlow<UpstreamCatalog, DownstreamCatalog, ThirdCatalog>(
        "Merge",
        (_, down, t) =>
          FlowBuilder.CreateFlow("Merge", p =>
            p.AddStep<int, int>("Merge", x => x + 10, down.DownstreamOutput, t.FinalOutput)
          )
      );
    });

    using var sp = services.BuildServiceProvider();
    var service = sp.GetRequiredService<IFlowthruService>();

    var result = await service.RunAsync();
    Assert.That(result.IsSuccess, Is.True);
    Assert.That(result.StepResults, Has.Count.EqualTo(3),
      "All three flows must execute when the merged DAG runs.");

    var finalEff = await third.FinalOutput.Load().Run();
    Assert.That(((EffResult<int>.Success)finalEff).Value, Is.EqualTo(18),
      "End-to-end: ((3 + 1) * 2) + 10 = 18 — proves the full 3-catalog dependency chain resolved correctly.");
  }

  // ── Shared-entry object identity ──────────────────────────────────────

  [Test]
  public void RegisterFlow_SharedEntryPreservesObjectIdentity_AcrossFlows()
  {
    // up.UpstreamOutput is referenced by both Upstream (as an output)
    // and Bridge (as an input). Both flow factories receive the SAME
    // catalog instance from DI, so the IItem objects are ReferenceEquals.
    // This is the critical property that lets DependencyAnalyzer resolve
    // the cross-flow edge as a single DAG vertex.
    var upstream = new UpstreamCatalog();
    var downstream = new DownstreamCatalog();

    BuiltFlow? upstreamBuilt = null;
    BuiltFlow? bridgeBuilt = null;

    var services = new ServiceCollection();
    services.AddFlowthru(b =>
    {
      b.RegisterCatalog(_ => upstream);
      b.RegisterCatalog(_ => downstream);

      b.RegisterFlow<UpstreamCatalog>("Upstream", up =>
      {
        upstreamBuilt = FlowBuilder.CreateFlow("Upstream", p =>
          p.AddStep<int, int>("Process", x => x, up.UpstreamInput, up.UpstreamOutput)
        );
        return upstreamBuilt;
      });

      b.RegisterFlow<UpstreamCatalog, DownstreamCatalog>("Bridge", (up, down) =>
      {
        bridgeBuilt = FlowBuilder.CreateFlow("Bridge", p =>
          p.AddStep<int, int>("Bridge", x => x, up.UpstreamOutput, down.DownstreamOutput)
        );
        return bridgeBuilt;
      });
    });

    using var sp = services.BuildServiceProvider();
    // Touch the service so flow factories are invoked.
    _ = sp.GetRequiredService<IFlowthruService>().RegisteredFlowLabels;

    Assert.That(upstreamBuilt, Is.Not.Null);
    Assert.That(bridgeBuilt, Is.Not.Null);

    var producerOutput = upstreamBuilt!.Steps.Single(s => s.Label == "Process").Outputs[0];
    var consumerInput = bridgeBuilt!.Steps.Single(s => s.Label == "Bridge").Inputs[0];

    Assert.That(producerOutput, Is.SameAs(consumerInput),
      "The shared catalog item must be the SAME object instance across flow factories — "
      + "DependencyAnalyzer relies on reference equality to resolve cross-flow edges."
    );
  }

  // ── Fluent description after multi-catalog registration ──────────────

  [Test]
  public void RegisterFlow_TwoCatalogs_WithDescription_AttachesToRegistration()
  {
    IFlowRegistration? registration = null;

    var services = new ServiceCollection();
    services.AddFlowthru(b =>
    {
      b.RegisterCatalog(_ => new UpstreamCatalog());
      b.RegisterCatalog(_ => new DownstreamCatalog());

      registration = b
        .RegisterFlow<UpstreamCatalog, DownstreamCatalog>("Bridge", (up, down) =>
          FlowBuilder.CreateFlow("Bridge", p =>
            p.AddStep<int, int>("B", x => x, up.UpstreamOutput, down.DownstreamOutput)
          )
        )
        .WithDescription("Bridges upstream → downstream domain");
    });

    using var sp = services.BuildServiceProvider();
    _ = sp.GetRequiredService<IFlowthruService>();

    Assert.That(registration, Is.Not.Null);
    Assert.That(registration!.Label, Is.EqualTo("Bridge"));
    // WithDescription returns the same registration object — fluent chain.
    Assert.That(((FlowthruServiceBuilder.FlowRegistration)registration).Description,
      Is.EqualTo("Bridges upstream → downstream domain"));
  }
}
