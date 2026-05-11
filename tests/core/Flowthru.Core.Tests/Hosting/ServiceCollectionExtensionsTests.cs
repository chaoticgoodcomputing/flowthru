using Flowthru.Data.Catalog;
using Flowthru.Diagnostics;
using Flowthru.Flow;
using Flowthru.Hosting;
using Flowthru.Prelude;
using Microsoft.Extensions.DependencyInjection;

namespace Flowthru.Core.Tests.Hosting;

/// <summary>
/// Tests verifying correct behaviour of <see cref="ServiceCollectionExtensions.AddFlowthru"/>
/// and the <see cref="IFlowthruBuilder"/> registration surface — ported from
/// the legacy <c>ServiceCollectionExtensionsTests</c> (gap #11 in the
/// FP-rewrite test-coverage gap analysis). End-to-end execution paths are
/// covered by <see cref="FlowthruServiceTests"/>; these tests pin the
/// registration-extensions surface directly: which DI services are
/// registered, how overloads compose, and the null-arg contract.
/// </summary>
[TestFixture]
[Category("Hosting")]
[Category("DependencyInjection")]
public class ServiceCollectionExtensionsTests
{
  public sealed class TestCatalog : CatalogAbstract
  {
    public IItem<int> Input => CreateItem(() => ItemFactory.Singleton.Memory<int>("sce-input"));
    public IItem<int> Output => CreateItem(() => ItemFactory.Singleton.Memory<int>("sce-output"));
  }

  // ── AddFlowthru core wiring ───────────────────────────────────────────

  [Test]
  public void AddFlowthru_RegistersService()
  {
    var services = new ServiceCollection();

    services.AddFlowthru(b =>
    {
      b.RegisterCatalog(_ => new TestCatalog());
      b.RegisterFlow("noop", () => FlowBuilder.CreateFlow("noop", _ => { }));
    });

    using var sp = services.BuildServiceProvider();
    var service = sp.GetService<IFlowthruService>();
    Assert.That(service, Is.Not.Null,
      "AddFlowthru must register IFlowthruService in the host's DI container.");
  }

  [Test]
  public void AddFlowthru_RegisterCatalog_FactoryInstanceIsResolvableByType()
  {
    var catalog = new TestCatalog();
    var services = new ServiceCollection();

    services.AddFlowthru(b =>
    {
      b.RegisterCatalog(_ => catalog);
      b.RegisterFlow("noop", () => FlowBuilder.CreateFlow("noop", _ => { }));
    });

    using var sp = services.BuildServiceProvider();
    var resolved = sp.GetService<TestCatalog>();

    Assert.That(resolved, Is.Not.Null);
    Assert.That(resolved, Is.SameAs(catalog),
      "RegisterCatalog's factory should be wired as the singleton — calling code must get back the same instance.");
  }

  [Test]
  public void AddFlowthru_RegisterCatalog_WithFactory_CreatesCatalogLazily()
  {
    var calls = 0;
    var services = new ServiceCollection();

    services.AddFlowthru(b =>
    {
      b.RegisterCatalog(_ =>
      {
        calls++;
        return new TestCatalog();
      });
      b.RegisterFlow("noop", () => FlowBuilder.CreateFlow("noop", _ => { }));
    });

    using var sp = services.BuildServiceProvider();
    Assert.That(calls, Is.EqualTo(0),
      "Factory must not run during AddFlowthru — registration is description-only.");

    var first = sp.GetRequiredService<TestCatalog>();
    var second = sp.GetRequiredService<TestCatalog>();

    Assert.That(calls, Is.EqualTo(1), "Factory should run exactly once even on repeated resolution.");
    Assert.That(first, Is.SameAs(second), "Catalog is registered as a DI singleton.");
  }

  [Test]
  public void AddFlowthru_RegisterFlow_ExposesLabelOnService()
  {
    var services = new ServiceCollection();

    services.AddFlowthru(b =>
    {
      b.RegisterCatalog(_ => new TestCatalog());
      b.RegisterFlow<TestCatalog>("main", catalog =>
        FlowBuilder.CreateFlow("main", p =>
          p.AddStep<int, int>("identity", x => x, catalog.Input, catalog.Output)
        )
      );
    });

    using var sp = services.BuildServiceProvider();
    var service = sp.GetRequiredService<IFlowthruService>();

    Assert.That(service.RegisteredFlowLabels, Has.Count.EqualTo(1));
    Assert.That(service.RegisteredFlowLabels, Does.Contain("main"));
  }

  // ── Null-arg validation ───────────────────────────────────────────────

  [Test]
  public void AddFlowthru_WithNullServices_ThrowsArgumentNullException()
  {
    IServiceCollection services = null!;
    Assert.Throws<ArgumentNullException>(() => services.AddFlowthru(_ => { }));
  }

  [Test]
  public void AddFlowthru_WithNullConfigure_ThrowsArgumentNullException()
  {
    var services = new ServiceCollection();
    Assert.Throws<ArgumentNullException>(() => services.AddFlowthru(configure: null!));
  }

  [Test]
  public void RegisterCatalog_WithNullFactory_ThrowsArgumentNullException()
  {
    var services = new ServiceCollection();

    Assert.Throws<ArgumentNullException>(() =>
      services.AddFlowthru(b =>
      {
        b.RegisterCatalog<TestCatalog>(factory: null!);
      })
    );
  }

  [Test]
  public void RegisterFlow_WithNullFactory_ThrowsArgumentNullException()
  {
    var services = new ServiceCollection();

    Assert.Throws<ArgumentNullException>(() =>
      services.AddFlowthru(b =>
      {
        b.RegisterCatalog(_ => new TestCatalog());
        b.RegisterFlow("oops", factory: (Func<BuiltFlow>)null!);
      })
    );
  }

  [Test]
  public void RegisterFlow_WithEmptyLabel_ThrowsArgumentException()
  {
    var services = new ServiceCollection();

    Assert.Throws<ArgumentException>(() =>
      services.AddFlowthru(b =>
      {
        b.RegisterCatalog(_ => new TestCatalog());
        b.RegisterFlow("", () => FlowBuilder.CreateFlow("x", _ => { }));
      })
    );
  }

  // ── ConfigureMetadata + service singleton + multi-flow merging ────────

  [Test]
  public void AddFlowthru_WithMetadata_ConfiguresMetadataBuilder()
  {
    var services = new ServiceCollection();
    var stubProvider = new StubMetadataProvider();

    services.AddFlowthru(b =>
    {
      b.RegisterCatalog(_ => new TestCatalog());
      b.RegisterFlow("noop", () => FlowBuilder.CreateFlow("noop", _ => { }));
      b.ConfigureMetadata(meta => meta.AddProvider(stubProvider));
    });

    using var sp = services.BuildServiceProvider();
    var metadataBuilder = sp.GetService<FlowthruServiceBuilder>()?.MetadataBuilder;

    Assert.That(metadataBuilder, Is.Not.Null,
      "AddFlowthru should make the FlowthruServiceBuilder (and thus the metadata builder) DI-resolvable.");
    Assert.That(metadataBuilder!.PreRunProviders, Has.Count.EqualTo(1));
    Assert.That(metadataBuilder.PreRunProviders[0], Is.SameAs(stubProvider));
  }

  [Test]
  public void AddFlowthru_ServiceIsSingleton()
  {
    var services = new ServiceCollection();

    services.AddFlowthru(b =>
    {
      b.RegisterCatalog(_ => new TestCatalog());
      b.RegisterFlow("noop", () => FlowBuilder.CreateFlow("noop", _ => { }));
    });

    using var sp = services.BuildServiceProvider();
    var first = sp.GetRequiredService<IFlowthruService>();
    var second = sp.GetRequiredService<IFlowthruService>();

    Assert.That(first, Is.SameAs(second),
      "IFlowthruService must be registered as a singleton — every resolution returns the same instance.");
  }

  [Test]
  public void RegisterFlow_MultipleFlows_AllAppearInRegisteredLabels()
  {
    var services = new ServiceCollection();
    var stage1 = ItemFactory.Singleton.Memory<int>("rf-stage1");
    var stage2 = ItemFactory.Singleton.Memory<int>("rf-stage2");
    var stage3 = ItemFactory.Singleton.Memory<int>("rf-stage3");

    services.AddFlowthru(b =>
    {
      b.RegisterCatalog(_ => new TestCatalog());

      // Each RegisterFlow call adds an independent label to the merged DAG.
      b.RegisterFlow("first", () =>
        FlowBuilder.CreateFlow("first", p =>
          p.AddStep<int, int>("first-step", x => x + 1, stage1, stage2)
        )
      );

      b.RegisterFlow("second", () =>
        FlowBuilder.CreateFlow("second", p =>
          p.AddStep<int, int>("second-step", x => x * 2, stage2, stage3)
        )
      );
    });

    using var sp = services.BuildServiceProvider();
    var service = sp.GetRequiredService<IFlowthruService>();

    Assert.That(service.RegisteredFlowLabels, Has.Count.EqualTo(2));
    Assert.That(service.RegisteredFlowLabels, Is.EquivalentTo(new[] { "first", "second" }),
      "Successive RegisterFlow calls accumulate — both labels must appear on the service.");
  }

  // ── Stub provider used by ConfigureMetadata test ─────────────────────

  /// <summary>Minimal <see cref="IMetadataProvider"/> for registration-only assertions.</summary>
  private sealed class StubMetadataProvider : IMetadataProvider
  {
    public string ProviderId => "stub";
    public FlowIO<FlowUnit> Emit(FlowMetadataContext ctx) => FlowIO.Pure(FlowUnit.Default);
  }
}
