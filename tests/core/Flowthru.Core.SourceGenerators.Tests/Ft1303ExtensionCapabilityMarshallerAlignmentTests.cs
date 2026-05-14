using Flowthru.Core.SourceGenerators.Step;

namespace Flowthru.Core.SourceGenerators.Tests;

/// <summary>
/// Tests for <see cref="ExtensionCapabilityMarshallerAlignmentAnalyzer"/> —
/// the <c>FT1303</c> analyzer that enforces alignment between the
/// <c>[StepExtensionCapabilities]</c> attribute and the marshaller
/// marker interfaces an extension implements.
/// </summary>
[TestFixture]
public class Ft1303ExtensionCapabilityMarshallerAlignmentTests
{
  // ── Stubs ─────────────────────────────────────────────────────────────

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

    namespace Flowthru.Step.Marshalling
    {
      public interface IContainerMarshaller<TExtension> where TExtension : Flowthru.Step.IStepExtension { }
      public interface IQueryableMarshaller<TExtension> where TExtension : Flowthru.Step.IStepExtension { }
      public interface IAsyncStreamMarshaller<TExtension> where TExtension : Flowthru.Step.IStepExtension { }
    }
    """;

  // ── Floor alignment ──────────────────────────────────────────────────

  [Test]
  public async Task FloorDeclared_WithContainerMarshaller_Silent()
  {
    var consumer = """
      using Flowthru.Step;
      using Flowthru.Step.Marshalling;

      namespace Sample;

      [StepExtensionCapabilities(
        StepContainerKind.Singleton | StepContainerKind.Enumerable,
        StepContainerKind.Singleton | StepContainerKind.Enumerable)]
      public sealed class GoodExtension :
        IStepExtension,
        IContainerMarshaller<GoodExtension>
      { }
      """;

    var diags = await AnalyzerTestHarness.RunAsync(
      new ExtensionCapabilityMarshallerAlignmentAnalyzer(),
      new[] { Stubs, consumer }
    );
    Assert.That(diags.Where("FT1303").ToList(), Is.Empty,
      "An aligned extension (floor + IContainerMarshaller) must not fire FT1303.");
  }

  [Test]
  public async Task FloorDeclared_WithoutContainerMarshaller_FiresFt1303()
  {
    var consumer = """
      using Flowthru.Step;

      namespace Sample;

      [StepExtensionCapabilities(
        StepContainerKind.Singleton | StepContainerKind.Enumerable,
        StepContainerKind.Singleton | StepContainerKind.Enumerable)]
      public sealed class MissingMarshallerExtension : IStepExtension { }
      """;

    var diags = await AnalyzerTestHarness.RunAsync(
      new ExtensionCapabilityMarshallerAlignmentAnalyzer(),
      new[] { Stubs, consumer }
    );

    var ft1303 = diags.Where("FT1303").ToList();
    Assert.That(ft1303, Is.Not.Empty);
    Assert.That(ft1303[0].GetMessage(), Does.Contain("IContainerMarshaller"));
  }

  [Test]
  public async Task ContainerMarshallerWithoutFloor_FiresFt1303()
  {
    // Class implements IContainerMarshaller but declares no Singleton or
    // Enumerable — silent drift in the other direction.
    var consumer = """
      using Flowthru.Step;
      using Flowthru.Step.Marshalling;

      namespace Sample;

      [StepExtensionCapabilities(StepContainerKind.Queryable, StepContainerKind.Queryable)]
      public sealed class OverclaimingExtension :
        IStepExtension,
        IContainerMarshaller<OverclaimingExtension>,
        IQueryableMarshaller<OverclaimingExtension>
      { }
      """;

    var diags = await AnalyzerTestHarness.RunAsync(
      new ExtensionCapabilityMarshallerAlignmentAnalyzer(),
      new[] { Stubs, consumer }
    );

    var ft1303 = diags.Where("FT1303").ToList();
    Assert.That(ft1303, Is.Not.Empty);
    Assert.That(string.Join("\n", ft1303.Select(d => d.GetMessage())),
      Does.Contain("Singleton").Or.Contain("Enumerable"));
  }

  // ── Queryable alignment ──────────────────────────────────────────────

  [Test]
  public async Task QueryableDeclared_WithMarshaller_Silent()
  {
    var consumer = """
      using Flowthru.Step;
      using Flowthru.Step.Marshalling;

      namespace Sample;

      [StepExtensionCapabilities(
        StepContainerKind.Singleton | StepContainerKind.Enumerable | StepContainerKind.Queryable,
        StepContainerKind.Singleton | StepContainerKind.Enumerable)]
      public sealed class QueryableExtension :
        IStepExtension,
        IContainerMarshaller<QueryableExtension>,
        IQueryableMarshaller<QueryableExtension>
      { }
      """;

    var diags = await AnalyzerTestHarness.RunAsync(
      new ExtensionCapabilityMarshallerAlignmentAnalyzer(),
      new[] { Stubs, consumer }
    );
    Assert.That(diags.Where("FT1303").ToList(), Is.Empty);
  }

  [Test]
  public async Task QueryableDeclared_WithoutMarshaller_FiresFt1303()
  {
    var consumer = """
      using Flowthru.Step;
      using Flowthru.Step.Marshalling;

      namespace Sample;

      [StepExtensionCapabilities(
        StepContainerKind.Singleton | StepContainerKind.Enumerable | StepContainerKind.Queryable,
        StepContainerKind.Singleton | StepContainerKind.Enumerable)]
      public sealed class HalfQueryableExtension :
        IStepExtension,
        IContainerMarshaller<HalfQueryableExtension>
      { }
      """;

    var diags = await AnalyzerTestHarness.RunAsync(
      new ExtensionCapabilityMarshallerAlignmentAnalyzer(),
      new[] { Stubs, consumer }
    );

    var ft1303 = diags.Where("FT1303").ToList();
    Assert.That(ft1303, Is.Not.Empty);
    Assert.That(ft1303[0].GetMessage(), Does.Contain("IQueryableMarshaller"));
  }

  [Test]
  public async Task QueryableMarshaller_WithoutDeclaration_FiresFt1303()
  {
    var consumer = """
      using Flowthru.Step;
      using Flowthru.Step.Marshalling;

      namespace Sample;

      [StepExtensionCapabilities(
        StepContainerKind.Singleton | StepContainerKind.Enumerable,
        StepContainerKind.Singleton | StepContainerKind.Enumerable)]
      public sealed class StealthQueryableExtension :
        IStepExtension,
        IContainerMarshaller<StealthQueryableExtension>,
        IQueryableMarshaller<StealthQueryableExtension>
      { }
      """;

    var diags = await AnalyzerTestHarness.RunAsync(
      new ExtensionCapabilityMarshallerAlignmentAnalyzer(),
      new[] { Stubs, consumer }
    );

    var ft1303 = diags.Where("FT1303").ToList();
    Assert.That(ft1303, Is.Not.Empty);
    Assert.That(ft1303[0].GetMessage(), Does.Contain("does not declare Queryable"));
  }

  // ── AsyncStream alignment ────────────────────────────────────────────

  [Test]
  public async Task AsyncStreamDeclared_WithoutMarshaller_FiresFt1303()
  {
    var consumer = """
      using Flowthru.Step;
      using Flowthru.Step.Marshalling;

      namespace Sample;

      [StepExtensionCapabilities(
        StepContainerKind.Singleton | StepContainerKind.Enumerable | StepContainerKind.AsyncStream,
        StepContainerKind.Singleton | StepContainerKind.Enumerable)]
      public sealed class HalfAsyncExtension :
        IStepExtension,
        IContainerMarshaller<HalfAsyncExtension>
      { }
      """;

    var diags = await AnalyzerTestHarness.RunAsync(
      new ExtensionCapabilityMarshallerAlignmentAnalyzer(),
      new[] { Stubs, consumer }
    );

    var ft1303 = diags.Where("FT1303").ToList();
    Assert.That(ft1303, Is.Not.Empty);
    Assert.That(ft1303[0].GetMessage(), Does.Contain("IAsyncStreamMarshaller"));
  }

  // ── Non-extension classes are silent ─────────────────────────────────

  [Test]
  public async Task ClassWithoutMarker_Silent()
  {
    var consumer = """
      using Flowthru.Step;

      namespace Sample;

      [StepExtensionCapabilities(StepContainerKind.None, StepContainerKind.None)]
      public sealed class NotAnExtension { }
      """;

    var diags = await AnalyzerTestHarness.RunAsync(
      new ExtensionCapabilityMarshallerAlignmentAnalyzer(),
      new[] { Stubs, consumer }
    );
    Assert.That(diags.Where("FT1303").ToList(), Is.Empty);
  }

  [Test]
  public void SupportedDiagnostics_ExposesFt1303()
  {
    var analyzer = new ExtensionCapabilityMarshallerAlignmentAnalyzer();
    Assert.That(analyzer.SupportedDiagnostics.Select(d => d.Id),
      Has.Member("FT1303"));
  }
}
