using System.Collections.Immutable;
using System.Reflection;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;

namespace Flowthru.Extensions.Python.SourceGenerators.Tests;

/// <summary>
/// Minimal harness for running a Roslyn <see cref="IIncrementalGenerator"/>
/// against an inline C# source string plus optional Python
/// <c>AdditionalFiles</c>, and inspecting the resulting generated trees
/// and diagnostics. Mirrors the spirit of
/// <c>AnalyzerTestHarness</c> from <c>Flowthru.Core.SourceGenerators.Tests</c>
/// but specialised to <c>ISourceGenerator</c>-driven workflows where we
/// also need to feed <c>AdditionalText</c> Python files into the
/// incremental pipeline.
/// </summary>
internal static class GeneratorTestHarness
{
  /// <summary>
  /// Adapter that wraps an inline string as an
  /// <see cref="AdditionalText"/> with the given (virtual) path. The
  /// Python source-generator's discovery filters on a <c>.py</c> path
  /// suffix and reads the text through this contract, so this is the
  /// smallest faithful stand-in for a real <c>AdditionalFiles</c>
  /// entry in a consumer csproj.
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
  /// Result bundle returned by <see cref="Run"/>. Captures both the
  /// diagnostics raised by the generator and the source texts it
  /// added, keyed by their hint name (e.g. <c>PythonSteps.g.cs</c>),
  /// so individual fixtures can inspect each output without re-running
  /// the driver.
  /// </summary>
  public sealed record GeneratorRunResult(
    ImmutableArray<Diagnostic> Diagnostics,
    IReadOnlyDictionary<string, string> GeneratedSources
  );

  /// <summary>
  /// Compile <paramref name="source"/> with the standard reference set
  /// plus any <paramref name="extraReferences"/>, attach
  /// <paramref name="generator"/> and the optional Python
  /// <paramref name="additionalFiles"/>, and return everything the
  /// driver produced. The default
  /// <c>assemblyName</c> is the same name the
  /// <see cref="PythonStepGenerator"/> uses to gate emission on
  /// (<c>Flowthru.Extensions.Python</c>), so fixtures don't have to
  /// pass it explicitly. Override it to exercise the
  /// "skipped for non-Python compilation" branch.
  /// </summary>
  public static GeneratorRunResult Run(
    IIncrementalGenerator generator,
    string source,
    IReadOnlyList<AdditionalText>? additionalFiles = null,
    string assemblyName = "Flowthru.Extensions.Python",
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
      assemblyName: assemblyName,
      syntaxTrees: new[] { syntaxTree },
      references: references,
      options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary)
    );

    GeneratorDriver driver = CSharpGeneratorDriver.Create(
      generators: new[] { generator.AsSourceGenerator() },
      additionalTexts: additionalFiles is null
        ? ImmutableArray<AdditionalText>.Empty
        : additionalFiles.ToImmutableArray()
    );

    driver = driver.RunGeneratorsAndUpdateCompilation(
      compilation,
      out _,
      out var diagnostics
    );

    var runResult = driver.GetRunResult();
    var generated = new Dictionary<string, string>(StringComparer.Ordinal);
    foreach (var result in runResult.Results)
    {
      foreach (var src in result.GeneratedSources)
      {
        generated[src.HintName] = src.SourceText.ToString();
      }
    }

    return new GeneratorRunResult(diagnostics, generated);
  }

  /// <summary>
  /// Filter to diagnostics matching <paramref name="diagnosticId"/>.
  /// Same convenience extension as the analyzer harness in
  /// <c>Flowthru.Core.SourceGenerators.Tests</c>.
  /// </summary>
  public static IEnumerable<Diagnostic> Where(this IEnumerable<Diagnostic> diagnostics, string diagnosticId) =>
    diagnostics.Where(d => d.Id == diagnosticId);
}
