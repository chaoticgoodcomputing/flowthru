using Flowthru.Core.Data;
using Flowthru.FUnit;
using Flowthru.Tests.Helpers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Flowthru.FUnit.SourceGenerators.Tests;

[TestFixture]
[Category("FUnit")]
[Category("Generator")]
public class StepTestRegistryGeneratorTests
{
  // ===========================================================================
  // Registry shape
  // ===========================================================================

  [Test]
  public void Generator_EmitsRegistry_WhenFlowthruStepClassExists()
  {
    const string source = """
      using Flowthru.Core.Steps;
      using Flowthru.FUnit;

      [FlowthruStep]
      public static class MyStep { }

      public class MyTests
      {
          [StepTest(typeof(MyStep))]
          public void Test1() { }

          [StepTest(typeof(MyStep))]
          public void Test2() { }
      }
      """;

    var result = FUnitGeneratorTestHelper.RunFUnitGenerator(source);

    Assert.That(result.Success, Is.True, FormatErrors(result));
    Assert.That(result.GeneratedSource, Does.Contain("StepTestRegistry"));
    Assert.That(result.GeneratedSource, Does.Contain("typeof(MyStep)"));
    Assert.That(result.GeneratedSource, Does.Contain("= 2"));
  }

  [Test]
  public void Generator_EmitsZeroCount_ForUntestedStep()
  {
    const string source = """
      using Flowthru.Core.Steps;

      [FlowthruStep]
      public static class UntestedStep { }
      """;

    var result = FUnitGeneratorTestHelper.RunFUnitGenerator(source);

    Assert.That(result.GeneratedSource, Does.Contain("= 0"));
  }

  [Test]
  public void Generator_EmitsNoRegistry_WhenNoStepsExist()
  {
    const string source = """
      public class SomePlainClass { }
      """;

    var result = FUnitGeneratorTestHelper.RunFUnitGenerator(source);

    Assert.That(result.GeneratedSource, Is.Null.Or.Empty);
  }

  // ===========================================================================
  // FU001 diagnostic
  // ===========================================================================

  [Test]
  public void Generator_EmitsFU001_ForStepWithNoTests()
  {
    const string source = """
      using Flowthru.Core.Steps;

      [FlowthruStep]
      public static class UntestedStep { }
      """;

    var result = FUnitGeneratorTestHelper.RunFUnitGenerator(source);

    var fu001 = result.Diagnostics.FirstOrDefault(d => d.Id == "FU001");
    Assert.That(fu001, Is.Not.Null, "Expected FU001 diagnostic");
    Assert.That(fu001!.Severity, Is.EqualTo(DiagnosticSeverity.Warning));
    Assert.That(fu001.GetMessage(), Does.Contain("UntestedStep"));
  }

  [Test]
  public void Generator_DoesNotEmitFU001_WhenStepHasTests()
  {
    const string source = """
      using Flowthru.Core.Steps;
      using Flowthru.FUnit;

      [FlowthruStep]
      public static class TestedStep { }

      public class MyTests
      {
          [StepTest(typeof(TestedStep))]
          public void Test1() { }
      }
      """;

    var result = FUnitGeneratorTestHelper.RunFUnitGenerator(source);

    var fu001 = result.Diagnostics.Where(d => d.Id == "FU001").ToList();
    Assert.That(fu001, Is.Empty, "FU001 should not fire for a covered step");
  }

  // ===========================================================================
  // Cross-assembly aggregation (FUnitAggregate=true)
  // ===========================================================================

  [Test]
  public void Generator_EmitsRunners_ForStepTestsInReferencedAssembly_WhenAggregateEnabled()
  {
    // Compile a library that contains a [StepTest]-annotated method
    const string librarySource = """
      using Flowthru.Core.Steps;
      using Flowthru.FUnit;

      [FlowthruStep]
      public static class RemoteStep { }

      public class RemoteTests : FUnitContext
      {
          [StepTest(typeof(RemoteStep))]
          public void RemoteTest1() { }
      }
      """;

    var runtimePath = Path.GetDirectoryName(typeof(object).Assembly.Location)!;
    var sharedRefs = new List<MetadataReference>
    {
      MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
      MetadataReference.CreateFromFile(typeof(Enumerable).Assembly.Location),
      MetadataReference.CreateFromFile(Path.Combine(runtimePath, "System.Runtime.dll")),
      MetadataReference.CreateFromFile(typeof(CatalogAbstract).Assembly.Location),
      MetadataReference.CreateFromFile(typeof(FUnitContext).Assembly.Location),
    };

    var libCompilation = CSharpCompilation.Create(
      "RemoteLibrary",
      [CSharpSyntaxTree.ParseText(librarySource)],
      sharedRefs,
      new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary)
    );

    using var libStream = new System.IO.MemoryStream();
    var emitResult = libCompilation.Emit(libStream);
    Assert.That(emitResult.Success, Is.True, "Remote library compilation failed");
    libStream.Position = 0;
    var libRef = MetadataReference.CreateFromStream(libStream);

    // Aggregator project: no [StepTest] source of its own, but FUnitAggregate=true
    const string aggregatorSource = """
      using NUnit.Framework;
      """;

    var result = FUnitGeneratorTestHelper.RunFUnitGenerator(
      aggregatorSource,
      extraReferences:
      [
        libRef,
        MetadataReference.CreateFromFile(typeof(NUnit.Framework.TestAttribute).Assembly.Location),
      ],
      buildProperties: new Dictionary<string, string> { { "FUnitAggregate", "true" } }
    );

    Assert.That(result.Success, Is.True, FormatErrors(result));

    var allGenerated = result.AllGeneratedSources;
    var runnerSource = allGenerated.FirstOrDefault(s => s.Contains("RemoteTests"));
    Assert.That(runnerSource, Is.Not.Null, "Expected a runner class for RemoteTests");
    Assert.That(runnerSource, Does.Contain("TestFixture").Or.Contain("Test").Or.Contain("Fact"));
    Assert.That(runnerSource, Does.Contain("RemoteTest1"));
  }

  [Test]
  public void Generator_DoesNotEmitCrossAssemblyRunners_WhenAggregateDisabled()
  {
    // Same library as above
    const string librarySource = """
      using Flowthru.Core.Steps;
      using Flowthru.FUnit;

      [FlowthruStep]
      public static class RemoteStep2 { }

      public class RemoteTests2 : FUnitContext
      {
          [StepTest(typeof(RemoteStep2))]
          public void RemoteTest2() { }
      }
      """;

    var runtimePath = Path.GetDirectoryName(typeof(object).Assembly.Location)!;
    var sharedRefs = new List<MetadataReference>
    {
      MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
      MetadataReference.CreateFromFile(typeof(Enumerable).Assembly.Location),
      MetadataReference.CreateFromFile(Path.Combine(runtimePath, "System.Runtime.dll")),
      MetadataReference.CreateFromFile(typeof(CatalogAbstract).Assembly.Location),
      MetadataReference.CreateFromFile(typeof(FUnitContext).Assembly.Location),
    };

    var libCompilation = CSharpCompilation.Create(
      "RemoteLibrary2",
      [CSharpSyntaxTree.ParseText(librarySource)],
      sharedRefs,
      new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary)
    );

    using var libStream = new System.IO.MemoryStream();
    var emitResult = libCompilation.Emit(libStream);
    Assert.That(emitResult.Success, Is.True, "Remote library compilation failed");
    libStream.Position = 0;
    var libRef = MetadataReference.CreateFromStream(libStream);

    // FUnitAggregate intentionally absent
    const string aggregatorSource = """
      using NUnit.Framework;
      """;

    var result = FUnitGeneratorTestHelper.RunFUnitGenerator(
      aggregatorSource,
      extraReferences: [libRef],
      buildProperties: new Dictionary<string, string>()
    );

    Assert.That(result.Success, Is.True, FormatErrors(result));
    Assert.That(
      result.AllGeneratedSources.Any(s => s.Contains("RemoteTests2")),
      Is.False,
      "Runner for remote test class should not appear without FUnitAggregate=true"
    );
  }

  // ===========================================================================
  // Helpers
  // ===========================================================================

  private static string FormatErrors(FUnitGeneratorResult result) =>
    string.Join(
      "\n",
      result
        .Diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error)
        .Select(d => d.GetMessage())
    );
}
