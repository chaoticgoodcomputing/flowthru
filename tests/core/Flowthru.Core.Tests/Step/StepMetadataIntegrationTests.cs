using Flowthru.Core.Tests.Diagnostics;
using Flowthru.Data.Catalog;
using Flowthru.Diagnostics;
using Flowthru.Flow;
using Flowthru.Hosting;
using Flowthru.Prelude;
using Flowthru.Step;
using Flowthru.Validation.Runtime;
using Microsoft.Extensions.DependencyInjection;

namespace Flowthru.Core.Tests.Step;

/// <summary>
/// Integration tests for source-generated step metadata as it flows
/// through <see cref="FlowBuilder.AddStep"/> into the engine's
/// <see cref="IStepNode"/> view, and through <c>FlowthruService</c>'s
/// merged-DAG construction.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="StepMetadataGeneratorTests"/> covers the source-generator
/// output (the <c>_Metadata</c> companion). These tests cover the
/// FlowBuilder integration boundary: how an attributed step's
/// metadata, plus the FlowBuilder's own knowledge of its flow label,
/// end up on the constructed <see cref="IStepNode"/>.
/// </para>
/// <para>
/// <strong>Phantom merged flow regression net.</strong>
/// <see cref="Merge_PreservesPreMergeFlowLabelOnEveryStep"/> is the
/// explicit pin for the cross-cutting concern from
/// <c>docs/scratch/test-coverage-gap-analysis.md</c>: after merging
/// multiple registered flows, every step's
/// <see cref="IStepNode.FlowLabel"/> must still name the flow that
/// declared it — never <c>"__merged__"</c> or any synthesized
/// container. Downstream metadata renderers (JSON, Mermaid) project
/// this field as <c>FlowOfOrigin</c>; a regression would silently
/// scramble flow attribution across every metadata consumer.
/// </para>
/// </remarks>
[TestFixture]
public class StepMetadataIntegrationTests
{
  // ── FlowBuilder.AddStep populates FlowLabel from the flow's label ──────

  [Test]
  public void FlowBuilder_AddStep_StampsFlowLabelOntoEveryStep()
  {
    // The FlowBuilder source generator threads its own `this.Label`
    // into the Step constructor's flowLabel parameter; the engine
    // surface (IStepNode.FlowLabel) is the read-back.
    var input = ItemFactory.Singleton.Memory<int>("flb-input");
    var output = ItemFactory.Singleton.Memory<int>("flb-output");

    var flow = FlowBuilder.CreateFlow("ds-pipeline", b =>
      b.AddStep<int, int>("double", x => x * 2, input, output)
    );

    var step = flow.Steps.Single();
    Assert.That(step.FlowLabel, Is.EqualTo("ds-pipeline"),
      "AddStep should stamp the FlowBuilder's label onto each constructed step.");
  }

  [Test]
  public void FlowBuilder_AddStep_InlineLambda_LeavesServiceDependenciesEmpty()
  {
    // Inline lambdas carry no metadata — the StepMetadataGenerator
    // emits a companion only for [FlowthruStep]-attributed classes.
    // FlowBuilder.AddStep must therefore default ServiceDependencies
    // to empty for the lambda case (which is what a Step constructor
    // sees when serviceDependencies is null).
    var input = ItemFactory.Singleton.Memory<int>("inline-input");
    var output = ItemFactory.Singleton.Memory<int>("inline-output");

    var flow = FlowBuilder.CreateFlow("inline-flow", b =>
      b.AddStep<int, int>("inline-step", x => x + 1, input, output)
    );

    var step = flow.Steps.Single();
    Assert.That(step.ServiceDependencies, Is.Empty,
      "Inline lambdas have no attribute metadata; ServiceDependencies must default to empty.");
  }

  // ── Explicit ServiceDependencies survive through FlowBuilder.Add ───────

  [Test]
  public void FlowBuilder_Add_PreservesExplicitServiceDependencies()
  {
    // Steps constructed directly (e.g. by extension AddStep variants
    // like PythonStep, or by user-authored bespoke step classes) may
    // declare ServiceDependencies up front. The FlowBuilder.Add path
    // must preserve them as-is — it's the only way the engine sees
    // which services the step intends to consume.
    var input = ItemFactory.Singleton.Memory<int>("svc-input");
    var output = ItemFactory.Singleton.Memory<int>("svc-output");

    var serviceDeps = new ServiceRef[]
    {
      ServiceRef.Of<IIntegrationFakeService>(),
    };

    var step = new Step<int, int>(
      label: "service-step",
      transform: x => FlowIO.Pure(x),
      inputs: new IItem[] { input },
      outputs: new IItem[] { output },
      loadInputs: () => input.Load(),
      saveOutputs: r => output.Save(r),
      serviceDependencies: serviceDeps,
      flowLabel: "svc-flow"
    );

    var flow = FlowBuilder.CreateFlow("svc-flow", b => b.Add(step));

    var built = flow.Steps.Single();
    Assert.That(built.ServiceDependencies,
      Is.EquivalentTo(new[] { ServiceRef.Of<IIntegrationFakeService>() }),
      "Explicit ServiceDependencies on the constructed Step must survive FlowBuilder.Add.");
  }

  // ── Phantom 'merged' flow attribution regression ───────────────────────

  /// <summary>
  /// CRITICAL REGRESSION TEST. After merging multiple registered
  /// flows into a single execution DAG, every step's
  /// <see cref="IStepNode.FlowLabel"/> must still name its
  /// <em>pre-merge</em> defining flow. A regression would surface as
  /// every step's <c>FlowOfOrigin</c> (the JSON metadata projection's
  /// field name) reading <c>"__merged__"</c> instead of the original
  /// flow label — silently scrambling flow attribution in every
  /// metadata consumer (JSON, Mermaid, dashboards).
  /// </summary>
  [Test]
  public async Task Merge_PreservesPreMergeFlowLabelOnEveryStep()
  {
    // Two registered flows merge into a single execution DAG. After
    // merging, each step's FlowLabel must still name the flow that
    // declared it — never the merged container's synthesized label.
    var stage1 = ItemFactory.Singleton.Memory<int>("merge-stage1");
    var stage2 = ItemFactory.Singleton.Memory<int>("merge-stage2");
    var stage3 = ItemFactory.Singleton.Memory<int>("merge-stage3");
    await stage1.Save(0).Run();

    // Probe the metadata pathway directly — same surface JSON / Mermaid
    // renderers read, so any phantom-merged regression surfaces at the
    // same boundary a third-party metadata consumer would see.
    var captured = new List<FlowMetadataContext>();

    var services = new ServiceCollection();
    services.AddFlowthru(b =>
    {
      b.RegisterFlow("DataEngineering", () => FlowBuilder.CreateFlow("DataEngineering", p =>
        p.AddStep<int, int>("de-step", x => x + 1, stage1, stage2)
      ));
      b.RegisterFlow("DataScience", () => FlowBuilder.CreateFlow("DataScience", p =>
        p.AddStep<int, int>("ds-step", x => x * 2, stage2, stage3)
      ));
      b.ConfigureMetadata(m => m.AddProvider(new CapturingContextProvider(captured)));
    });

    await using var sp = services.BuildServiceProvider();
    var flowthru = sp.GetRequiredService<IFlowthruService>();
    var result = await flowthru.RunAsync();
    Assert.That(result.IsSuccess, Is.True, "Sanity check: merged DAG should execute cleanly.");

    var attribution = captured.Single().MergedFlow.Steps
      .ToDictionary(s => s.Label, s => s.FlowLabel, StringComparer.Ordinal);

    Assert.Multiple(() =>
    {
      Assert.That(attribution.Keys, Is.EquivalentTo(new[] { "de-step", "ds-step" }),
        "The merged DAG should expose every registered step to the metadata pathway.");
      Assert.That(attribution["de-step"], Is.EqualTo("DataEngineering"),
        "de-step's FlowLabel must remain 'DataEngineering' after merge — never the synthesized merged label.");
      Assert.That(attribution["ds-step"], Is.EqualTo("DataScience"),
        "ds-step's FlowLabel must remain 'DataScience' after merge — never the synthesized merged label.");

      // The phantom-merged-flow attribution bug would manifest as any
      // step's FlowLabel being the merged-DAG's own label or the
      // historical synthesized container name. Assert against both
      // directly so a future regression is reported in exactly those
      // terms.
      Assert.That(attribution.Values, Has.None.EqualTo("__merged__"),
        "No step should be attributed to '__merged__' — that's the FlowthruService's internal merged-DAG label, never a per-step attribution.");
      Assert.That(attribution.Values, Has.None.EqualTo("merged"),
        "No step should be attributed to 'merged' — phantom-merged-flow regression sentinel.");
    });
  }
}

// ── Top-level fixtures (must be top-level for [FlowthruStep] generator) ──

/// <summary>
/// Marker service for the explicit-ServiceDependencies test. Lives at
/// namespace scope because some Flowthru tooling expects DI services to
/// be reachable without nested-type qualification.
/// </summary>
public interface IIntegrationFakeService { }

/// <summary>Trivial implementation of <see cref="IIntegrationFakeService"/>.</summary>
public sealed class IntegrationFakeService : IIntegrationFakeService { }
