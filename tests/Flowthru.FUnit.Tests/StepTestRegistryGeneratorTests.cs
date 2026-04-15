using System.Collections.Generic;
using System.Collections.Immutable;
using Flowthru.FUnit.SourceGenerators;
using Flowthru.FUnit.Tests.Fixtures;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
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

      public class RemoteTests : FunitContext
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
      MetadataReference.CreateFromFile(typeof(Core.Data.IItem).Assembly.Location),
      MetadataReference.CreateFromFile(typeof(FunitContext).Assembly.Location),
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

        var result = RunGenerator(
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

      public class RemoteTests2 : FunitContext
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
      MetadataReference.CreateFromFile(typeof(Core.Data.IItem).Assembly.Location),
      MetadataReference.CreateFromFile(typeof(FunitContext).Assembly.Location),
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

        var result = RunGenerator(
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
    // Roslyn helper
    // ===========================================================================

    private static GeneratorResult RunGenerator(string source) =>
      RunGenerator(source, extraReferences: [], buildProperties: new Dictionary<string, string>());

    private static GeneratorResult RunGenerator(
      string source,
      IReadOnlyList<MetadataReference> extraReferences,
      IReadOnlyDictionary<string, string> buildProperties
    )
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

        references.AddRange(extraReferences);

        var compilation = CSharpCompilation.Create(
          "GeneratorTestAssembly",
          new[] { syntaxTree },
          references,
          new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary)
        );

        var generator = new StepTestRegistryGenerator();

        var optionsProvider = new TestAnalyzerConfigOptionsProvider(buildProperties);
        GeneratorDriver driver = CSharpGeneratorDriver.Create(
          generators: [generator.AsSourceGenerator()],
          optionsProvider: optionsProvider
        );

        driver = driver.RunGeneratorsAndUpdateCompilation(
          compilation,
          out var outputCompilation,
          out var generatorDiagnostics
        );

        var runResult = driver.GetRunResult();
        var allGeneratedSources = runResult
          .Results.SelectMany(r => r.GeneratedSources)
          .Select(s => s.SourceText.ToString())
          .ToList();

        var generatedSource = allGeneratedSources.FirstOrDefault(s => s.Contains("StepTestRegistry"));

        // Also run the FUnit diagnostic analyzer so FU001/FU002 appear in Diagnostics.
        var analyzerDiagnostics = outputCompilation
          .WithAnalyzers(ImmutableArray.Create<DiagnosticAnalyzer>(new FunitDiagnosticAnalyzer()))
          .GetAnalyzerDiagnosticsAsync()
          .GetAwaiter()
          .GetResult();

        var allDiagnostics = generatorDiagnostics.Concat(analyzerDiagnostics).ToList();

        return new GeneratorResult(
          Success: !allDiagnostics.Any(d => d.Severity == DiagnosticSeverity.Error),
          Diagnostics: allDiagnostics,
          GeneratedSource: generatedSource,
          AllGeneratedSources: allGeneratedSources
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
      string? GeneratedSource,
      List<string> AllGeneratedSources
    );

    // Minimal AnalyzerConfigOptionsProvider that surfaces build_property.* entries.
    private sealed class TestAnalyzerConfigOptionsProvider(
      IReadOnlyDictionary<string, string> properties
    ) : AnalyzerConfigOptionsProvider
    {
        public override AnalyzerConfigOptions GlobalOptions { get; } =
          new TestAnalyzerConfigOptions(properties);

        public override AnalyzerConfigOptions GetOptions(SyntaxTree tree) => GlobalOptions;

        public override AnalyzerConfigOptions GetOptions(AdditionalText textFile) => GlobalOptions;
    }

    private sealed class TestAnalyzerConfigOptions(IReadOnlyDictionary<string, string> properties)
      : AnalyzerConfigOptions
    {
        public override bool TryGetValue(string key, out string value)
        {
            // build_property.FUnitAggregate → properties["FUnitAggregate"]
            if (key.StartsWith("build_property.", StringComparison.OrdinalIgnoreCase))
            {
                var shortKey = key.Substring("build_property.".Length);
                return properties.TryGetValue(shortKey, out value!);
            }

            value = null!;
            return false;
        }
    }
}
