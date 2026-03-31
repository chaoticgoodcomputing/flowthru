using Flowthru.Data;
using Flowthru.Pipelines;
using Flowthru.Services;
using Flowthru.Tests.Fixtures.TestCatalogs;
using Flowthru.Tests.Fixtures.TestNodes;
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
  public void UseCatalog_MultipleCatalogs_AllResolvableByConcreteType()
  {
    // Arrange
    var services = new ServiceCollection();
    services.AddLogging();

    // Act
    services.AddFlowthru(flowthru =>
    {
      flowthru.UseCatalog<UpstreamCatalog>();
      flowthru.UseCatalog<DownstreamCatalog>();
      flowthru.RegisterPipeline<UpstreamCatalog>(
        "Upstream",
        up =>
          PipelineBuilder.CreatePipeline(b =>
            b.AddNode("U1", PassthroughNode.Create(), up.UpstreamInput, up.UpstreamOutput)
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
      flowthru.UseCatalog<UpstreamCatalog>();
      flowthru.UseCatalog<DownstreamCatalog>();
      flowthru.RegisterPipeline<UpstreamCatalog>(
        "Upstream",
        up =>
          PipelineBuilder.CreatePipeline(b =>
            b.AddNode("U1", PassthroughNode.Create(), up.UpstreamInput, up.UpstreamOutput)
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
  public void RegisterPipeline_TwoCatalogs_PipelineIsRegistered()
  {
    // Arrange
    var services = new ServiceCollection();
    services.AddLogging();

    services.AddFlowthru(flowthru =>
    {
      flowthru.UseCatalog<UpstreamCatalog>();
      flowthru.UseCatalog<DownstreamCatalog>();

      // Single-catalog upstream pipeline
      flowthru.RegisterPipeline<UpstreamCatalog>(
        "Upstream",
        up =>
          PipelineBuilder.CreatePipeline(b =>
            b.AddNode("Process", PassthroughNode.Create(), up.UpstreamInput, up.UpstreamOutput)
          )
      );

      // 2-catalog bridge pipeline: reads from upstream, writes to downstream
      flowthru.RegisterPipeline<UpstreamCatalog, DownstreamCatalog>(
        "Bridge",
        (up, down) =>
          PipelineBuilder.CreatePipeline(b =>
            b.AddNode("Bridge", PassthroughNode.Create(), up.UpstreamOutput, down.DownstreamOutput)
          )
      );
    });

    var sp = services.BuildServiceProvider();
    var service = sp.GetRequiredService<IFlowthruService>();

    // Assert
    Assert.That(service.PipelineNames, Does.Contain("Upstream"));
    Assert.That(service.PipelineNames, Does.Contain("Bridge"));
  }

  [Test]
  public async Task RegisterPipeline_TwoCatalogs_DagResolvesCrossCatalogEdge()
  {
    // Arrange — the Bridge pipeline reads up.UpstreamOutput, which is written by the
    // Upstream pipeline. When merged, the DAG must see a single ICatalogEntry instance
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
      flowthru.UseCatalog(upstream);
      flowthru.UseCatalog(downstream);

      flowthru.RegisterPipeline<UpstreamCatalog>(
        "Upstream",
        up =>
          PipelineBuilder.CreatePipeline(b =>
            b.AddNode("Process", PassthroughNode.Create(), up.UpstreamInput, up.UpstreamOutput)
          )
      );

      flowthru.RegisterPipeline<UpstreamCatalog, DownstreamCatalog>(
        "Bridge",
        (up, down) =>
          PipelineBuilder.CreatePipeline(b =>
            b.AddNode("Bridge", PassthroughNode.Create(), up.UpstreamOutput, down.DownstreamOutput)
          )
      );
    });

    var sp = services.BuildServiceProvider();
    var service = sp.GetRequiredService<IFlowthruService>();

    // Act
    var result = await service.ExecutePipelineAsync(exportMetadata: false);

    // Assert — both pipelines executed and data flowed through
    Assert.That(result.Success, Is.True);
    Assert.That(result.NodeResults, Has.Count.EqualTo(2));

    var output = await downstream.DownstreamOutput.Load().Run();
    Assert.That(output, Is.Not.Null);
    Assert.That(output!.Count(), Is.EqualTo(1));
    Assert.That(output!.First().Name, Is.EqualTo("cross-catalog"));
  }

  // ─────────────────────────────────────────────────────────────────────────
  // 3-catalog pipeline registration
  // ─────────────────────────────────────────────────────────────────────────

  [Test]
  public async Task RegisterPipeline_ThreeCatalogs_DagResolvesFullChain()
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
      flowthru.UseCatalog(upstream);
      flowthru.UseCatalog(downstream);
      flowthru.UseCatalog(third);

      flowthru.RegisterPipeline<UpstreamCatalog>(
        "Upstream",
        up =>
          PipelineBuilder.CreatePipeline(b =>
            b.AddNode("Process", PassthroughNode.Create(), up.UpstreamInput, up.UpstreamOutput)
          )
      );

      flowthru.RegisterPipeline<UpstreamCatalog, DownstreamCatalog>(
        "Bridge",
        (up, down) =>
          PipelineBuilder.CreatePipeline(b =>
            b.AddNode("Bridge", PassthroughNode.Create(), up.UpstreamOutput, down.DownstreamOutput)
          )
      );

      // 3-catalog pipeline reads from both upstream and downstream, writes to third
      flowthru.RegisterPipeline<UpstreamCatalog, DownstreamCatalog, ThirdCatalog>(
        "Merge",
        (up, down, t) =>
          PipelineBuilder.CreatePipeline(b =>
            // Use downstream output (which depends on upstream) as the final step input
            b.AddNode("Merge", PassthroughNode.Create(), down.DownstreamOutput, t.FinalOutput)
          )
      );
    });

    var sp = services.BuildServiceProvider();
    var service = sp.GetRequiredService<IFlowthruService>();

    // Act
    var result = await service.ExecutePipelineAsync(exportMetadata: false);

    // Assert — all three pipelines executed in dependency order
    Assert.That(result.Success, Is.True);
    Assert.That(result.NodeResults, Has.Count.EqualTo(3));

    var finalOutput = await third.FinalOutput.Load().Run();
    Assert.That(finalOutput!.First().Name, Is.EqualTo("three-catalog"));
  }

  // ─────────────────────────────────────────────────────────────────────────
  // WithDescription chaining after multi-catalog registration
  // ─────────────────────────────────────────────────────────────────────────

  [Test]
  public void RegisterPipeline_TwoCatalogs_WithDescription_SetsDescription()
  {
    // Arrange
    var services = new ServiceCollection();
    services.AddLogging();

    services.AddFlowthru(flowthru =>
    {
      flowthru.UseCatalog<UpstreamCatalog>();
      flowthru.UseCatalog<DownstreamCatalog>();

      flowthru
        .RegisterPipeline<UpstreamCatalog, DownstreamCatalog>(
          "Bridge",
          (up, down) =>
            PipelineBuilder.CreatePipeline(b =>
              b.AddNode("B", PassthroughNode.Create(), up.UpstreamOutput, down.DownstreamOutput)
            )
        )
        .WithDescription("Bridges upstream to downstream domain");
    });

    var sp = services.BuildServiceProvider();
    var service = sp.GetRequiredService<IFlowthruService>();
    var meta = service.GetPipelineMetadata("Bridge");

    // Assert
    Assert.That(meta.Description, Is.EqualTo("Bridges upstream to downstream domain"));
  }

  // ─────────────────────────────────────────────────────────────────────────
  // DAG identity guarantee
  // ─────────────────────────────────────────────────────────────────────────

  [Test]
  public void RegisterPipeline_TwoCatalogs_SharedEntryPreservesObjectIdentity()
  {
    // Arrange — upstream.UpstreamOutput is referenced by both the Upstream pipeline
    // (as an output) and the Bridge pipeline (as an input). Both pipelines receive the
    // SAME catalog instance from DI, so the entries are ReferenceEquals. This is the
    // critical property that enables correct DAG edge resolution.
    var upstream = new UpstreamCatalog();
    var downstream = new DownstreamCatalog();

    Pipeline? upstreamPipeline = null;
    Pipeline? bridgePipeline = null;

    var services = new ServiceCollection();
    services.AddLogging();

    services.AddFlowthru(flowthru =>
    {
      flowthru.UseCatalog(upstream);
      flowthru.UseCatalog(downstream);

      flowthru.RegisterPipeline<UpstreamCatalog>(
        "Upstream",
        up =>
        {
          upstreamPipeline = PipelineBuilder.CreatePipeline(b =>
            b.AddNode("P", PassthroughNode.Create(), up.UpstreamInput, up.UpstreamOutput)
          );
          return upstreamPipeline;
        }
      );

      flowthru.RegisterPipeline<UpstreamCatalog, DownstreamCatalog>(
        "Bridge",
        (up, down) =>
        {
          bridgePipeline = PipelineBuilder.CreatePipeline(b =>
            b.AddNode("B", PassthroughNode.Create(), up.UpstreamOutput, down.DownstreamOutput)
          );
          return bridgePipeline;
        }
      );
    });

    // Trigger service build (which invokes the pipeline factories)
    var sp = services.BuildServiceProvider();
    _ = sp.GetRequiredService<IFlowthruService>();

    // Assert — UpstreamOutput entry is the same instance in both pipelines
    var upstreamOutputInProducer = upstreamPipeline!.Nodes.First(n => n.Label == "P").Outputs[0];
    var upstreamOutputInConsumer = bridgePipeline!.Nodes.First(n => n.Label == "B").Inputs[0];

    Assert.That(
      upstreamOutputInProducer,
      Is.SameAs(upstreamOutputInConsumer),
      "The shared entry must be the same object instance for the DAG to resolve the dependency edge."
    );
  }
}
