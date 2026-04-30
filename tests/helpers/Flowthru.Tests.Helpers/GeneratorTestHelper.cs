using System.Collections.Immutable;
using Flowthru.Core.Data;
using Flowthru.Core.SourceGenerators;
using Flowthru.Core.SourceGenerators.SchemaAnalysis;
using Flowthru.Core.SourceGenerators.StepAnalysis;
using Flowthru.Core.Steps;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.Extensions.Configuration;

namespace Flowthru.Tests.Helpers;

/// <summary>
/// Helper for running source generator tests against in-memory compilations.
/// </summary>
public static class GeneratorTestHelper
{
  /// <summary>
  /// Runs the <see cref="SchemaInterfaceGenerator"/> against the given source code
  /// and returns the generator output alongside compilation diagnostics.
  /// </summary>
  public static GeneratorTestResult RunSchemaGenerator(string source) =>
    RunGenerators(
      source,
      assemblyName: "GeneratorTestAssembly",
      generators: new IIncrementalGenerator[] { new SchemaInterfaceGenerator() },
      includeAnalyzer: true
    );

  /// <summary>
  /// Runs the <see cref="ConfigCatalogGenerator"/> against the given source code. Includes the
  /// references needed for <c>[FlowthruConfig]</c> + <c>[ConfigSection]</c> partial classes
  /// to resolve (<c>Flowthru.Core</c>, <c>Microsoft.Extensions.Configuration</c>).
  /// </summary>
  public static GeneratorTestResult RunConfigCatalogGenerator(string source) =>
    RunGenerators(
      source,
      assemblyName: "ConfigCatalogTestAssembly",
      generators: new IIncrementalGenerator[] { new ConfigCatalogGenerator() }
    );

  /// <summary>
  /// Runs the <see cref="FlowBuilderGenerator"/>. The generator is gated on the consuming
  /// assembly name being <c>"Flowthru.Core"</c>, so the test forces that name. The result's
  /// generated sources contain the <c>FlowBuilder.Generated.cs</c> AddStep overloads.
  /// </summary>
  public static GeneratorTestResult RunFlowBuilderGenerator() =>
    RunGenerators(
      // Empty source — FlowBuilderGenerator doesn't depend on consumer code, only assembly name.
      source: "namespace Flowthru.Core.Flows { internal class _Marker { } }",
      assemblyName: "Flowthru.Core",
      generators: new IIncrementalGenerator[] { new FlowBuilderGenerator() }
    );

  /// <summary>
  /// Runs the <see cref="FlowBuilderGenerator"/> against a compilation with a non-Core
  /// assembly name. Used to assert the generator is correctly gated and emits nothing for
  /// consumer projects.
  /// </summary>
  public static GeneratorTestResult RunFlowBuilderGeneratorAsConsumer() =>
    RunGenerators(
      source: "namespace Test { internal class _Marker { } }",
      assemblyName: "Some.Consumer.Assembly",
      generators: new IIncrementalGenerator[] { new FlowBuilderGenerator() }
    );

  /// <summary>
  /// Runs the <see cref="StepMetadataGenerator"/> against the given source. Caller must
  /// include a <c>[FlowthruStep]</c>-attributed class in the source for any output to be
  /// emitted.
  /// </summary>
  public static GeneratorTestResult RunStepMetadataGenerator(string source) =>
    RunGenerators(
      source,
      assemblyName: "StepMetadataTestAssembly",
      generators: new IIncrementalGenerator[] { new StepMetadataGenerator() }
    );

  private static GeneratorTestResult RunGenerators(
    string source,
    string assemblyName,
    IIncrementalGenerator[] generators,
    bool includeAnalyzer = false
  )
  {
    var syntaxTree = CSharpSyntaxTree.ParseText(source);

    var references = new List<MetadataReference>
    {
      MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
      MetadataReference.CreateFromFile(typeof(Console).Assembly.Location),
      MetadataReference.CreateFromFile(typeof(Enumerable).Assembly.Location),
    };

    var runtimePath = Path.GetDirectoryName(typeof(object).Assembly.Location)!;
    references.Add(
      MetadataReference.CreateFromFile(Path.Combine(runtimePath, "System.Runtime.dll"))
    );
    references.Add(
      MetadataReference.CreateFromFile(Path.Combine(runtimePath, "System.Collections.dll"))
    );

    references.Add(MetadataReference.CreateFromFile(typeof(CatalogAbstract).Assembly.Location));
    references.Add(
      MetadataReference.CreateFromFile(typeof(IConfiguration).Assembly.Location)
    );

    var compilation = CSharpCompilation.Create(
      assemblyName,
      new[] { syntaxTree },
      references,
      new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary)
    );

    GeneratorDriver driver = CSharpGeneratorDriver.Create(generators);
    driver = driver.RunGeneratorsAndUpdateCompilation(
      compilation,
      out var outputCompilation,
      out var generatorDiagnostics
    );

    var runResult = driver.GetRunResult();
    var allDiagnostics = outputCompilation.GetDiagnostics().Concat(generatorDiagnostics).ToList();
    var combinedGeneratorDiagnostics = generatorDiagnostics.ToList();

    if (includeAnalyzer)
    {
      var analyzers = ImmutableArray.Create<DiagnosticAnalyzer>(new FlowthruSchemaAnalyzer());
      var analyzerDiagnostics = outputCompilation
        .WithAnalyzers(analyzers)
        .GetAnalyzerDiagnosticsAsync()
        .GetAwaiter()
        .GetResult();

      combinedGeneratorDiagnostics = combinedGeneratorDiagnostics
        .Concat(analyzerDiagnostics)
        .ToList();
      allDiagnostics = allDiagnostics.Concat(analyzerDiagnostics).ToList();
    }

    var errors = allDiagnostics.Where(d => d.Severity == DiagnosticSeverity.Error).ToList();

    return new GeneratorTestResult
    {
      Success = !errors.Any(),
      Diagnostics = allDiagnostics,
      GeneratorDiagnostics = combinedGeneratorDiagnostics,
      GeneratedSources = runResult.Results.SelectMany(r => r.GeneratedSources).ToList(),
      OutputCompilation = outputCompilation,
    };
  }
}

/// <summary>
/// Result of running a source generator test.
/// </summary>
public class GeneratorTestResult
{
  /// <summary>
  /// Whether the resulting compilation succeeded without errors.
  /// </summary>
  public required bool Success { get; init; }

  /// <summary>
  /// All diagnostics (compilation + generator).
  /// </summary>
  public required List<Diagnostic> Diagnostics { get; init; }

  /// <summary>
  /// Diagnostics emitted by the generator itself (FT1001, FT1002, etc.).
  /// </summary>
  public required List<Diagnostic> GeneratorDiagnostics { get; init; }

  /// <summary>
  /// Source files emitted by the generator.
  /// </summary>
  public required List<GeneratedSourceResult> GeneratedSources { get; init; }

  /// <summary>
  /// The final compilation with generated sources applied.
  /// </summary>
  public required Compilation OutputCompilation { get; init; }

  /// <summary>
  /// Gets generated source text by hint name suffix.
  /// </summary>
  public string? GetGeneratedSource(string hintNameContains) =>
    GeneratedSources
      .FirstOrDefault(s => s.HintName.Contains(hintNameContains))
      .SourceText?.ToString();
}
