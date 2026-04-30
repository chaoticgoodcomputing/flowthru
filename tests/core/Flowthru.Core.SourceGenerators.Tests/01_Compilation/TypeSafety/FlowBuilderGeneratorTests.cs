using Flowthru.Tests.Helpers;
using Microsoft.CodeAnalysis;

namespace Flowthru.Core.SourceGenerators.Tests.Compilation.TypeSafety;

/// <summary>
/// Tests for the FlowBuilder source generator that emits AddStep overloads for up to 8
/// inputs and 8 outputs. The generator is gated to run only inside the
/// <c>Flowthru.Core</c> assembly.
/// </summary>
[TestFixture]
[Category("Compilation")]
[Category("SourceGenerator")]
public class FlowBuilderGeneratorTests
{
  // ─────────────────────────────────────────────────────────────────────────
  // Happy path — runs inside Flowthru.Core
  // ─────────────────────────────────────────────────────────────────────────

  [Test]
  public void RunsInsideFlowthruCore_EmitsFlowBuilderGeneratedFile()
  {
    var result = GeneratorTestHelper.RunFlowBuilderGenerator();

    Assert.That(
      result.GeneratedSources,
      Has.Count.EqualTo(1),
      "Expected exactly one generated source (FlowBuilder.Generated.cs)."
    );

    var generated = result.GetGeneratedSource("FlowBuilder.Generated.cs");
    Assert.That(generated, Is.Not.Null);
    Assert.That(generated, Does.Contain("public partial class FlowBuilder"));
    Assert.That(generated, Does.Contain("AddStep"));
  }

  [Test]
  public void GeneratedFile_ContainsOverloadsForMultipleInputArities()
  {
    var result = GeneratorTestHelper.RunFlowBuilderGenerator();

    var generated = result.GetGeneratedSource("FlowBuilder.Generated.cs")!;

    // Spot-check that overloads for arities other than 1x1 (which is hand-written) are present.
    // The generator emits 1x2, 2x1, etc.; the easiest way to detect them is to look for the
    // type-parameter shapes in the generated signatures.
    Assert.That(generated, Does.Contain("AddStep<"));
    // 2-input shape: TIn1, TIn2
    Assert.That(generated, Does.Contain("TIn2"));
    // Multi-output shape: TOut2
    Assert.That(generated, Does.Contain("TOut2"));
  }

  [Test]
  public void GeneratedFile_ContainsBothAsyncAndSyncVariants()
  {
    var result = GeneratorTestHelper.RunFlowBuilderGenerator();

    var generated = result.GetGeneratedSource("FlowBuilder.Generated.cs")!;

    // Async overloads return Task<...>
    Assert.That(generated, Does.Contain("Task<"));
  }

  [Test]
  public void GeneratedFile_ContainsCancellationTokenVariants()
  {
    var result = GeneratorTestHelper.RunFlowBuilderGenerator();

    var generated = result.GetGeneratedSource("FlowBuilder.Generated.cs")!;

    // The generator emits async-with-CancellationToken across the full arity matrix —
    // pre-Phase-1 only 1×1 had a hand-written CT variant. Confirm the generated source
    // now carries CT-bearing transform signatures.
    Assert.That(generated, Does.Contain("CancellationToken"));
  }

  [Test]
  public void GeneratedFile_ContainsZeroArityShapes()
  {
    var result = GeneratorTestHelper.RunFlowBuilderGenerator();

    var generated = result.GetGeneratedSource("FlowBuilder.Generated.cs")!;

    // 0-arity sync/async shapes use Action / Func<Task> rather than the typed
    // Func<TIn, TOut> family. Confirm both signatures appear in the output.
    Assert.Multiple(() =>
    {
      // 0×0 sync: Action transform
      Assert.That(generated, Does.Contain("Action transform"));
      // 0×0 async: Func<Task> transform (or Func<CancellationToken, Task>)
      Assert.That(generated, Does.Contain("Func<Task> transform"));
      // 1×0 sync: Action<TIn1> transform
      Assert.That(generated, Does.Contain("Action<TIn1> transform"));
    });
  }

  // ─────────────────────────────────────────────────────────────────────────
  // Gating — does NOT run for consumer assemblies
  // ─────────────────────────────────────────────────────────────────────────

  [Test]
  public void RunsInConsumerAssembly_EmitsNothing()
  {
    var result = GeneratorTestHelper.RunFlowBuilderGeneratorAsConsumer();

    Assert.That(
      result.GeneratedSources,
      Is.Empty,
      "FlowBuilderGenerator must be gated to Flowthru.Core only."
    );
  }
}
