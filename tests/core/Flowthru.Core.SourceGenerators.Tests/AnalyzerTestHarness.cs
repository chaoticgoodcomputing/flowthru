using System.Collections.Immutable;
using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Flowthru.Core.SourceGenerators.Tests;

/// <summary>
/// Minimal harness for running a Roslyn <see cref="DiagnosticAnalyzer"/>
/// against an inline C# source string and asserting the produced
/// diagnostic IDs. Intentionally hand-rolled instead of leaning on
/// <c>Microsoft.CodeAnalysis.Testing</c> — keeps the test project
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
  )
  {
    var syntaxTree = CSharpSyntaxTree.ParseText(source);
    var references = new List<MetadataReference>
    {
      MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
      MetadataReference.CreateFromFile(typeof(Enumerable).Assembly.Location),
      MetadataReference.CreateFromFile(typeof(Console).Assembly.Location),
      MetadataReference.CreateFromFile(typeof(Task).Assembly.Location),
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

    var compilation = CSharpCompilation.Create(
      assemblyName: "AnalyzerTestAssembly",
      syntaxTrees: new[] { syntaxTree },
      references: references,
      options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary)
    );

    var withAnalyzers = compilation.WithAnalyzers(ImmutableArray.Create(analyzer));
    return await withAnalyzers.GetAllDiagnosticsAsync().ConfigureAwait(false);
  }

  /// <summary>
  /// Filter to diagnostics matching <paramref name="diagnosticId"/>.
  /// Most fixtures want to know "did FT0001 fire?" rather than "did
  /// any diagnostic fire?".
  /// </summary>
  public static IEnumerable<Diagnostic> Where(this IEnumerable<Diagnostic> diagnostics, string diagnosticId) =>
    diagnostics.Where(d => d.Id == diagnosticId);
}
