using Flowthru.Data.Catalog;
using Flowthru.Data.Storage;
using Flowthru.Flow;
using Flowthru.Prelude;
using Flowthru.Step;
using Flowthru.Validation.Runtime;

namespace Flowthru.Core.Tests.Step;

/// <summary>
/// Tests for <see cref="IStepNode.CodeVersion"/> — the per-step
/// build-time identity that downstream cache-plan logic (a later RFC
/// phase) consumes to decide when a step's output can be reused. The
/// identity is sourced from <c>StepMetadataGenerator</c> for
/// <c>[FlowthruStep]</c>-decorated classes, threaded through
/// <see cref="FlowBuilder.AddStep"/>, and exposed on the
/// <see cref="IStepNode"/> surface.
/// </summary>
/// <remarks>
/// <para>
/// Hand-constructed <see cref="Step{TIn, TOut}"/> instances without a
/// <c>codeVersion:</c> argument return <c>null</c> — the explicit
/// "we don't know" signal that downstream consumers treat as a
/// cache-miss. This is the fail-safe path: a missing identity never
/// silently asserts equality.
/// </para>
/// </remarks>
[TestFixture]
public class StepCodeVersionTests
{
  // ── Default-interface contract ────────────────────────────────────────

  [Test]
  public void IStepNode_DefaultImplementation_ReturnsNullForCodeVersion()
  {
    // A hand-rolled IStepNode that does not opt into CodeVersion
    // should surface null — the engine's "fail-safe" cache signal.
    IStepNode node = new HandRolledNoCodeVersionStep("hand-rolled");
    Assert.That(node.CodeVersion, Is.Null,
      "Default interface implementation of CodeVersion should return null. "
      + "A null value signals 'unknown identity' to cache-plan consumers, "
      + "guaranteeing fail-safe cache-miss rather than a silent false match.");
  }

  // ── Step<TIn, TOut> constructor ───────────────────────────────────────

  [Test]
  public void Step_ConstructedWithoutCodeVersion_ExposesNull()
  {
    var input = ItemFactory.Singleton.Memory<int>("cv-null-in");
    var output = ItemFactory.Singleton.Memory<int>("cv-null-out");

    var step = new Step<int, int>(
      label: "no-version",
      transform: x => FlowIO.Pure(x),
      inputs: new IItem[] { input },
      outputs: new IItem[] { output },
      loadInputs: () => input.Load(),
      saveOutputs: r => output.Save(r)
      // codeVersion intentionally omitted
    );

    Assert.That(((IStepNode)step).CodeVersion, Is.Null,
      "Step<TIn, TOut> without a codeVersion constructor argument must expose null on IStepNode.");
  }

  [Test]
  public void Step_ConstructedWithCodeVersion_ExposesProvidedValue()
  {
    var input = ItemFactory.Singleton.Memory<int>("cv-set-in");
    var output = ItemFactory.Singleton.Memory<int>("cv-set-out");

    var step = new Step<int, int>(
      label: "v1",
      transform: x => FlowIO.Pure(x),
      inputs: new IItem[] { input },
      outputs: new IItem[] { output },
      loadInputs: () => input.Load(),
      saveOutputs: r => output.Save(r),
      codeVersion: "abc123"
    );

    Assert.That(((IStepNode)step).CodeVersion, Is.EqualTo("abc123"),
      "Step<TIn, TOut> must expose the codeVersion value it was constructed with.");
  }

  // ── FlowBuilder.AddStep threading ─────────────────────────────────────

  [Test]
  public void FlowBuilder_AddStep_InlineLambda_LeavesCodeVersionNull()
  {
    // Inline lambdas carry no metadata — the StepMetadataGenerator
    // emits a companion only for [FlowthruStep]-attributed classes.
    // The lambda branch of AddStep must therefore default CodeVersion
    // to null, just like ServiceDependencies defaults to empty.
    var input = ItemFactory.Singleton.Memory<int>("cv-inline-in");
    var output = ItemFactory.Singleton.Memory<int>("cv-inline-out");

    var flow = FlowBuilder.CreateFlow("cv-inline-flow", b =>
      b.AddStep<int, int>("inline", x => x + 1, input, output)
    );

    var step = flow.Steps.Single();
    Assert.That(step.CodeVersion, Is.Null,
      "Inline lambdas have no [FlowthruStep] metadata; CodeVersion must default to null.");
  }

  // ── Integration: [FlowthruStep] companion threads CodeVersion through ──

  [Test]
  public void FlowBuilder_AddStep_WithGeneratedCodeVersion_ExposesItOnIStepNode()
  {
    // The StepMetadataGenerator emits the CodeVersion constant on the
    // {ClassName}_Metadata companion. Callers thread it as
    // `codeVersion: ProbeStep_Metadata.CodeVersion`. The constructed
    // step must surface that value on IStepNode.CodeVersion so the
    // future cache-plan phase can read it.
    var input = ItemFactory.Singleton.Memory<int>("cv-probe-in");
    var output = ItemFactory.Singleton.Memory<int>("cv-probe-out");

    var flow = FlowBuilder.CreateFlow("cv-probe-flow", b =>
      b.AddStep<int, int>(
        label: "probe",
        transform: ProbeCodeVersionStep.Create(),
        inputs: input,
        outputs: output,
        codeVersion: ProbeCodeVersionStep_Metadata.CodeVersion
      )
    );

    var step = flow.Steps.Single();
    Assert.That(step.CodeVersion, Is.EqualTo(ProbeCodeVersionStep_Metadata.CodeVersion),
      "The {ClassName}_Metadata.CodeVersion constant should reach IStepNode.CodeVersion verbatim.");
    Assert.That(step.CodeVersion, Is.Not.Null.And.Not.Empty,
      "Source-generated CodeVersion must be a non-empty hex string for steps with bodies.");
  }
}

/// <summary>
/// Probe step decorated with <c>[FlowthruStep]</c> so the source
/// generator emits its companion <c>ProbeCodeVersionStep_Metadata</c>
/// with a non-null <c>CodeVersion</c> constant. Used by the
/// integration test to verify the threading path from generator output
/// to IStepNode.CodeVersion.
/// </summary>
[FlowthruStep]
public static class ProbeCodeVersionStep
{
  public static Func<int, int> Create() => x => x + 7;
}

/// <summary>
/// Hand-rolled IStepNode that opts into NEITHER OnAddedToFlow NOR
/// CodeVersion. Used to pin the default-interface contract: bespoke
/// step types stay null-versioned unless they explicitly self-report.
/// </summary>
internal sealed class HandRolledNoCodeVersionStep : IStepNode
{
  public HandRolledNoCodeVersionStep(string label) { Label = label; }
  public string Label { get; }
  public NodeTraits Traits { get; } = new NodeTraits();
  public IReadOnlyList<IItem> Inputs { get; } = Array.Empty<IItem>();
  public IReadOnlyList<IItem> Outputs { get; } = Array.Empty<IItem>();
  public IReadOnlyList<ServiceDependency> ServiceDependencies { get; } = Array.Empty<ServiceDependency>();
  public FlowIO<ValidationResult> Validate() => FlowIO.Pure(ValidationResult.Success());
  public FlowIO<FlowUnit> Execute() => FlowIO.Pure(FlowUnit.Default);
  // Intentionally does NOT override CodeVersion — default null applies.
}
