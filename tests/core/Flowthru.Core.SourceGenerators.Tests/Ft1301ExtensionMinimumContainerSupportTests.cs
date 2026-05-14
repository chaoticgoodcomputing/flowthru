using Flowthru.Core.SourceGenerators.Step;
using Microsoft.CodeAnalysis;

namespace Flowthru.Core.SourceGenerators.Tests;

/// <summary>
/// Tests for <see cref="ExtensionMinimumContainerSupportAnalyzer"/> —
/// the <c>FT1301</c> analyzer that enforces the Singleton | Enumerable
/// minimum floor for step extensions. Severity downgrades to warning
/// when the attribute's <c>Status</c> is <c>InDevelopment</c>.
/// </summary>
[TestFixture]
public class Ft1301ExtensionMinimumContainerSupportTests
{
  // ── Stubs ─────────────────────────────────────────────────────────────
  //
  // Mirror just enough of the Flowthru.Step surface for the analyzer to
  // bind. Keeping these inline (rather than referencing Flowthru.Core)
  // matches the Ft1102 fixture pattern and isolates the test from
  // unrelated breakage in Core.

  private const string Stubs = """
    namespace Flowthru.Step
    {
      public interface IStepExtension { }

      [System.Flags]
      public enum StepContainerKind
      {
        None = 0,
        Singleton = 1,
        Enumerable = 2,
        Queryable = 4,
        AsyncStream = 8,
      }

      public enum ExtensionStatus
      {
        Production = 0,
        InDevelopment = 1,
      }

      [System.AttributeUsage(System.AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
      public sealed class StepExtensionCapabilitiesAttribute : System.Attribute
      {
        public StepContainerKind Inputs { get; }
        public StepContainerKind Outputs { get; }
        public ExtensionStatus Status { get; set; } = ExtensionStatus.Production;
        public StepExtensionCapabilitiesAttribute(StepContainerKind inputs, StepContainerKind outputs)
        {
          Inputs = inputs;
          Outputs = outputs;
        }
      }
    }
    """;

  // ── Happy path: full minimum floor is silent ──────────────────────────

  [Test]
  public async Task FullFloor_BothSides_Silent()
  {
    var consumer = """
      using Flowthru.Step;

      namespace Sample;

      [StepExtensionCapabilities(
        StepContainerKind.Singleton | StepContainerKind.Enumerable,
        StepContainerKind.Singleton | StepContainerKind.Enumerable)]
      public sealed class GoodExtension : IStepExtension { }
      """;

    var diags = await AnalyzerTestHarness.RunAsync(
      new ExtensionMinimumContainerSupportAnalyzer(),
      new[] { Stubs, consumer }
    );
    Assert.That(diags.Where("FT1301").ToList(), Is.Empty,
      "An extension declaring full Singleton | Enumerable on both sides must not fire FT1301.");
  }

  // ── Failure modes: missing kinds fire FT1301 ──────────────────────────

  [Test]
  public async Task MissingSingleton_OnInputs_FiresFt1301()
  {
    var consumer = """
      using Flowthru.Step;

      namespace Sample;

      [StepExtensionCapabilities(
        StepContainerKind.Enumerable,
        StepContainerKind.Singleton | StepContainerKind.Enumerable)]
      public sealed class HalfInputExtension : IStepExtension { }
      """;

    var diags = await AnalyzerTestHarness.RunAsync(
      new ExtensionMinimumContainerSupportAnalyzer(),
      new[] { Stubs, consumer }
    );

    var ft1301 = diags.Where("FT1301").ToList();
    Assert.That(ft1301, Is.Not.Empty,
      "Missing Singleton on the inputs side must fire FT1301.");
    Assert.That(ft1301[0].Severity, Is.EqualTo(DiagnosticSeverity.Error),
      "Without Status = InDevelopment the diagnostic must fire as Error.");
    Assert.That(ft1301[0].GetMessage(), Does.Contain("Inputs"));
  }

  [Test]
  public async Task MissingEnumerable_OnOutputs_FiresFt1301()
  {
    var consumer = """
      using Flowthru.Step;

      namespace Sample;

      [StepExtensionCapabilities(
        StepContainerKind.Singleton | StepContainerKind.Enumerable,
        StepContainerKind.Singleton)]
      public sealed class HalfOutputExtension : IStepExtension { }
      """;

    var diags = await AnalyzerTestHarness.RunAsync(
      new ExtensionMinimumContainerSupportAnalyzer(),
      new[] { Stubs, consumer }
    );

    var ft1301 = diags.Where("FT1301").ToList();
    Assert.That(ft1301, Is.Not.Empty);
    Assert.That(ft1301[0].GetMessage(), Does.Contain("Outputs"));
  }

  [Test]
  public async Task NoneKind_BothSides_FiresFt1301Twice()
  {
    var consumer = """
      using Flowthru.Step;

      namespace Sample;

      [StepExtensionCapabilities(StepContainerKind.None, StepContainerKind.None)]
      public sealed class EmptyExtension : IStepExtension { }
      """;

    var diags = await AnalyzerTestHarness.RunAsync(
      new ExtensionMinimumContainerSupportAnalyzer(),
      new[] { Stubs, consumer }
    );

    Assert.That(diags.Where("FT1301").Count(), Is.EqualTo(2),
      "An extension declaring None on both sides must fire one FT1301 per slot.");
  }

  // ── Status = InDevelopment downgrades severity ───────────────────────

  [Test]
  public async Task InDevelopmentStatus_DowngradesToWarning()
  {
    var consumer = """
      using Flowthru.Step;

      namespace Sample;

      [StepExtensionCapabilities(
        StepContainerKind.Enumerable,
        StepContainerKind.Enumerable,
        Status = ExtensionStatus.InDevelopment)]
      public sealed class InProgressExtension : IStepExtension { }
      """;

    var diags = await AnalyzerTestHarness.RunAsync(
      new ExtensionMinimumContainerSupportAnalyzer(),
      new[] { Stubs, consumer }
    );

    var ft1301 = diags.Where("FT1301").ToList();
    Assert.That(ft1301, Is.Not.Empty,
      "Missing Singleton must still fire FT1301 even when in development.");
    Assert.That(ft1301.All(d => d.Severity == DiagnosticSeverity.Warning), Is.True,
      "Status = InDevelopment must downgrade every FT1301 instance to Warning.");
  }

  // ── Class without IStepExtension marker is silent ────────────────────

  [Test]
  public async Task ClassWithoutMarker_Silent()
  {
    // The analyzer only applies to IStepExtension implementations.
    // A class carrying the attribute but not the marker is some other
    // user code we don't speak for — silent.
    var consumer = """
      using Flowthru.Step;

      namespace Sample;

      [StepExtensionCapabilities(StepContainerKind.None, StepContainerKind.None)]
      public sealed class NotAnExtension { }
      """;

    var diags = await AnalyzerTestHarness.RunAsync(
      new ExtensionMinimumContainerSupportAnalyzer(),
      new[] { Stubs, consumer }
    );
    Assert.That(diags.Where("FT1301").ToList(), Is.Empty);
  }

  // ── IStepExtension without the attribute is silent ────────────────────

  [Test]
  public async Task ExtensionWithoutAttribute_Silent()
  {
    // FT1301 only validates declared capabilities. An extension that
    // forgot to declare them at all is a different concern (could be a
    // future FT-code). For now: silent.
    var consumer = """
      using Flowthru.Step;

      namespace Sample;

      public sealed class UndeclaredExtension : IStepExtension { }
      """;

    var diags = await AnalyzerTestHarness.RunAsync(
      new ExtensionMinimumContainerSupportAnalyzer(),
      new[] { Stubs, consumer }
    );
    Assert.That(diags.Where("FT1301").ToList(), Is.Empty);
  }

  [Test]
  public void SupportedDiagnostics_ExposesFt1301()
  {
    var analyzer = new ExtensionMinimumContainerSupportAnalyzer();
    Assert.That(analyzer.SupportedDiagnostics.Select(d => d.Id),
      Has.Member("FT1301"));
  }
}
