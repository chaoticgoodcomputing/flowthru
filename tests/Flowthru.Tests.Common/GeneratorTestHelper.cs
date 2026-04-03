using Flowthru.SourceGenerators.SchemaAnalysis;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Flowthru.Tests.Common;

/// <summary>
/// Helper for running source generator tests against in-memory compilations.
/// </summary>
public static class GeneratorTestHelper
{
  /// <summary>
  /// Runs the <see cref="SchemaInterfaceGenerator"/> against the given source code
  /// and returns the generator output alongside compilation diagnostics.
  /// </summary>
  public static GeneratorTestResult RunSchemaGenerator(string source)
  {
    var syntaxTree = CSharpSyntaxTree.ParseText(source);

    var references = new List<MetadataReference>
    {
      MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
      MetadataReference.CreateFromFile(typeof(Console).Assembly.Location),
      MetadataReference.CreateFromFile(typeof(Enumerable).Assembly.Location),
    };

    // .NET runtime references
    var runtimePath = Path.GetDirectoryName(typeof(object).Assembly.Location)!;
    references.Add(
      MetadataReference.CreateFromFile(Path.Combine(runtimePath, "System.Runtime.dll"))
    );
    references.Add(
      MetadataReference.CreateFromFile(Path.Combine(runtimePath, "System.Collections.dll"))
    );

    // Flowthru assembly — so the marker interfaces resolve
    references.Add(
      MetadataReference.CreateFromFile(typeof(Data.CatalogAbstract).Assembly.Location)
    );

    var compilation = CSharpCompilation.Create(
      "GeneratorTestAssembly",
      new[] { syntaxTree },
      references,
      new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary)
    );

    var generator = new SchemaInterfaceGenerator();
    GeneratorDriver driver = CSharpGeneratorDriver.Create(generator);
    driver = driver.RunGeneratorsAndUpdateCompilation(
      compilation,
      out var outputCompilation,
      out var generatorDiagnostics
    );

    var runResult = driver.GetRunResult();
    var allDiagnostics = outputCompilation.GetDiagnostics().Concat(generatorDiagnostics).ToList();

    var errors = allDiagnostics.Where(d => d.Severity == DiagnosticSeverity.Error).ToList();

    return new GeneratorTestResult
    {
      Success = !errors.Any(),
      Diagnostics = allDiagnostics,
      GeneratorDiagnostics = generatorDiagnostics.ToList(),
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
