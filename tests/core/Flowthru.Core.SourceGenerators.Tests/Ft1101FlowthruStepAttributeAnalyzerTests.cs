using Flowthru.Core.SourceGenerators.Step;

namespace Flowthru.Core.SourceGenerators.Tests;

/// <summary>
/// Tests for <see cref="FlowthruStepAttributeAnalyzer"/> — the
/// <c>FT1101</c> analyzer that warns when a named-class step factory
/// passed to <c>FlowBuilder.AddStep(transform: …)</c> is missing the
/// <c>[FlowthruStep]</c> attribute. Inline lambdas / anonymous methods
/// are exempted.
/// </summary>
[TestFixture]
public class Ft1101FlowthruStepAttributeAnalyzerTests
{
  // ── Stubs ─────────────────────────────────────────────────────────────
  //
  // The analyzer keys on these exact fully-qualified names. Stubbing
  // them inline keeps the fixture self-contained — we don't drag in
  // Flowthru.Core just to reach the AddStep symbol, and stubbing also
  // lets us exercise the "Flowthru.Flow.FlowBuilder" match precisely.

  private const string Stubs = """
    namespace Flowthru.Step
    {
      [System.AttributeUsage(System.AttributeTargets.Class)]
      public sealed class FlowthruStepAttribute : System.Attribute { }
    }

    namespace Flowthru.Flow
    {
      public partial class FlowBuilder
      {
        public FlowBuilder AddStep<TIn, TOut>(
          string label,
          System.Func<TIn, TOut> transform
        ) => this;

        // Second non-transform method so the cheap syntactic gate
        // (method name == AddStep) can be exercised without crossing
        // into the slow semantic gate.
        public FlowBuilder Something(string label) => this;
      }

      // Distinct receiver type whose AddStep should NOT trigger the
      // analyzer's "is this Flowthru.Flow.FlowBuilder?" check.
      public class Imposter
      {
        public Imposter AddStep<TIn, TOut>(
          string label,
          System.Func<TIn, TOut> transform
        ) => this;
      }
    }
    """;

  // ── Lambda / anonymous-method exemption ───────────────────────────────

  [Test]
  public async Task LambdaTransform_Silent()
  {
    // Inline lambdas don't reference a named step class, so there's
    // nothing to annotate — the analyzer exempts them.
    var consumer = """
      namespace Sample;

      public class C
      {
        public void M(Flowthru.Flow.FlowBuilder b) =>
          b.AddStep<int, int>("step", x => x + 1);
      }
      """;

    var diags = await AnalyzerTestHarness.RunAsync(
      new FlowthruStepAttributeAnalyzer(),
      new[] { Stubs, consumer }
    );
    Assert.That(diags.Where("FT1101").ToList(), Is.Empty,
      "Lambda transforms should not trigger FT1101.");
  }

  [Test]
  public async Task AnonymousMethodTransform_Silent()
  {
    var consumer = """
      namespace Sample;

      public class C
      {
        public void M(Flowthru.Flow.FlowBuilder b) =>
          b.AddStep<int, int>("step", delegate(int x) { return x + 1; });
      }
      """;

    var diags = await AnalyzerTestHarness.RunAsync(
      new FlowthruStepAttributeAnalyzer(),
      new[] { Stubs, consumer }
    );
    Assert.That(diags.Where("FT1101").ToList(), Is.Empty,
      "Anonymous-method transforms should also be exempted.");
  }

  // ── Named-class factory branches ──────────────────────────────────────

  [Test]
  public async Task NamedFactoryMissingAttribute_FiresFt1101()
  {
    // The user passes `MyStep.Create()` as the transform; MyStep
    // lacks [FlowthruStep]; the analyzer should fire.
    var consumer = """
      namespace Sample;

      public static class MyStep
      {
        public static System.Func<int, int> Create() => x => x + 1;
      }

      public class C
      {
        public void M(Flowthru.Flow.FlowBuilder b) =>
          b.AddStep<int, int>("step", MyStep.Create());
      }
      """;

    var diags = await AnalyzerTestHarness.RunAsync(
      new FlowthruStepAttributeAnalyzer(),
      new[] { Stubs, consumer }
    );

    var ft1101 = diags.Where("FT1101").ToList();
    Assert.That(ft1101, Is.Not.Empty,
      "A named step factory class lacking [FlowthruStep] should fire FT1101.");
    Assert.That(ft1101[0].GetMessage(), Does.Contain("MyStep"),
      "FT1101 message should name the missing-attribute class. Got: " + ft1101[0].GetMessage());
  }

  [Test]
  public async Task NamedFactoryWithAttribute_Silent()
  {
    var consumer = """
      namespace Sample;

      [Flowthru.Step.FlowthruStepAttribute]
      public static class MyStep
      {
        public static System.Func<int, int> Create() => x => x + 1;
      }

      public class C
      {
        public void M(Flowthru.Flow.FlowBuilder b) =>
          b.AddStep<int, int>("step", MyStep.Create());
      }
      """;

    var diags = await AnalyzerTestHarness.RunAsync(
      new FlowthruStepAttributeAnalyzer(),
      new[] { Stubs, consumer }
    );
    Assert.That(diags.Where("FT1101").ToList(), Is.Empty,
      "FT1101 must stay silent when the named-class factory carries [FlowthruStep].");
  }

  [Test]
  public async Task NamedFactoryMethodGroup_FiresFt1101()
  {
    // Method-group shape (no parentheses on Create) is the other
    // accepted authoring form. ResolveReceiverType handles it via
    // the MemberAccess branch — should still trigger FT1101 when
    // the receiver lacks the attribute.
    var consumer = """
      namespace Sample;

      public static class MyStep
      {
        public static int Run(int x) => x + 1;
      }

      public class C
      {
        public void M(Flowthru.Flow.FlowBuilder b) =>
          b.AddStep<int, int>("step", MyStep.Run);
      }
      """;

    var diags = await AnalyzerTestHarness.RunAsync(
      new FlowthruStepAttributeAnalyzer(),
      new[] { Stubs, consumer }
    );
    Assert.That(diags.Where("FT1101").ToList(), Is.Not.Empty,
      "Method-group transforms on undecorated classes should also fire FT1101.");
  }

  [Test]
  public async Task BareIdentifierFromEnclosingType_ResolvesViaIdentifierBranch()
  {
    // ResolveReceiverType's IdentifierNameSyntax branch covers the
    // case where the user invokes a static method on their own
    // enclosing class without a receiver. The receiver type is the
    // method's ContainingType, which here lacks [FlowthruStep] — so
    // FT1101 fires.
    var consumer = """
      namespace Sample;

      public class C
      {
        public static System.Func<int, int> Create() => x => x + 1;

        public void M(Flowthru.Flow.FlowBuilder b) =>
          b.AddStep<int, int>("step", Create());
      }
      """;

    var diags = await AnalyzerTestHarness.RunAsync(
      new FlowthruStepAttributeAnalyzer(),
      new[] { Stubs, consumer }
    );
    Assert.That(diags.Where("FT1101").ToList(), Is.Not.Empty,
      "A bare-identifier `Create()` should resolve via the enclosing-type branch and fire FT1101.");
  }

  // ── Named-argument handling ───────────────────────────────────────────

  [Test]
  public async Task TransformPassedByName_StillResolves()
  {
    // FindTransformArgument prefers `transform:` named lookup over
    // positional matching. Putting the transform first by name
    // exercises that branch (otherwise positional would still work).
    var consumer = """
      namespace Sample;

      public static class MyStep
      {
        public static System.Func<int, int> Create() => x => x + 1;
      }

      public class C
      {
        public void M(Flowthru.Flow.FlowBuilder b) =>
          b.AddStep<int, int>(transform: MyStep.Create(), label: "step");
      }
      """;

    var diags = await AnalyzerTestHarness.RunAsync(
      new FlowthruStepAttributeAnalyzer(),
      new[] { Stubs, consumer }
    );
    Assert.That(diags.Where("FT1101").ToList(), Is.Not.Empty,
      "FT1101 should fire regardless of named-argument ordering — the named lookup must find `transform:`.");
  }

  // ── Receiver-type gating ──────────────────────────────────────────────

  [Test]
  public async Task AddStepOnNonFlowBuilderReceiver_Silent()
  {
    // The analyzer's semantic check rejects AddStep methods declared
    // on any type other than Flowthru.Flow.FlowBuilder — so an
    // imposter type's AddStep must not trigger FT1101 even with an
    // undecorated factory argument.
    var consumer = """
      namespace Sample;

      public static class MyStep
      {
        public static System.Func<int, int> Create() => x => x + 1;
      }

      public class C
      {
        public void M(Flowthru.Flow.Imposter b) =>
          b.AddStep<int, int>("step", MyStep.Create());
      }
      """;

    var diags = await AnalyzerTestHarness.RunAsync(
      new FlowthruStepAttributeAnalyzer(),
      new[] { Stubs, consumer }
    );
    Assert.That(diags.Where("FT1101").ToList(), Is.Empty,
      "FT1101 must not fire when AddStep is declared on a non-FlowBuilder receiver.");
  }

  [Test]
  public async Task NonAddStepInvocationOnFlowBuilder_Silent()
  {
    // The cheap syntactic gate filters on the method name being
    // exactly "AddStep". Any other method on FlowBuilder — even one
    // taking a transform-shaped argument — must be ignored.
    var consumer = """
      namespace Sample;

      public class C
      {
        public void M(Flowthru.Flow.FlowBuilder b) => b.Something("step");
      }
      """;

    var diags = await AnalyzerTestHarness.RunAsync(
      new FlowthruStepAttributeAnalyzer(),
      new[] { Stubs, consumer }
    );
    Assert.That(diags.Where("FT1101").ToList(), Is.Empty,
      "Non-AddStep invocations on FlowBuilder should bypass the analyzer cheaply.");
  }

  // ── SupportedDiagnostics ──────────────────────────────────────────────

  [Test]
  public void SupportedDiagnostics_ExposesFt1101()
  {
    // Tooling reads SupportedDiagnostics to decide whether to run the
    // analyzer; FT1101 must appear there or the analyzer is invisible.
    var analyzer = new FlowthruStepAttributeAnalyzer();
    Assert.That(analyzer.SupportedDiagnostics.Select(d => d.Id),
      Has.Member("FT1101"),
      "Analyzer must advertise FT1101 in SupportedDiagnostics.");
  }
}
