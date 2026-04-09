using Flowthru.FUnit.SourceGenerators;
using Flowthru.FUnit.Tests.Fixtures;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;

namespace Flowthru.FUnit.Tests;

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

    var result = RunGenerator(source);

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

    var result = RunGenerator(source);

    Assert.That(result.GeneratedSource, Does.Contain("= 0"));
  }

  [Test]
  public void Generator_EmitsNoRegistry_WhenNoStepsExist()
  {
    const string source = """
      public class SomePlainClass { }
      """;

    var result = RunGenerator(source);

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

    var result = RunGenerator(source);

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

    var result = RunGenerator(source);

    var fu001 = result.Diagnostics.Where(d => d.Id == "FU001").ToList();
    Assert.That(fu001, Is.Empty, "FU001 should not fire for a covered step");
  }

  // ===========================================================================
  // Roslyn helper
  // ===========================================================================

  private static GeneratorResult RunGenerator(string source)
  {
    var syntaxTree = CSharpSyntaxTree.ParseText(source);

    var runtimePath = Path.GetDirectoryName(typeof(object).Assembly.Location)!;
    var references = new List<MetadataReference>
    {
      MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
      MetadataReference.CreateFromFile(typeof(Enumerable).Assembly.Location),
      MetadataReference.CreateFromFile(Path.Combine(runtimePath, "System.Runtime.dll")),
      MetadataReference.CreateFromFile(typeof(Core.Data.IItem).Assembly.Location),
      MetadataReference.CreateFromFile(typeof(FunitContext).Assembly.Location),
    };

    var compilation = CSharpCompilation.Create(
      "GeneratorTestAssembly",
      new[] { syntaxTree },
      references,
      new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary)
    );

    var generator = new StepTestRegistryGenerator();
    GeneratorDriver driver = CSharpGeneratorDriver.Create(generator);
    driver = driver.RunGeneratorsAndUpdateCompilation(
      compilation,
      out _,
      out var generatorDiagnostics
    );

    var runResult = driver.GetRunResult();
    var generatedSource = runResult
      .Results.SelectMany(r => r.GeneratedSources)
      .FirstOrDefault(s => s.HintName.Contains("StepTestRegistry"))
      .SourceText?.ToString();

    return new GeneratorResult(
      Success: !generatorDiagnostics.Any(d => d.Severity == DiagnosticSeverity.Error),
      Diagnostics: generatorDiagnostics.ToList(),
      GeneratedSource: generatedSource
    );
  }

  private static string FormatErrors(GeneratorResult result) =>
    string.Join(
      "\n",
      result
        .Diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error)
        .Select(d => d.GetMessage())
    );

  private sealed record GeneratorResult(
    bool Success,
    List<Diagnostic> Diagnostics,
    string? GeneratedSource
  );
}
