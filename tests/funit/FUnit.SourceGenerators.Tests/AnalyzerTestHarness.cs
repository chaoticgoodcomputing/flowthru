using System.Collections.Immutable;
using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;

namespace FUnit.SourceGenerators.Tests;

/// <summary>
/// Minimal harness for running Roslyn analyzers and incremental
/// generators against inline C# sources. Hand-rolled to avoid pulling
/// the Microsoft.CodeAnalysis.Testing harness — keeps the test project
/// dependency-light and the assertions easy to read.
/// </summary>
internal static class AnalyzerTestHarness
{
  /// <summary>
  /// Compile <paramref name="sources"/> against the standard reference
  /// set plus <paramref name="extraReferences"/>, attach the supplied
  /// analyzer, and return every diagnostic produced (analyzer +
  /// compiler). Multi-source so fixtures can split stubs and consumer
  /// code without manually concatenating strings.
  /// </summary>
  public static async Task<ImmutableArray<Diagnostic>> RunAnalyzerAsync(
    DiagnosticAnalyzer analyzer,
    IEnumerable<string> sources,
    params Assembly[] extraReferences
  )
  {
    var compilation = CreateCompilation(sources, extraReferences);
    var withAnalyzers = compilation.WithAnalyzers(ImmutableArray.Create(analyzer));
    return await withAnalyzers.GetAllDiagnosticsAsync().ConfigureAwait(false);
  }

  /// <summary>
  /// Compile <paramref name="sources"/> and drive
  /// <paramref name="generator"/> against the resulting compilation,
  /// returning the generated sources plus any generator diagnostics.
  /// </summary>
  public static GeneratorRunResultPayload RunGenerator(
    IIncrementalGenerator generator,
    IEnumerable<string> sources,
    params Assembly[] extraReferences
  ) => RunGenerator(generator, sources, extraReferences, System.Array.Empty<MetadataReference>());

  /// <summary>
  /// Generator-runner overload that accepts both reflection
  /// <see cref="Assembly"/> references (loaded by file path) and
  /// pre-built <see cref="MetadataReference"/>s — needed for stub
  /// assemblies emitted to disk that don't have a backing
  /// <c>Assembly</c> in the current AppDomain.
  /// </summary>
  public static GeneratorRunResultPayload RunGenerator(
    IIncrementalGenerator generator,
    IEnumerable<string> sources,
    Assembly[] extraReferences,
    MetadataReference[] extraMetadataReferences
  )
  {
    var compilation = CreateCompilation(sources, extraReferences, extraMetadataReferences);
    var driver = CSharpGeneratorDriver
      .Create(generator.AsSourceGenerator())
      .RunGeneratorsAndUpdateCompilation(
        compilation,
        out var outputCompilation,
        out var diagnostics
      );

    var runResult = driver.GetRunResult();
    var generatedSources = runResult
      .Results.SelectMany(r => r.GeneratedSources)
      .Select(s => (HintName: s.HintName, Source: s.SourceText.ToString()))
      .ToList();

    return new GeneratorRunResultPayload(
      OutputCompilation: outputCompilation,
      Diagnostics: diagnostics,
      GeneratedSources: generatedSources
    );
  }

  private static CSharpCompilation CreateCompilation(
    IEnumerable<string> sources,
    Assembly[] extraReferences
  ) => CreateCompilation(sources, extraReferences, System.Array.Empty<MetadataReference>());

  private static CSharpCompilation CreateCompilation(
    IEnumerable<string> sources,
    Assembly[] extraReferences,
    MetadataReference[] extraMetadataReferences
  )
  {
    var syntaxTrees = sources.Select(s => CSharpSyntaxTree.ParseText(s)).ToList();

    var references = new List<MetadataReference>
    {
      MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
      MetadataReference.CreateFromFile(typeof(Enumerable).Assembly.Location),
      MetadataReference.CreateFromFile(typeof(Console).Assembly.Location),
      MetadataReference.CreateFromFile(typeof(Task).Assembly.Location),
      MetadataReference.CreateFromFile(typeof(Attribute).Assembly.Location),
    };
    foreach (var name in new[]
    {
      "System.Runtime",
      "System.Collections",
      "System.Linq",
      "netstandard",
      "System.Threading.Tasks",
    })
    {
      var asm = AppDomain.CurrentDomain.GetAssemblies()
        .FirstOrDefault(a => a.GetName().Name == name);
      if (asm is not null && !string.IsNullOrEmpty(asm.Location))
      {
        references.Add(MetadataReference.CreateFromFile(asm.Location));
      }
    }
    foreach (var asm in extraReferences)
    {
      references.Add(MetadataReference.CreateFromFile(asm.Location));
    }
    foreach (var mref in extraMetadataReferences)
    {
      references.Add(mref);
    }

    return CSharpCompilation.Create(
      assemblyName: "AnalyzerTestAssembly",
      syntaxTrees: syntaxTrees,
      references: references,
      options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary)
    );
  }

  /// <summary>Filter diagnostics to a specific rule id.</summary>
  public static IEnumerable<Diagnostic> WithId(this IEnumerable<Diagnostic> diagnostics, string id) =>
    diagnostics.Where(d => d.Id == id);
}

/// <summary>
/// Result of driving an <see cref="IIncrementalGenerator"/> through a
/// <see cref="CSharpGeneratorDriver"/> — the augmented compilation,
/// any generator diagnostics, and the emitted sources keyed by hint
/// name.
/// </summary>
internal sealed record GeneratorRunResultPayload(
  Compilation OutputCompilation,
  ImmutableArray<Diagnostic> Diagnostics,
  IReadOnlyList<(string HintName, string Source)> GeneratedSources
);
