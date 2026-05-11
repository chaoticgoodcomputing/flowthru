using Flowthru.FUnit.SourceGenerators;

namespace FUnit.SourceGenerators.Tests;

/// <summary>
/// Positive + negative tests for the two FUnit diagnostics:
/// <list type="bullet">
///   <item>FU001 — <c>[FlowthruStep]</c> class has no
///     <c>[FUnitStepTest]</c> methods anywhere in the project.</item>
///   <item>FU002 — <c>FUnitContext</c> subclass not guarded by
///     <c>#if FUNIT_ENABLED</c>.</item>
/// </list>
/// The analyzer keys off fully-qualified names
/// (<c>Flowthru.Step.FlowthruStepAttribute</c>,
/// <c>Flowthru.Step.Testing.FUnitStepTestAttribute</c>,
/// <c>Flowthru.Step.Testing.FUnitContext</c>) — every fixture supplies
/// matching stubs in-source so the harness stays self-contained and
/// doesn't pull production assemblies into the test compilation.
/// </summary>
[TestFixture]
public class FUnitDiagnosticAnalyzerTests
{
  // ── Stubs ──────────────────────────────────────────────────────────────
  //
  // The analyzer keys on these exact fully-qualified names; stub
  // declarations let fixtures stay self-contained.

  private const string FlowthruStepStub = """
    namespace Flowthru.Step
    {
      [System.AttributeUsage(System.AttributeTargets.Class)]
      public sealed class FlowthruStepAttribute : System.Attribute { }
    }

    namespace Flowthru.Step.Testing
    {
      [System.AttributeUsage(System.AttributeTargets.Method)]
      public sealed class FUnitStepTestAttribute : System.Attribute
      {
        public FUnitStepTestAttribute(System.Type stepType) { }
      }

      public class FUnitContext { }
    }
    """;

  // ── FU001: [FlowthruStep] without [FUnitStepTest] coverage ────────────

  [Test]
  public async Task SupportedDiagnostics_ContainsFu001AndFu002()
  {
    var analyzer = new FUnitDiagnosticAnalyzer();
    var ids = analyzer.SupportedDiagnostics.Select(d => d.Id).ToList();
    Assert.That(ids, Does.Contain("FU001"));
    Assert.That(ids, Does.Contain("FU002"));
  }

  [Test]
  public async Task FlowthruStepWithoutAnyStepTest_FiresFu001()
  {
    var consumer = """
      namespace Sample;

      [Flowthru.Step.FlowthruStepAttribute]
      public class OrphanStep { }
      """;

    var diags = await AnalyzerTestHarness.RunAnalyzerAsync(
      new FUnitDiagnosticAnalyzer(),
      new[] { FlowthruStepStub, consumer }
    );
    Assert.That(diags.WithId("FU001").ToList(), Is.Not.Empty,
      "FU001 should fire on a [FlowthruStep] class with no [FUnitStepTest] anywhere. "
      + "Got: " + string.Join(", ", diags.Select(d => d.Id)));
  }

  [Test]
  public async Task FlowthruStepWithMatchingStepTest_NoFu001()
  {
    var consumer = """
      namespace Sample;

      [Flowthru.Step.FlowthruStepAttribute]
      public class CoveredStep { }

      public class CoveredStepTests
      {
        [Flowthru.Step.Testing.FUnitStepTestAttribute(typeof(CoveredStep))]
        public void Sanity() { }
      }
      """;

    var diags = await AnalyzerTestHarness.RunAnalyzerAsync(
      new FUnitDiagnosticAnalyzer(),
      new[] { FlowthruStepStub, consumer }
    );
    Assert.That(diags.WithId("FU001").ToList(), Is.Empty,
      "FU001 should be silent when a [FUnitStepTest] method targets the step.");
  }

  [Test]
  public async Task FlowthruStepWithStepTestForDifferentStep_FiresFu001()
  {
    // A step test exists, but it points at a different step. The
    // orphan step is still uncovered, so FU001 must still fire.
    var consumer = """
      namespace Sample;

      [Flowthru.Step.FlowthruStepAttribute]
      public class CoveredStep { }

      [Flowthru.Step.FlowthruStepAttribute]
      public class OrphanStep { }

      public class OnlyCoveredTests
      {
        [Flowthru.Step.Testing.FUnitStepTestAttribute(typeof(CoveredStep))]
        public void Sanity() { }
      }
      """;

    var diags = await AnalyzerTestHarness.RunAnalyzerAsync(
      new FUnitDiagnosticAnalyzer(),
      new[] { FlowthruStepStub, consumer }
    );
    var fu001 = diags.WithId("FU001").ToList();
    Assert.That(fu001, Is.Not.Empty,
      "FU001 should fire for OrphanStep even when CoveredStep has a test.");
    Assert.That(
      fu001.Any(d => d.GetMessage().Contains("OrphanStep")),
      Is.True,
      "FU001's message should name the orphan step. Got: "
        + string.Join(" / ", fu001.Select(d => d.GetMessage()))
    );
  }

  [Test]
  public async Task NonStepClass_NoFu001()
  {
    var consumer = """
      namespace Sample;

      // Plain class without [FlowthruStep] — analyzer must be silent.
      public class RegularClass { }
      """;

    var diags = await AnalyzerTestHarness.RunAnalyzerAsync(
      new FUnitDiagnosticAnalyzer(),
      new[] { FlowthruStepStub, consumer }
    );
    Assert.That(diags.WithId("FU001").ToList(), Is.Empty);
  }

  // ── FU002: FUnitContext subclass not guarded by #if FUNIT_ENABLED ─────

  [Test]
  public async Task FUnitContextSubclassWithoutGuard_FiresFu002()
  {
    var consumer = """
      namespace Sample;

      public class UnguardedTests : Flowthru.Step.Testing.FUnitContext { }
      """;

    var diags = await AnalyzerTestHarness.RunAnalyzerAsync(
      new FUnitDiagnosticAnalyzer(),
      new[] { FlowthruStepStub, consumer }
    );
    var fu002 = diags.WithId("FU002").ToList();
    Assert.That(fu002, Is.Not.Empty,
      "FU002 should fire on an FUnitContext subclass not wrapped in #if FUNIT_ENABLED.");
    Assert.That(
      fu002.Any(d => d.GetMessage().Contains("UnguardedTests")),
      Is.True,
      "FU002 message should name the subclass. Got: "
        + string.Join(" / ", fu002.Select(d => d.GetMessage()))
    );
  }

  [Test]
  public async Task FUnitContextSubclassInsideFUnitEnabledGuard_NoFu002()
  {
    var consumer = """
      namespace Sample;

      #if FUNIT_ENABLED
      public class GuardedTests : Flowthru.Step.Testing.FUnitContext { }
      #endif
      """;

    var diags = await AnalyzerTestHarness.RunAnalyzerAsync(
      new FUnitDiagnosticAnalyzer(),
      new[] { FlowthruStepStub, consumer }
    );
    Assert.That(diags.WithId("FU002").ToList(), Is.Empty,
      "FU002 should be silent when the subclass sits inside #if FUNIT_ENABLED.");
  }

  [Test]
  public async Task NonFUnitContextClass_NoFu002()
  {
    var consumer = """
      namespace Sample;

      public class NotAContext { }
      public class StillNotAContext { public int X { get; set; } }
      """;

    var diags = await AnalyzerTestHarness.RunAnalyzerAsync(
      new FUnitDiagnosticAnalyzer(),
      new[] { FlowthruStepStub, consumer }
    );
    Assert.That(diags.WithId("FU002").ToList(), Is.Empty);
  }

  [Test]
  public async Task TransitivelyDerivedFUnitContext_FiresFu002OnConcreteSubclass()
  {
    // The analyzer walks the BaseType chain — a class three hops away
    // from FUnitContext is still detected.
    var consumer = """
      namespace Sample;

      public abstract class IntermediateContext : Flowthru.Step.Testing.FUnitContext { }
      public class DeepUnguardedTests : IntermediateContext { }
      """;

    var diags = await AnalyzerTestHarness.RunAnalyzerAsync(
      new FUnitDiagnosticAnalyzer(),
      new[] { FlowthruStepStub, consumer }
    );
    var fu002 = diags.WithId("FU002").ToList();
    // Both IntermediateContext and DeepUnguardedTests inherit from
    // FUnitContext (directly and transitively). Each should trigger.
    Assert.That(fu002, Is.Not.Empty,
      "FU002 should fire on transitively-derived FUnitContext subclasses too.");
    Assert.That(
      fu002.Select(d => d.GetMessage()).Any(m => m.Contains("DeepUnguardedTests")),
      Is.True,
      "FU002 should report the concrete subclass. Got: "
        + string.Join(" / ", fu002.Select(d => d.GetMessage()))
    );
  }
}
