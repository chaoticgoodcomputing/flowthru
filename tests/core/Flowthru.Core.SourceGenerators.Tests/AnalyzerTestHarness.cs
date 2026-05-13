using System.Collections.Immutable;
using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Flowthru.Core.SourceGenerators.Tests;

/// <summary>
/// Minimal harness for running Roslyn analyzers and incremental
/// generators against inline C# sources. Hand-rolled instead of leaning
/// on <c>Microsoft.CodeAnalysis.Testing</c> — keeps the test project
/// dependency-free and the diagnostics rendering legible.
/// </summary>
internal static class AnalyzerTestHarness
{
  /// <summary>
  /// Compile <paramref name="source"/> against the standard reference
  /// set plus <paramref name="extraReferences"/>, attach
  /// <paramref name="analyzer"/>, and return every diagnostic the
  /// analyzer reports. Compilation errors are also returned so
  /// fixtures can sanity-check that the source itself parsed.
  /// </summary>
  public static async Task<ImmutableArray<Diagnostic>> RunAsync(
    DiagnosticAnalyzer analyzer,
    string source,
    params Assembly[] extraReferences
  ) => await RunAsync(analyzer, new[] { source }, extraReferences).ConfigureAwait(false);

  /// <summary>
  /// Multi-source overload of <see cref="RunAsync(DiagnosticAnalyzer,string,Assembly[])"/>.
  /// Useful when a fixture needs to split stubs and the consumer's
  /// declaration into separate syntax trees.
  /// </summary>
  public static async Task<ImmutableArray<Diagnostic>> RunAsync(
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
  /// returning the augmented compilation, any generator diagnostics,
  /// and the emitted sources keyed by hint name. Mirrors the
  /// <c>RunGenerator</c> shape used in
  /// <c>tests/core/Flowthru.FUnit.SourceGenerators.Tests/AnalyzerTestHarness.cs</c>
  /// so fixtures port between projects without changing call sites.
  /// </summary>
  public static GeneratorRunResultPayload RunGenerator(
    IIncrementalGenerator generator,
    IEnumerable<string> sources,
    params Assembly[] extraReferences
  )
  {
    var compilation = CreateCompilation(sources, extraReferences);
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

  /// <summary>Single-source convenience overload of <see cref="RunGenerator"/>.</summary>
  public static GeneratorRunResultPayload RunGenerator(
    IIncrementalGenerator generator,
    string source,
    params Assembly[] extraReferences
  ) => RunGenerator(generator, new[] { source }, extraReferences);

  private static CSharpCompilation CreateCompilation(
    IEnumerable<string> sources,
    Assembly[] extraReferences
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

    return CSharpCompilation.Create(
      assemblyName: "AnalyzerTestAssembly",
      syntaxTrees: syntaxTrees,
      references: references,
      options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary)
    );
  }

  /// <summary>
  /// Filter to diagnostics matching <paramref name="diagnosticId"/>.
  /// Most fixtures want to know "did FT0001 fire?" rather than "did
  /// any diagnostic fire?".
  /// </summary>
  public static IEnumerable<Diagnostic> Where(this IEnumerable<Diagnostic> diagnostics, string diagnosticId) =>
    diagnostics.Where(d => d.Id == diagnosticId);
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
