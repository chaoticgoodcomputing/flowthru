using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Flowthru.Tests.Common;

/// <summary>
/// Helper for compiling C# code snippets using Roslyn to verify compile-time type safety.
/// </summary>
/// <remarks>
/// Used for testing that certain code patterns produce compile-time errors as expected,
/// ensuring that Flowthru's type safety guarantees are maintained.
/// </remarks>
public static class CompilationTestHelper
{
  /// <summary>
  /// Compiles C# code and returns the compilation result with diagnostics.
  /// </summary>
  /// <param name="code">The C# code to compile</param>
  /// <param name="includeFlowthru">Whether to include Flowthru assembly references</param>
  /// <param name="additionalAssemblies">Additional assemblies to reference (e.g., test fixture assemblies)</param>
  /// <returns>Compilation result with success status and diagnostics</returns>
  public static CompilationResult Compile(
    string code,
    bool includeFlowthru = false,
    params Type[] additionalAssemblies
  )
  {
    var syntaxTree = CSharpSyntaxTree.ParseText(code);

    var references = new List<MetadataReference>
    {
      MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
      MetadataReference.CreateFromFile(typeof(Console).Assembly.Location),
      MetadataReference.CreateFromFile(typeof(Enumerable).Assembly.Location),
    };

    // Add .NET runtime references
    var runtimePath = Path.GetDirectoryName(typeof(object).Assembly.Location)!;
    references.Add(
      MetadataReference.CreateFromFile(Path.Combine(runtimePath, "System.Runtime.dll"))
    );
    references.Add(
      MetadataReference.CreateFromFile(Path.Combine(runtimePath, "System.Collections.dll"))
    );

    if (includeFlowthru)
    {
      // Add Flowthru assembly reference
      references.Add(
        MetadataReference.CreateFromFile(typeof(Data.DataCatalogBase).Assembly.Location)
      );

      // Add LanguageExt for Flowthru dependencies
      references.Add(MetadataReference.CreateFromFile(typeof(LanguageExt.IO<>).Assembly.Location));
    }

    // Add any additional assemblies (e.g., test fixtures)
    foreach (var type in additionalAssemblies)
    {
      var assemblyLocation = type.Assembly.Location;
      if (!string.IsNullOrEmpty(assemblyLocation))
      {
        references.Add(MetadataReference.CreateFromFile(assemblyLocation));
      }
    }

    var compilation = CSharpCompilation.Create(
      "TestAssembly",
      new[] { syntaxTree },
      references,
      new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary)
    );

    var diagnostics = compilation.GetDiagnostics();
    var errors = diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error).ToList();

    return new CompilationResult { Success = !errors.Any(), Diagnostics = diagnostics.ToList() };
  }

  /// <summary>
  /// Compiles C# code with ML.NET and ML.Next assembly references.
  /// </summary>
  /// <param name="code">The C# code to compile</param>
  /// <returns>Compilation result with success status and diagnostics</returns>
  public static CompilationResult CompileWithMLExt(string code)
  {
    var syntaxTree = CSharpSyntaxTree.ParseText(code);

    var references = new List<MetadataReference>
    {
      MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
      MetadataReference.CreateFromFile(typeof(Console).Assembly.Location),
      MetadataReference.CreateFromFile(typeof(Enumerable).Assembly.Location),
    };

    // Add .NET runtime references
    var runtimePath = Path.GetDirectoryName(typeof(object).Assembly.Location)!;
    references.Add(
      MetadataReference.CreateFromFile(Path.Combine(runtimePath, "System.Runtime.dll"))
    );
    references.Add(
      MetadataReference.CreateFromFile(Path.Combine(runtimePath, "System.Collections.dll"))
    );
    references.Add(MetadataReference.CreateFromFile(Path.Combine(runtimePath, "System.Linq.dll")));
    references.Add(
      MetadataReference.CreateFromFile(Path.Combine(runtimePath, "System.Linq.Expressions.dll"))
    );
    references.Add(MetadataReference.CreateFromFile(Path.Combine(runtimePath, "netstandard.dll")));

    // Add Microsoft.ML assembly references - need all the different packages
    references.Add(
      MetadataReference.CreateFromFile(typeof(Microsoft.ML.IDataView).Assembly.Location)
    ); // Microsoft.ML.DataView
    references.Add(
      MetadataReference.CreateFromFile(typeof(Microsoft.ML.MLContext).Assembly.Location)
    ); // Microsoft.ML.Core + Microsoft.ML.Data

    // Add ML.NET trainer packages by finding them through the MLContext's clustering trainers
    var mlContextType = typeof(Microsoft.ML.MLContext);
    var mlContextAssembly = mlContextType.Assembly;

    // Get all loaded assemblies that are ML.NET related
    var mlNetAssemblies = AppDomain
      .CurrentDomain.GetAssemblies()
      .Where(a => a.GetName().Name?.StartsWith("Microsoft.ML") == true)
      .ToList();

    foreach (var assembly in mlNetAssemblies)
    {
      try
      {
        if (!string.IsNullOrEmpty(assembly.Location))
        {
          references.Add(MetadataReference.CreateFromFile(assembly.Location));
        }
      }
      catch
      {
        // Skip assemblies that can't be referenced
      }
    }

    // Add ML.Next assembly reference
    references.Add(
      MetadataReference.CreateFromFile(typeof(ML.Next.Core.Schema.DataView<>).Assembly.Location)
    );

    // Add LanguageExt for ML.Next dependencies (Fin, Validation, etc.)
    references.Add(MetadataReference.CreateFromFile(typeof(LanguageExt.Fin<>).Assembly.Location));
    references.Add(
      MetadataReference.CreateFromFile(typeof(LanguageExt.Common.Error).Assembly.Location)
    );

    var compilation = CSharpCompilation.Create(
      "TestAssembly",
      new[] { syntaxTree },
      references,
      new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary)
    );

    var diagnostics = compilation.GetDiagnostics();
    var errors = diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error).ToList();

    return new CompilationResult { Success = !errors.Any(), Diagnostics = diagnostics.ToList() };
  }
}

/// <summary>
/// Result of a compilation test.
/// </summary>
public class CompilationResult
{
  /// <summary>
  /// Whether the compilation succeeded without errors.
  /// </summary>
  public required bool Success { get; init; }

  /// <summary>
  /// All diagnostics (errors, warnings, info) from the compilation.
  /// </summary>
  public required List<Diagnostic> Diagnostics { get; init; }
}
