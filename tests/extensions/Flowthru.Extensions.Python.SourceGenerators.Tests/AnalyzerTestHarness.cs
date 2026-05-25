using System.Collections.Immutable;
using System.Reflection;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;

namespace Flowthru.Extensions.Python.SourceGenerators.Tests;

/// <summary>
/// Minimal harness for running a Roslyn <see cref="DiagnosticAnalyzer"/>
/// against an inline C# source plus optional <c>AdditionalText</c>
/// entries (notably uv.lock for the requirements analyzer). Mirrors
/// the spirit of Core's AnalyzerTestHarness but adds explicit
/// AdditionalFiles support so the requirements analyzer can be
/// driven without a real consumer csproj.
/// </summary>
internal static class AnalyzerTestHarness
{
  /// <summary>
  /// Adapter that wraps a string as an <see cref="AdditionalText"/>.
  /// Reused so callers can feed the requirements analyzer synthetic
  /// uv.lock contents without spawning files on disk.
  /// </summary>
  internal sealed class InMemoryAdditionalText : AdditionalText
  {
    private readonly string _text;
    public override string Path { get; }

    public InMemoryAdditionalText(string path, string text)
    {
      Path = path;
      _text = text;
    }

    public override SourceText GetText(CancellationToken cancellationToken = default) =>
      SourceText.From(_text, Encoding.UTF8);
  }

  /// <summary>
  /// Compile <paramref name="source"/> with the standard reference set
  /// plus <paramref name="extraReferences"/>, attach
  /// <paramref name="analyzer"/> with the supplied
  /// <paramref name="additionalFiles"/>, and return every diagnostic
  /// the analyzer reports.
  /// </summary>
  public static async Task<ImmutableArray<Diagnostic>> RunAsync(
    DiagnosticAnalyzer analyzer,
    string source,
    IReadOnlyList<AdditionalText>? additionalFiles = null,
    params Assembly[] extraReferences
  )
  {
    var compilation = CreateCompilation(new[] { source }, extraReferences);

    var options = additionalFiles is null
      ? new AnalyzerOptions(ImmutableArray<AdditionalText>.Empty)
      : new AnalyzerOptions(additionalFiles.ToImmutableArray());

    var withAnalyzers = compilation.WithAnalyzers(
      ImmutableArray.Create(analyzer),
      options
    );

    return await withAnalyzers.GetAllDiagnosticsAsync().ConfigureAwait(false);
  }

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

  // No `Where(IEnumerable<Diagnostic>, string)` extension here —
  // GeneratorTestHarness already exposes one in the same namespace,
  // so test code uses that without an ambiguity.
}
