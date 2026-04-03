using Flowthru.Data;
using Flowthru.Flows;
using Flowthru.Services;
using Flowthru.Tests.Fixtures.TestCatalogs;
using Flowthru.Tests.Fixtures.TestSteps;
using Microsoft.Extensions.DependencyInjection;

namespace Flowthru.Tests.Services;

/// <summary>
/// Tests verifying that multiple catalogs can be registered, resolved, and used across
/// pipelines in a single Flowthru runtime — the distributed library composition pattern.
/// </summary>
[TestFixture]
[Category("Services")]
[Category("MultiCatalog")]
public class MultiCatalogRegistrationTests
{
  // ─────────────────────────────────────────────────────────────────────────
  // Registration
  // ─────────────────────────────────────────────────────────────────────────

  [Test]
  public void RegisterCatalog_MultipleCatalogs_AllResolvableByConcreteType()
  {
    // Arrange
    var services = new ServiceCollection();
    services.AddLogging();

    // Act
    services.AddFlowthru(flowthru =>
    {
      flowthru.RegisterCatalog<UpstreamCatalog>();
      flowthru.RegisterCatalog<DownstreamCatalog>();
      flowthru.RegisterFlow(
        "Upstream",
        (UpstreamCatalog up) =>
          FlowBuilder.CreateFlow(b =>
            b.AddStep("U1", PassthroughStep.Create(), up.UpstreamInput, up.UpstreamOutput)
          )
      );
    });

    var sp = services.BuildServiceProvider();

    // Assert — each catalog type is independently resolvable
    var upstream = sp.GetService<UpstreamCatalog>();
    var downstream = sp.GetService<DownstreamCatalog>();

    Assert.That(upstream, Is.Not.Null);
    Assert.That(downstream, Is.Not.Null);
  }

  [Test]
  public void IFlowthruService_Catalogs_ContainsAllRegisteredCatalogs()
  {
    // Arrange
    var services = new ServiceCollection();
    services.AddLogging();

    services.AddFlowthru(flowthru =>
    {
      flowthru.RegisterCatalog<UpstreamCatalog>();
      flowthru.RegisterCatalog<DownstreamCatalog>();
      flowthru.RegisterFlow(
        "Upstream",
        (UpstreamCatalog up) =>
          FlowBuilder.CreateFlow(b =>
            b.AddStep("U1", PassthroughStep.Create(), up.UpstreamInput, up.UpstreamOutput)
          )
      );
    });

    var sp = services.BuildServiceProvider();

    // Act
    var service = sp.GetRequiredService<IFlowthruService>();

    // Assert
    Assert.That(service.Catalogs, Has.Count.EqualTo(2));
    Assert.That(service.Catalogs.Any(c => c is UpstreamCatalog), Is.True);
    Assert.That(service.Catalogs.Any(c => c is DownstreamCatalog), Is.True);
  }

  // ─────────────────────────────────────────────────────────────────────────
  // 2-catalog pipeline registration
  // ─────────────────────────────────────────────────────────────────────────

  [Test]
  public void RegisterFlow_TwoCatalogs_FlowIsRegistered()
  {
    // Arrange
    var services = new ServiceCollection();
    services.AddLogging();

    services.AddFlowthru(flowthru =>
    {
      flowthru.RegisterCatalog<UpstreamCatalog>();
      flowthru.RegisterCatalog<DownstreamCatalog>();

      // Single-catalog upstream pipeline
      flowthru.RegisterFlow(
        "Upstream",
        (UpstreamCatalog up) =>
          FlowBuilder.CreateFlow(b =>
            b.AddStep("Process", PassthroughStep.Create(), up.UpstreamInput, up.UpstreamOutput)
          )
      );

      // 2-catalog bridge pipeline: reads from upstream, writes to downstream
      flowthru.RegisterFlow(
        "Bridge",
        (UpstreamCatalog up, DownstreamCatalog down) =>
          FlowBuilder.CreateFlow(b =>
            b.AddStep("Bridge", PassthroughStep.Create(), up.UpstreamOutput, down.DownstreamOutput)
          )
      );
    });

    var sp = services.BuildServiceProvider();
    var service = sp.GetRequiredService<IFlowthruService>();

    // Assert
    Assert.That(service.FlowNames, Does.Contain("Upstream"));
    Assert.That(service.FlowNames, Does.Contain("Bridge"));
  }

  [Test]
  public async Task RegisterFlow_TwoCatalogs_DagResolvesCrossCatalogEdge()
  {
    // Arrange — the Bridge pipeline reads up.UpstreamOutput, which is written by the
    // Upstream pipeline. When merged, the DAG must see a single IItem instance
    // (object identity) and schedule Bridge after Upstream.
    var upstream = new UpstreamCatalog();
    var downstream = new DownstreamCatalog();

    var testData = new[]
    {
      new TestData
      {
        Id = 1,
        Name = "cross-catalog",
        Value = 1.0,
      },
    };
    await upstream.UpstreamInput.Save(testData).Run();

    var services = new ServiceCollection();
    services.AddLogging();

    services.AddFlowthru(flowthru =>
    {
      flowthru.RegisterCatalog(upstream);
      flowthru.RegisterCatalog(downstream);

      flowthru.RegisterFlow(
        "Upstream",
        (UpstreamCatalog up) =>
          FlowBuilder.CreateFlow(b =>
            b.AddStep("Process", PassthroughStep.Create(), up.UpstreamInput, up.UpstreamOutput)
          )
      );

      flowthru.RegisterFlow(
        "Bridge",
        (UpstreamCatalog up, DownstreamCatalog down) =>
          FlowBuilder.CreateFlow(b =>
            b.AddStep("Bridge", PassthroughStep.Create(), up.UpstreamOutput, down.DownstreamOutput)
          )
      );
    });

    var sp = services.BuildServiceProvider();
    var service = sp.GetRequiredService<IFlowthruService>();

    // Act
    var result = await service.ExecuteFlowAsync(exportMetadata: false);

    // Assert — both pipelines executed and data flowed through
    Assert.That(result.Success, Is.True);
    Assert.That(result.StepResults, Has.Count.EqualTo(2));

    var output = await downstream.DownstreamOutput.Load().Run();
    Assert.That(output, Is.Not.Null);
    Assert.That(output!.Count(), Is.EqualTo(1));
    Assert.That(output!.First().Name, Is.EqualTo("cross-catalog"));
  }

  // ─────────────────────────────────────────────────────────────────────────
  // 3-catalog pipeline registration
  // ─────────────────────────────────────────────────────────────────────────

  [Test]
  public async Task RegisterFlow_ThreeCatalogs_DagResolvesFullChain()
  {
    // Arrange — three catalogs: Upstream → Downstream → Third
    var upstream = new UpstreamCatalog();
    var downstream = new DownstreamCatalog();
    var third = new ThirdCatalog();

    var testData = new[]
    {
      new TestData
      {
        Id = 1,
        Name = "three-catalog",
        Value = 2.0,
      },
    };
    await upstream.UpstreamInput.Save(testData).Run();

    var services = new ServiceCollection();
    services.AddLogging();

    services.AddFlowthru(flowthru =>
    {
      flowthru.RegisterCatalog(upstream);
      flowthru.RegisterCatalog(downstream);
      flowthru.RegisterCatalog(third);

      flowthru.RegisterFlow(
        "Upstream",
        (UpstreamCatalog up) =>
          FlowBuilder.CreateFlow(b =>
            b.AddStep("Process", PassthroughStep.Create(), up.UpstreamInput, up.UpstreamOutput)
          )
      );

      flowthru.RegisterFlow(
        "Bridge",
        (UpstreamCatalog up, DownstreamCatalog down) =>
          FlowBuilder.CreateFlow(b =>
            b.AddStep("Bridge", PassthroughStep.Create(), up.UpstreamOutput, down.DownstreamOutput)
          )
      );

      // 3-catalog pipeline reads from both upstream and downstream, writes to third
      flowthru.RegisterFlow(
        "Merge",
        (UpstreamCatalog up, DownstreamCatalog down, ThirdCatalog t) =>
          FlowBuilder.CreateFlow(b =>
            // Use downstream output (which depends on upstream) as the final step input
            b.AddStep("Merge", PassthroughStep.Create(), down.DownstreamOutput, t.FinalOutput)
          )
      );
    });

    var sp = services.BuildServiceProvider();
    var service = sp.GetRequiredService<IFlowthruService>();

    // Act
    var result = await service.ExecuteFlowAsync(exportMetadata: false);

    // Assert — all three pipelines executed in dependency order
    Assert.That(result.Success, Is.True);
    Assert.That(result.StepResults, Has.Count.EqualTo(3));

    var finalOutput = await third.FinalOutput.Load().Run();
    Assert.That(finalOutput!.First().Name, Is.EqualTo("three-catalog"));
  }

  // ─────────────────────────────────────────────────────────────────────────
  // WithDescription chaining after multi-catalog registration
  // ─────────────────────────────────────────────────────────────────────────

  [Test]
  public void RegisterFlow_TwoCatalogs_WithDescription_SetsDescription()
  {
    // Arrange
    var services = new ServiceCollection();
    services.AddLogging();

    services.AddFlowthru(flowthru =>
    {
      flowthru.RegisterCatalog<UpstreamCatalog>();
      flowthru.RegisterCatalog<DownstreamCatalog>();

      flowthru
        .RegisterFlow(
          "Bridge",
          (UpstreamCatalog up, DownstreamCatalog down) =>
            FlowBuilder.CreateFlow(b =>
              b.AddStep("B", PassthroughStep.Create(), up.UpstreamOutput, down.DownstreamOutput)
            )
        )
        .WithDescription("Bridges upstream to downstream domain");
    });

    var sp = services.BuildServiceProvider();
    var service = sp.GetRequiredService<IFlowthruService>();
    var meta = service.GetFlowMetadata("Bridge");

    // Assert
    Assert.That(meta.Description, Is.EqualTo("Bridges upstream to downstream domain"));
  }

  // ─────────────────────────────────────────────────────────────────────────
  // DAG identity guarantee
  // ─────────────────────────────────────────────────────────────────────────

  [Test]
  public void RegisterFlow_TwoCatalogs_SharedEntryPreservesObjectIdentity()
  {
    // Arrange — upstream.UpstreamOutput is referenced by both the Upstream pipeline
    // (as an output) and the Bridge pipeline (as an input). Both pipelines receive the
    // SAME catalog instance from DI, so the entries are ReferenceEquals. This is the
    // critical property that enables correct DAG edge resolution.
    var upstream = new UpstreamCatalog();
    var downstream = new DownstreamCatalog();

    Flow? upstreamFlow = null;
    Flow? bridgeFlow = null;

    var services = new ServiceCollection();
    services.AddLogging();

    services.AddFlowthru(flowthru =>
    {
      flowthru.RegisterCatalog(upstream);
      flowthru.RegisterCatalog(downstream);

      flowthru.RegisterFlow(
        "Upstream",
        (UpstreamCatalog up) =>
        {
          upstreamFlow = FlowBuilder.CreateFlow(b =>
            b.AddStep("P", PassthroughStep.Create(), up.UpstreamInput, up.UpstreamOutput)
          );
          return upstreamFlow;
        }
      );

      flowthru.RegisterFlow(
        "Bridge",
        (UpstreamCatalog up, DownstreamCatalog down) =>
        {
          bridgeFlow = FlowBuilder.CreateFlow(b =>
            b.AddStep("B", PassthroughStep.Create(), up.UpstreamOutput, down.DownstreamOutput)
          );
          return bridgeFlow;
        }
      );
    });

    // Trigger service build (which invokes the pipeline factories)
    var sp = services.BuildServiceProvider();
    _ = sp.GetRequiredService<IFlowthruService>();

    // Assert — UpstreamOutput entry is the same instance in both pipelines
    var upstreamOutputInProducer = upstreamFlow!.Steps.First(n => n.Label == "P").Outputs[0];
    var upstreamOutputInConsumer = bridgeFlow!.Steps.First(n => n.Label == "B").Inputs[0];

    Assert.That(
      upstreamOutputInProducer,
      Is.SameAs(upstreamOutputInConsumer),
      "The shared entry must be the same object instance for the DAG to resolve the dependency edge."
    );
  }

  // ─────────────────────────────────────────────────────────────────────────
  // RegisterCatalogs — dynamic/iterative catalog registration
  // ─────────────────────────────────────────────────────────────────────────

  [Test]
  public void RegisterCatalogs_InstanceOverload_AllCatalogsVisibleToService()
  {
    // Arrange — simulate the fan-out pattern: N catalogs built in a loop,
    // registered via RegisterCatalogs rather than individual RegisterCatalog calls.
    var shards = new[] { new ShardCatalog("X"), new ShardCatalog("Y"), new ShardCatalog("Z") };

    var services = new ServiceCollection();
    services.AddLogging();

    services.AddFlowthru(flowthru =>
    {
      flowthru.RegisterCatalogs(shards);

      flowthru.RegisterFlows(_ => new Dictionary<string, Flow>
      {
        // Minimal pipeline so AddFlowthru has something to inject
        ["noop"] = FlowBuilder.CreateFlow(_ => { }),
      });
    });

    var sp = services.BuildServiceProvider();
    var service = sp.GetRequiredService<IFlowthruService>();

    // Assert — all three dynamically-registered catalogs appear in IFlowthruService.Catalogs
    Assert.That(service.Catalogs, Has.Count.EqualTo(3));
    Assert.That(
      service.Catalogs.OfType<ShardCatalog>().Count(),
      Is.EqualTo(3),
      "All three ShardCatalog instances should be present"
    );
  }

  [Test]
  public void RegisterCatalogs_MixedWithRegisterCatalog_AllCatalogsVisibleToService()
  {
    // Arrange — one static catalog registered via RegisterCatalog, N via RegisterCatalogs.
    var staticCatalog = new UpstreamCatalog();
    var shards = new[] { new ShardCatalog("P"), new ShardCatalog("Q") };

    var services = new ServiceCollection();
    services.AddLogging();

    services.AddFlowthru(flowthru =>
    {
      flowthru.RegisterCatalog(staticCatalog);
      flowthru.RegisterCatalogs(shards);

      flowthru.RegisterFlows(_ => new Dictionary<string, Flow>
      {
        ["noop"] = FlowBuilder.CreateFlow(_ => { }),
      });
    });

    var sp = services.BuildServiceProvider();
    var service = sp.GetRequiredService<IFlowthruService>();

    // Assert — static + dynamic catalogs both present
    Assert.That(service.Catalogs, Has.Count.EqualTo(3));
    Assert.That(
      service.Catalogs.OfType<UpstreamCatalog>().Count(),
      Is.EqualTo(1),
      "Static RegisterCatalog entry should be present"
    );
    Assert.That(
      service.Catalogs.OfType<ShardCatalog>().Count(),
      Is.EqualTo(2),
      "Dynamic RegisterCatalogs entries should be present"
    );
  }

  [Test]
  public void RegisterCatalogs_FactoryOverload_AllCatalogsVisibleToService()
  {
    // Arrange — catalogs produced by a factory that receives the service provider.
    var services = new ServiceCollection();
    services.AddLogging();

    services.AddFlowthru(flowthru =>
    {
      flowthru.RegisterCatalogs(_ =>
        new CatalogAbstract[] { new ShardCatalog("factory_1"), new ShardCatalog("factory_2") }
      );

      flowthru.RegisterFlows(_ => new Dictionary<string, Flow>
      {
        ["noop"] = FlowBuilder.CreateFlow(_ => { }),
      });
    });

    var sp = services.BuildServiceProvider();
    var service = sp.GetRequiredService<IFlowthruService>();

    Assert.That(service.Catalogs, Has.Count.EqualTo(2));
    Assert.That(service.Catalogs.OfType<ShardCatalog>().Count(), Is.EqualTo(2));
  }
}
