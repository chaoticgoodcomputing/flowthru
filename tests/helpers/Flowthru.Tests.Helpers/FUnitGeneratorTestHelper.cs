using System.Collections.Immutable;
using Flowthru.Core.Data;
using Flowthru.FUnit;
using Flowthru.FUnit.SourceGenerators;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Flowthru.Tests.Helpers;

/// <summary>
/// Runs the FUnit source generator and analyzer against an in-memory compilation.
/// This helper intentionally lives in <c>Flowthru.Tests.Helpers</c>, which holds
/// <c>ProjectReference</c>s to both <c>Flowthru.FUnit</c> and
/// <c>Flowthru.FUnit.SourceGenerators</c>. Having the helper here forces the CLR to
/// load the instrumented generator assembly as a transitive dependency of this project
/// before any test method runs, giving coverlet visibility into every call.
/// </summary>
public static class FUnitGeneratorTestHelper
{
  public static FUnitGeneratorResult RunFUnitGenerator(string source) =>
    RunFUnitGenerator(
      source,
      extraReferences: [],
      buildProperties: new Dictionary<string, string>()
    );

  public static FUnitGeneratorResult RunFUnitGenerator(
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
      MetadataReference.CreateFromFile(typeof(CatalogAbstract).Assembly.Location),
      MetadataReference.CreateFromFile(typeof(FUnitContext).Assembly.Location),
    };

    references.AddRange(extraReferences);

    var compilation = CSharpCompilation.Create(
      "GeneratorTestAssembly",
      [syntaxTree],
      references,
      new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary)
    );

    var generator = new StepTestRegistryGenerator();
    var optionsProvider = new FUnitAnalyzerConfigOptionsProvider(buildProperties);
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

    var analyzerDiagnostics = outputCompilation
      .WithAnalyzers(ImmutableArray.Create<DiagnosticAnalyzer>(new FUnitDiagnosticAnalyzer()))
      .GetAnalyzerDiagnosticsAsync()
      .GetAwaiter()
      .GetResult();

    var allDiagnostics = generatorDiagnostics.Concat(analyzerDiagnostics).ToList();

    return new FUnitGeneratorResult(
      Success: !allDiagnostics.Any(d => d.Severity == DiagnosticSeverity.Error),
      Diagnostics: allDiagnostics,
      GeneratedSource: generatedSource,
      AllGeneratedSources: allGeneratedSources
    );
  }
}

public sealed record FUnitGeneratorResult(
  bool Success,
  List<Diagnostic> Diagnostics,
  string? GeneratedSource,
  List<string> AllGeneratedSources
);

internal sealed class FUnitAnalyzerConfigOptionsProvider(
  IReadOnlyDictionary<string, string> properties
) : AnalyzerConfigOptionsProvider
{
  public override AnalyzerConfigOptions GlobalOptions { get; } =
    new FUnitAnalyzerConfigOptions(properties);

  public override AnalyzerConfigOptions GetOptions(SyntaxTree tree) => GlobalOptions;

  public override AnalyzerConfigOptions GetOptions(AdditionalText textFile) => GlobalOptions;
}

internal sealed class FUnitAnalyzerConfigOptions(IReadOnlyDictionary<string, string> properties)
  : AnalyzerConfigOptions
{
  public override bool TryGetValue(string key, out string value)
  {
    if (key.StartsWith("build_property.", StringComparison.OrdinalIgnoreCase))
    {
      var shortKey = key.Substring("build_property.".Length);
      return properties.TryGetValue(shortKey, out value!);
    }

    value = null!;
    return false;
  }
}
