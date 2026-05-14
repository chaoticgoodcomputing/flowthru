using Flowthru.Core.SourceGenerators.Step;

namespace Flowthru.Core.SourceGenerators.Tests;

/// <summary>
/// Tests for <see cref="ReadOnlyItemOutputAnalyzer"/> — the <c>FT1102</c>
/// analyzer that rejects <see cref="IReadOnlyItem{T}"/> values appearing
/// in the <c>outputs:</c> position of <c>FlowBuilder.AddStep</c>. Per
/// the Phase 5 RFC: configuration items (and any other read-only item)
/// must never appear as a step output — write operations would always
/// fail at runtime, so the type system enforces the constraint at build
/// time instead.
/// </summary>
[TestFixture]
public class Ft1102ReadOnlyItemOutputAnalyzerTests
{
  // ── Stubs ─────────────────────────────────────────────────────────────
  //
  // The analyzer keys on these exact fully-qualified names. Stubbing
  // them inline keeps the fixture self-contained.

  private const string Stubs = """
    namespace Flowthru.Data.Catalog
    {
      public interface IItem<T> { }
      public interface IReadOnlyItem<T> : IItem<T> { }
    }

    namespace Flowthru.Flow
    {
      public partial class FlowBuilder
      {
        public FlowBuilder AddStep<TIn, TOut>(
          string label,
          System.Func<TIn, TOut> transform,
          Flowthru.Data.Catalog.IItem<TIn> inputs,
          Flowthru.Data.Catalog.IItem<TOut> outputs
        ) => this;

        public FlowBuilder AddStep<TIn, TOut1, TOut2>(
          string label,
          System.Func<TIn, (TOut1, TOut2)> transform,
          Flowthru.Data.Catalog.IItem<TIn> inputs,
          (Flowthru.Data.Catalog.IItem<TOut1>, Flowthru.Data.Catalog.IItem<TOut2>) outputs
        ) => this;
      }
    }
    """;

  // ── Reading a config item as an INPUT is fine ─────────────────────────

  [Test]
  public async Task ReadOnlyItem_AsInput_Silent()
  {
    var consumer = """
      namespace Sample;

      public class ConfigPayload { }
      public class OutputPayload { }

      public class C
      {
        public void M(
          Flowthru.Flow.FlowBuilder b,
          Flowthru.Data.Catalog.IReadOnlyItem<ConfigPayload> config,
          Flowthru.Data.Catalog.IItem<OutputPayload> output)
        {
          b.AddStep<ConfigPayload, OutputPayload>(
            "step",
            c => new OutputPayload(),
            inputs: config,
            outputs: output);
        }
      }
      """;

    var diags = await AnalyzerTestHarness.RunAsync(
      new ReadOnlyItemOutputAnalyzer(),
      new[] { Stubs, consumer }
    );
    Assert.That(diags.Where("FT1102").ToList(), Is.Empty,
      "Read-only items in the inputs position are the canonical use — must not fire FT1102.");
  }

  // ── Read-only item in OUTPUT position fires ───────────────────────────

  [Test]
  public async Task ReadOnlyItem_AsOutput_FiresFt1102()
  {
    var consumer = """
      namespace Sample;

      public class ConfigPayload { }

      public class C
      {
        public void M(
          Flowthru.Flow.FlowBuilder b,
          Flowthru.Data.Catalog.IItem<ConfigPayload> input,
          Flowthru.Data.Catalog.IReadOnlyItem<ConfigPayload> config)
        {
          b.AddStep<ConfigPayload, ConfigPayload>(
            "step",
            c => c,
            inputs: input,
            outputs: config);
        }
      }
      """;

    var diags = await AnalyzerTestHarness.RunAsync(
      new ReadOnlyItemOutputAnalyzer(),
      new[] { Stubs, consumer }
    );

    var ft1102 = diags.Where("FT1102").ToList();
    Assert.That(ft1102, Is.Not.Empty,
      "A step declaring a read-only item as its output must fire FT1102.");
    Assert.That(ft1102[0].GetMessage(), Does.Contain("read-only").IgnoreCase,
      "FT1102 message should mention 'read-only'. Got: " + ft1102[0].GetMessage());
  }

  [Test]
  public async Task WritableItem_AsOutput_Silent()
  {
    // The control case — a plain IItem<T> in the outputs position is
    // the canonical authoring shape and must not be flagged.
    var consumer = """
      namespace Sample;

      public class Payload { }

      public class C
      {
        public void M(
          Flowthru.Flow.FlowBuilder b,
          Flowthru.Data.Catalog.IItem<Payload> input,
          Flowthru.Data.Catalog.IItem<Payload> output)
        {
          b.AddStep<Payload, Payload>(
            "step",
            x => x,
            inputs: input,
            outputs: output);
        }
      }
      """;

    var diags = await AnalyzerTestHarness.RunAsync(
      new ReadOnlyItemOutputAnalyzer(),
      new[] { Stubs, consumer }
    );
    Assert.That(diags.Where("FT1102").ToList(), Is.Empty,
      "Writable items in the outputs position are the canonical use — must not fire FT1102.");
  }

  [Test]
  public async Task ReadOnlyItem_NestedInOutputTuple_FiresFt1102()
  {
    // Multi-output steps pass outputs as a tuple of items; a read-only
    // item buried inside the tuple is just as broken as a top-level
    // read-only output. Analyzer must walk tuple arguments.
    var consumer = """
      namespace Sample;

      public class A { }
      public class B { }

      public class C
      {
        public void M(
          Flowthru.Flow.FlowBuilder b,
          Flowthru.Data.Catalog.IItem<A> input,
          Flowthru.Data.Catalog.IItem<A> okOutput,
          Flowthru.Data.Catalog.IReadOnlyItem<B> badOutput)
        {
          b.AddStep<A, A, B>(
            "step",
            x => (x, new B()),
            inputs: input,
            outputs: (okOutput, badOutput));
        }
      }
      """;

    var diags = await AnalyzerTestHarness.RunAsync(
      new ReadOnlyItemOutputAnalyzer(),
      new[] { Stubs, consumer }
    );

    var ft1102 = diags.Where("FT1102").ToList();
    Assert.That(ft1102, Is.Not.Empty,
      "A read-only item nested inside a multi-output tuple must still fire FT1102.");
  }

  [Test]
  public async Task AddStepOnNonFlowBuilderReceiver_Silent()
  {
    // FT1102 must gate on receiver type — AddStep on a non-FlowBuilder
    // is not Flowthru's surface and must be ignored.
    var stubsWithImposter = Stubs + """

      namespace Flowthru.Flow
      {
        public class Imposter
        {
          public Imposter AddStep<TIn, TOut>(
            string label,
            System.Func<TIn, TOut> transform,
            Flowthru.Data.Catalog.IItem<TIn> inputs,
            Flowthru.Data.Catalog.IItem<TOut> outputs
          ) => this;
        }
      }
      """;

    var consumer = """
      namespace Sample;

      public class Payload { }

      public class C
      {
        public void M(
          Flowthru.Flow.Imposter b,
          Flowthru.Data.Catalog.IItem<Payload> input,
          Flowthru.Data.Catalog.IReadOnlyItem<Payload> output)
        {
          b.AddStep<Payload, Payload>("step", x => x, input, output);
        }
      }
      """;

    var diags = await AnalyzerTestHarness.RunAsync(
      new ReadOnlyItemOutputAnalyzer(),
      new[] { stubsWithImposter, consumer }
    );
    Assert.That(diags.Where("FT1102").ToList(), Is.Empty,
      "Read-only check must only apply to Flowthru.Flow.FlowBuilder.AddStep.");
  }

  [Test]
  public void SupportedDiagnostics_ExposesFt1102()
  {
    var analyzer = new ReadOnlyItemOutputAnalyzer();
    Assert.That(analyzer.SupportedDiagnostics.Select(d => d.Id),
      Has.Member("FT1102"),
      "Analyzer must advertise FT1102 in SupportedDiagnostics.");
  }
}
