using Flowthru.Core.Tests.Diagnostics;
using Flowthru.Data.Catalog;
using Flowthru.Data.Storage;
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

  // ── FlowBuilder.Add chokepoint — IStepNode.OnAddedToFlow ───────────────

  /// <summary>
  /// Pins the chokepoint contract: <see cref="FlowBuilder.Add"/> stamps
  /// the defining flow's label onto a framework-shipped step type
  /// (<see cref="Step{TIn, TOut}"/>) whose constructor left it empty.
  /// Replaces the previous design where every <c>AddStep</c> factory
  /// threaded <c>flowLabel: builder.Label</c> through the constructor
  /// manually — that convention drifted out of sync the moment an
  /// extension generator forgot to pass it, sending steps into a
  /// phantom <c>__merged__</c> bucket downstream. Removing this stamp
  /// is a regression that surfaces in every metadata consumer.
  /// </summary>
  [Test]
  public void FlowBuilder_Add_StampsFlowLabel_WhenStepConstructedWithoutOne()
  {
    var input = ItemFactory.Singleton.Memory<int>("chokepoint-in");
    var output = ItemFactory.Singleton.Memory<int>("chokepoint-out");

    // Construct directly without threading flowLabel — simulates an
    // extension factory that forgot the convention. The chokepoint
    // must compensate.
    var step = new Step<int, int>(
      label: "bare-step",
      transform: x => FlowIO.Pure(x),
      inputs: new IItem[] { input },
      outputs: new IItem[] { output },
      loadInputs: () => input.Load(),
      saveOutputs: r => output.Save(r)
      // intentionally no flowLabel: argument
    );

    Assert.That(step.FlowLabel, Is.Empty,
      "Pre-condition: constructor with no flowLabel should leave the slot empty.");

    var flow = FlowBuilder.CreateFlow("chokepoint-flow", b => b.Add(step));

    Assert.That(flow.Steps.Single().FlowLabel, Is.EqualTo("chokepoint-flow"),
      "FlowBuilder.Add must stamp the defining flow's label via IStepNode.OnAddedToFlow "
        + "when construction left FlowLabel empty. Without this, multi-arity Python steps "
        + "and any future extension that omits `flowLabel:` from its factory drift into "
        + "the synthetic __merged__ bucket in downstream metadata.");
  }

  /// <summary>
  /// Twin of the stamp-if-empty pin: a step constructed with an
  /// explicit <c>flowLabel</c> must keep it. The chokepoint is a
  /// safety net, not an override — overwriting a deliberate label
  /// would silently re-attribute steps and defeat the very
  /// flow-tracking guarantee the stamp exists to preserve.
  /// </summary>
  [Test]
  public void FlowBuilder_Add_DoesNotOverwrite_ExplicitlySuppliedFlowLabel()
  {
    var input = ItemFactory.Singleton.Memory<int>("explicit-in");
    var output = ItemFactory.Singleton.Memory<int>("explicit-out");

    var step = new Step<int, int>(
      label: "preset-step",
      transform: x => FlowIO.Pure(x),
      inputs: new IItem[] { input },
      outputs: new IItem[] { output },
      loadInputs: () => input.Load(),
      saveOutputs: r => output.Save(r),
      flowLabel: "preset-origin"
    );

    var flow = FlowBuilder.CreateFlow("different-flow", b => b.Add(step));

    Assert.That(flow.Steps.Single().FlowLabel, Is.EqualTo("preset-origin"),
      "OnAddedToFlow must be stamp-if-empty: an explicit ctor-supplied FlowLabel is a "
        + "deliberate attribution and the chokepoint must never overwrite it. Overwriting "
        + "would silently change the flow-of-origin for any consumer that pre-stamps a step "
        + "before threading it through a builder (e.g., test harnesses, advanced wiring).");
  }

  /// <summary>
  /// Pins the default-interface no-op semantics: a hand-rolled
  /// <see cref="IStepNode"/> that doesn't override
  /// <see cref="IStepNode.OnAddedToFlow"/> is unaffected by the
  /// chokepoint — its <see cref="IStepNode.FlowLabel"/> stays
  /// exactly what it self-reports. The chokepoint is opt-in via
  /// override; bypassing it is a legitimate use case (advanced
  /// consumers that manage flow attribution outside the framework's
  /// concrete step types).
  /// </summary>
  [Test]
  public void FlowBuilder_Add_LeavesHandRolledStepNode_FlowLabel_Untouched()
  {
    var step = new HandRolledStepNode("hand-rolled");
    var flow = FlowBuilder.CreateFlow("ignored-by-hand-rolled", b => b.Add(step));

    Assert.That(flow.Steps.Single().FlowLabel, Is.Empty,
      "A hand-rolled IStepNode that doesn't override OnAddedToFlow must keep its "
        + "self-reported FlowLabel — the chokepoint's default-interface no-op contract "
        + "means stamping is opt-in for framework-shipped step types only. Stamping a "
        + "hand-rolled type would silently change its self-reported attribution.");
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

/// <summary>
/// Minimal hand-rolled <see cref="IStepNode"/> that does NOT override
/// <see cref="IStepNode.OnAddedToFlow"/>. Used to pin the chokepoint's
/// default-interface no-op contract: bespoke step types stay
/// unstamped, so advanced consumers retain full control over
/// flow-of-origin attribution.
/// </summary>
internal sealed class HandRolledStepNode : IStepNode
{
  public HandRolledStepNode(string label) { Label = label; }
  public string Label { get; }
  public NodeTraits Traits { get; } = new NodeTraits();
  public IReadOnlyList<IItem> Inputs { get; } = Array.Empty<IItem>();
  public IReadOnlyList<IItem> Outputs { get; } = Array.Empty<IItem>();
  public IReadOnlyList<ServiceRef> ServiceDependencies { get; } = Array.Empty<ServiceRef>();
  public FlowIO<ValidationResult> Validate() => FlowIO.Pure(ValidationResult.Success());
  public FlowIO<FlowUnit> Execute() => FlowIO.Pure(FlowUnit.Default);
  // Intentionally does NOT override OnAddedToFlow — default no-op applies.
  // Intentionally does NOT override FlowLabel — default empty string applies.
}

/// <summary>Trivial implementation of <see cref="IIntegrationFakeService"/>.</summary>
public sealed class IntegrationFakeService : IIntegrationFakeService { }
