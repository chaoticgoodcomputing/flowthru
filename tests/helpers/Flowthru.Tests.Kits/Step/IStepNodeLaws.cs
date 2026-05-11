using Flowthru.Prelude;
using Flowthru.Step;

namespace Flowthru.Tests.Kits.Step;

/// <summary>
/// Laws every <see cref="IStepNode{TIn, TOut}"/> implementer must satisfy.
/// Subclasses bind a concrete step plus a sample input, and inherit
/// tests covering Kleisli identity, transform determinism (when
/// applicable), and the engine-facing contracts on <c>Inputs</c>,
/// <c>Outputs</c>, and <c>Execute</c>.
/// </summary>
/// <typeparam name="TIn">The step's input value type.</typeparam>
/// <typeparam name="TOut">The step's output value type.</typeparam>
/// <remarks>
/// <para>
/// Per §2.4 / §2.11, a step is a Kleisli arrow over <see cref="FlowIO{A}"/>.
/// The laws here check the structural invariants the engine relies on
/// (label is non-empty, inputs/outputs are non-null, transform is
/// non-null, declared inputs equal items the engine sees).
/// </para>
/// <para>
/// Determinism — re-running <c>Transform</c> with the same input
/// produces equal output — is opt-in via
/// <see cref="IsDeterministic"/>; steps that intentionally consume
/// time, randomness, or service state can opt out without weakening
/// the rest of the suite.
/// </para>
/// </remarks>
public abstract class IStepNodeLaws<TIn, TOut>
{
  /// <summary>Build the step under test.</summary>
  protected abstract IStepNode<TIn, TOut> CreateStep();

  /// <summary>Build a sample <typeparamref name="TIn"/> the determinism law can drive.</summary>
  protected abstract TIn SampleInput { get; }

  /// <summary>Optional comparer for the determinism law.</summary>
  protected virtual IEqualityComparer<TOut>? Comparer => null;

  /// <summary>
  /// True if running the transform twice with the same input must
  /// produce equal outputs. Steps that consume randomness, time, or
  /// mutable service state can override to <c>false</c>.
  /// </summary>
  protected virtual bool IsDeterministic => true;

  // ── Structural laws ────────────────────────────────────────────────────

  /// <summary>The step has a non-empty <see cref="IStepNode.Label"/>.</summary>
  [Test]
  public void LabelLaw()
  {
    var step = CreateStep();
    Assert.That(step.Label, Is.Not.Null.And.Not.Empty,
      "Every step must declare a non-empty label — the dependency analyzer keys off it.");
  }

  /// <summary>The step's <see cref="IStepNode.Transform"/> is not null.</summary>
  [Test]
  public void TransformPresentLaw()
  {
    var step = CreateStep();
    Assert.That(step.Transform, Is.Not.Null,
      "Transform delegate must be non-null — the engine cannot dispatch a null arrow.");
  }

  /// <summary>The step exposes a non-null <see cref="IStepNode.Inputs"/> collection.</summary>
  [Test]
  public void InputsCollectionLaw()
  {
    var step = CreateStep();
    Assert.That(step.Inputs, Is.Not.Null,
      "Inputs must be a non-null collection — empty is allowed (source steps), null is not.");
  }

  /// <summary>The step exposes a non-null <see cref="IStepNode.Outputs"/> collection.</summary>
  [Test]
  public void OutputsCollectionLaw()
  {
    var step = CreateStep();
    Assert.That(step.Outputs, Is.Not.Null,
      "Outputs must be a non-null collection — empty is allowed (sink steps), null is not.");
  }

  /// <summary>The step exposes a non-null <see cref="IStepNode.ServiceDependencies"/> collection.</summary>
  [Test]
  public void ServiceDependenciesCollectionLaw()
  {
    var step = CreateStep();
    Assert.That(step.ServiceDependencies, Is.Not.Null,
      "ServiceDependencies must be a non-null collection — extensions iterate it without a null check.");
  }

  // ── Behavioural laws ───────────────────────────────────────────────────

  /// <summary>
  /// Re-running the transform with the same input produces equal
  /// output (when <see cref="IsDeterministic"/> holds).
  /// </summary>
  [Test]
  public async Task TransformDeterminismLaw()
  {
    if (!IsDeterministic)
    {
      Assert.Pass("Step opted out of the determinism law via IsDeterministic = false.");
    }

    var step = CreateStep();

    var first = await step.Transform(SampleInput).Run();
    var second = await step.Transform(SampleInput).Run();

    Assert.That(first, Is.InstanceOf<EffResult<TOut>.Success>(),
      "First Transform call should succeed for the supplied SampleInput.");
    Assert.That(second, Is.InstanceOf<EffResult<TOut>.Success>(),
      "Second Transform call should succeed identically.");

    var v1 = ((EffResult<TOut>.Success)first).Value;
    var v2 = ((EffResult<TOut>.Success)second).Value;
    if (Comparer is not null)
    {
      Assert.That(Comparer.Equals(v1, v2), Is.True,
        "Determinism: two calls with the same input should compare equal under the supplied comparer.");
    }
    else
    {
      Assert.That(v2, Is.EqualTo(v1),
        "Determinism: two calls with the same input should compare equal.");
    }
  }
}
