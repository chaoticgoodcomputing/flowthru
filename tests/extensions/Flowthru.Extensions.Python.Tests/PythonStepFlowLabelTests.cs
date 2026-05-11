using Flowthru.Data.Catalog;
using Flowthru.Flow;
using Flowthru.Prelude;
using Flowthru.Step.Python;
using Flowthru.Validation.PreFlight;
using Flowthru.Validation.Runtime;

namespace Flowthru.Extensions.Python.Tests;

/// <summary>
/// Regression pin for the phantom <c>__merged__</c> flow attribution
/// bug. <see cref="PythonStepFactory.AddPythonStep{TIn, TOut}"/> must
/// stamp the step's <see cref="Flowthru.Step.IStepNode.FlowLabel"/> with
/// the defining flow's label — matching the convention the core
/// generator emits for plain <c>AddStep</c>. Without this, downstream
/// metadata providers (Mermaid, JSON) group Python steps into a
/// synthetic <c>__merged__</c> bucket instead of the flow that authored
/// them.
/// </summary>
[TestFixture]
public class PythonStepFlowLabelTests
{
  private sealed record ProbeIn(int Value);
  private sealed record ProbeOut(int Doubled);

  /// <summary>Minimal IPythonExecutor stub — no Python runtime is invoked.</summary>
  private sealed class NoopExecutor : IPythonExecutor
  {
    public FlowIO<PythonStepMetadata> ValidateStep(string moduleName, string functionName) =>
      FlowIO.Pure(PythonStepMetadata.Empty);

    public FlowIO<TOutput> Invoke<TInput, TOutput>(string moduleName, string functionName, TInput input) =>
      FlowIO.Fail<TOutput>(new RuntimeError.InvariantViolated(
        "NoopExecutor", "Invoke not used in FlowLabel pinning tests"
      ));

    public FlowIO<Validated<PreFlightError, FlowUnit>> InvokeInspector(
      PythonServiceRegistration registration
    ) => FlowIO.Pure(Validated<PreFlightError, FlowUnit>.Pure(FlowUnit.Default));
  }

  [Test]
  public void AddPythonStep_StampsFlowLabelOnEveryStep()
  {
    var input = ItemFactory.Singleton.Memory<ProbeIn>("flow-label-in");
    var output = ItemFactory.Singleton.Memory<ProbeOut>("flow-label-out");

    var flow = FlowBuilder.CreateFlow("Reporting", b =>
      b.AddPythonStep<ProbeIn, ProbeOut>(
        label: "GeneratePythonOutput",
        module: "demo",
        function: "step",
        input: input,
        output: output,
        executor: new NoopExecutor()
      )
    );

    var step = flow.Steps.Single();
    Assert.That(step.Label, Is.EqualTo("GeneratePythonOutput"));
    Assert.That(step.FlowLabel, Is.EqualTo("Reporting"),
      "AddPythonStep must stamp the defining flow's label onto the step. "
        + "An empty FlowLabel triggers the phantom __merged__ grouping in "
        + "metadata providers (Mermaid, JSON) — Python steps would appear "
        + "outside their authoring flow.");
  }

  [Test]
  public void AddPythonStep_MultiplePythonStepsInSameFlow_AllShareFlowLabel()
  {
    var inA = ItemFactory.Singleton.Memory<ProbeIn>("ml-in-a");
    var outA = ItemFactory.Singleton.Memory<ProbeOut>("ml-out-a");
    var inB = ItemFactory.Singleton.Memory<ProbeIn>("ml-in-b");
    var outB = ItemFactory.Singleton.Memory<ProbeOut>("ml-out-b");

    var flow = FlowBuilder.CreateFlow("ModelTraining", b =>
    {
      b.AddPythonStep<ProbeIn, ProbeOut>(
        "Train", "ml.train", "train", inA, outA, new NoopExecutor()
      );
      b.AddPythonStep<ProbeIn, ProbeOut>(
        "Score", "ml.score", "score", inB, outB, new NoopExecutor()
      );
    });

    Assert.That(flow.Steps.Select(s => s.FlowLabel),
      Is.EqualTo(new[] { "ModelTraining", "ModelTraining" }),
      "Every step in the flow must inherit the flow's label, regardless of "
        + "the AddStep variant used to construct it (core, Python, or future "
        + "extensions).");
  }
}
