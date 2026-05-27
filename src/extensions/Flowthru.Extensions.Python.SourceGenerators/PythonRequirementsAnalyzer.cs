using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Flowthru.Step.Python;
using Flowthru.Step.Python.Internal;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Flowthru.Extensions.Python.SourceGenerators;

/// <summary>
/// Design-time half of the Python requirements algebra.
/// Reads <c>uv.lock</c> from the project's <c>AdditionalFiles</c>,
/// folds the framework-level base requirements with any
/// <see cref="PythonPackageRequirementAttribute"/>-decorated capability
/// in the user's compilation, and emits FTPY1501 / FTPY1502 when the
/// resolved lockfile does not satisfy the closure.
/// </summary>
/// <remarks>
/// <para>
/// Pre-flight already enforces the same algebra via
/// <c>PythonRequirementsValidationHook</c> against the materialised
/// venv (FTPY3011 / FTPY3012); this analyzer fires earlier — at build
/// time — for the cases where <c>uv.lock</c> is reachable. The two
/// reuse the same primitives
/// (<see cref="PythonRequirementsAlgebra"/>,
/// <see cref="PythonVersion"/>,
/// <see cref="PythonVersionConstraint"/>) via linked-in source files,
/// so the satisfies / unsatisfies semantics cannot diverge.
/// </para>
/// <para>
/// The analyzer is silent when:
/// <list type="bullet">
///   <item>The consumer doesn't reference Flowthru.Extensions.Python.</item>
///   <item>No <c>uv.lock</c> is present in <c>AdditionalFiles</c>.</item>
/// </list>
/// In the no-lockfile case pre-flight is still the safety net; we just
/// can't catch the gap earlier than that without the file to consult.
/// </para>
/// </remarks>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class PythonRequirementsAnalyzer : DiagnosticAnalyzer
{
  internal static readonly DiagnosticDescriptor Ftpy1501 = new(
    id: "FTPY1501",
    title: "Python package missing from uv.lock",
    messageFormat: "Python package '{0}' (constraint: {1}) declared by [{2}] is not present in uv.lock. "
      + "Run `uv add {3}` to resolve.",
    category: "Flowthru.Validation",
    defaultSeverity: DiagnosticSeverity.Error,
    isEnabledByDefault: true,
    description: "A declared Python-side package requirement is not present in the project's uv.lock. "
      + "The Python extension's framework requirements (pyarrow, the flowthru Python companion) plus "
      + "any capability decorated with [PythonPackageRequirement] are folded and checked against the "
      + "resolved lockfile. Add the missing package via the suggested `uv add` command.",
    customTags: WellKnownDiagnosticTags.CompilationEnd
  );

  internal static readonly DiagnosticDescriptor Ftpy1502 = new(
    id: "FTPY1502",
    title: "Locked Python package version fails declared constraint",
    // Two sentences with trailing period — satisfies RS1032's "single
    // sentence without period OR multi-sentence with period" rule.
    messageFormat: "Python package '{0}' is locked at version '{1}' in uv.lock. "
      + "The folded constraint '{2}' (declared by [{3}]) is not satisfied.",
    category: "Flowthru.Validation",
    defaultSeverity: DiagnosticSeverity.Error,
    isEnabledByDefault: true,
    description: "uv.lock has an entry for the package but the locked version does not satisfy the "
      + "folded constraint. The constraint string includes every contributing declarer's clauses, so "
      + "conflicting requirements (e.g. one capability needs >=15 while another needs <14) are visible "
      + "directly in the diagnostic message.",
    customTags: WellKnownDiagnosticTags.CompilationEnd
  );

  /// <inheritdoc/>
  public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
    ImmutableArray.Create(Ftpy1501, Ftpy1502);

  /// <inheritdoc/>
  public override void Initialize(AnalysisContext context)
  {
    context.EnableConcurrentExecution();
    context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
    context.RegisterCompilationAction(AnalyzeCompilation);
  }

  private static void AnalyzeCompilation(CompilationAnalysisContext context)
  {
    // Gate: the consumer must reference Flowthru.Extensions.Python.
    // We use the IPythonCapability marker as the cheapest probe — its
    // symbol exists in metadata iff the package is referenced.
    var capabilitySymbol = context.Compilation.GetTypeByMetadataName(
      "Flowthru.Step.Python.IPythonCapability"
    );
    if (capabilitySymbol is null) return;

    var attributeSymbol = context.Compilation.GetTypeByMetadataName(
      "Flowthru.Step.Python.PythonPackageRequirementAttribute"
    );
    if (attributeSymbol is null) return;

    // Locate uv.lock in AdditionalFiles. Filename match is
    // case-insensitive — Windows vs Unix path conventions; uv.lock is
    // canonical lowercase but a tolerant match avoids false negatives.
    var uvLock = context.Options.AdditionalFiles.FirstOrDefault(f =>
      f.Path.EndsWith("uv.lock", StringComparison.OrdinalIgnoreCase)
    );
    if (uvLock is null) return;

    var text = uvLock.GetText(context.CancellationToken);
    if (text is null) return;

    var installed = UvLockParser.ParsePackages(text.ToString());

    // Collect requirements from three sources:
    //   1. Source-defined types carrying [PythonPackageRequirement] —
    //      the user's own IPythonCapability implementations.
    //   2. The framework base capability — always enforced when the
    //      Python extension is referenced; looked up by metadata name
    //      because it's internal to Flowthru.Extensions.Python.
    //   3. Types REFERENCED in the consumer's source code (new T(),
    //      AddSingleton<I, T>(), etc.). This is the key to avoiding
    //      false positives — TorchrunLauncher / AccelerateLauncher /
    //      DeepspeedLauncher all carry [PythonPackageRequirement], but
    //      only the ones the consumer actually instantiates should
    //      contribute their requirements.
    var allRequirements = GetAttributeDeclaredRequirements(context.Compilation, attributeSymbol);

    var folded = PythonRequirementsAlgebra.Fold(allRequirements);

    foreach (var req in folded)
    {
      var declarerList = string.Join("; ", req.Declarers.Select(d => d.ToString()));

      if (!installed.TryGetValue(req.Package, out var installedRaw))
      {
        var constraint = req.Constraint.ToString();
        var uvAddArg = constraint == "*"
          ? req.Package
          : req.Package + constraint;
        context.ReportDiagnostic(Diagnostic.Create(
          Ftpy1501,
          Location.None,
          req.Package,
          constraint,
          declarerList,
          uvAddArg
        ));
        continue;
      }

      if (req.Constraint.Clauses.Length == 0) continue;

      if (!PythonVersion.TryParse(installedRaw, out var installedVersion)) continue;

      if (!req.Constraint.Satisfies(installedVersion))
      {
        context.ReportDiagnostic(Diagnostic.Create(
          Ftpy1502,
          Location.None,
          req.Package,
          installedRaw,
          req.Constraint.ToString(),
          declarerList
        ));
      }
    }
  }

  /// <summary>
  /// Aggregate <c>[PythonPackageRequirement]</c>-decorated capabilities
  /// the consumer actually depends on:
  /// <list type="number">
  ///   <item>Types defined in the consumer's source assembly (any
  ///         user-written <see cref="IPythonCapability"/> with the
  ///         attribute).</item>
  ///   <item>The framework's base capability
  ///         (<c>Flowthru.Step.Python.Internal.BasePythonExtensionCapability</c>) —
  ///         enforced whenever the Python extension is referenced.</item>
  ///   <item>Types referenced in the consumer's source code (via
  ///         <c>new T()</c>, generic type arguments like
  ///         <c>AddSingleton&lt;IPythonLauncher, T&gt;()</c>,
  ///         <c>typeof(T)</c>, etc.). This is what keeps the analyzer
  ///         from folding every launcher class that happens to be in
  ///         the reference chain — only launchers the consumer
  ///         actually invokes contribute their requirements.</item>
  /// </list>
  /// </summary>
  /// <remarks>
  /// Earlier slice-3 implementations walked every type in every
  /// referenced assembly's namespace tree, which over-enforced —
  /// <c>AccelerateLauncher</c> would force every project that
  /// referenced Flowthru.Extensions.Python to add <c>accelerate</c>
  /// to its uv.lock, even when the project never instantiated it. The
  /// syntax-reference walk replaces that.
  /// </remarks>
  private static IEnumerable<PythonPackageRequirement> GetAttributeDeclaredRequirements(
    Compilation compilation,
    INamedTypeSymbol attributeSymbol
  )
  {
    // (1) Source-defined types with the attribute — user-written
    //     IPythonCapability implementations land here.
    foreach (var type in EnumerateTypes(compilation.Assembly.GlobalNamespace))
    {
      foreach (var req in ExtractRequirementsFrom(type, attributeSymbol))
      {
        yield return req;
      }
    }

    // (2) Framework base capability — fold whenever it resolves, since
    //     the user referencing the extension implicitly opts into the
    //     framework's floor requirements.
    var baseCapability = compilation.GetTypeByMetadataName(
      "Flowthru.Step.Python.Internal.BasePythonExtensionCapability"
    );
    if (baseCapability is not null)
    {
      foreach (var req in ExtractRequirementsFrom(baseCapability, attributeSymbol))
      {
        yield return req;
      }
    }

    // (3) Types referenced in the consumer's source code. The
    //     visited-set keeps us from re-walking shared symbols and
    //     yielding duplicates that the algebra would then have to
    //     merge by package.
    var referenced = FindSyntaxReferencedTypes(compilation);
    foreach (var type in referenced)
    {
      foreach (var req in ExtractRequirementsFrom(type, attributeSymbol))
      {
        yield return req;
      }
    }
  }

  /// <summary>
  /// Walk every syntax tree's name/object-creation nodes, bind each
  /// to a symbol, and return the named types referenced. Symbols are
  /// deduplicated; non-type symbols (methods, namespaces, locals) are
  /// filtered out by the <see cref="INamedTypeSymbol"/> cast.
  /// </summary>
  private static IEnumerable<INamedTypeSymbol> FindSyntaxReferencedTypes(
    Compilation compilation
  )
  {
    var visited = new HashSet<INamedTypeSymbol>(SymbolEqualityComparer.Default);

    foreach (var tree in compilation.SyntaxTrees)
    {
      var model = compilation.GetSemanticModel(tree);
      var root = tree.GetRoot();

      foreach (var node in root.DescendantNodes())
      {
        INamedTypeSymbol? typeSymbol = node switch
        {
          // `new T(...)` / `new T { ... }` — GetSymbolInfo returns the
          // constructor; we want its ContainingType. GetTypeInfo
          // works too but feels less direct.
          ObjectCreationExpressionSyntax => model.GetTypeInfo(node).Type as INamedTypeSymbol,
          // Identifier / generic-name / qualified-name — covers
          // `TorchrunLauncher` in `services.AddSingleton<IPythonLauncher, TorchrunLauncher>()`,
          // `typeof(T)`, `T x;`, and most other type-reference forms.
          NameSyntax => model.GetSymbolInfo(node).Symbol as INamedTypeSymbol,
          _ => null,
        };

        if (typeSymbol is null) continue;
        if (!visited.Add(typeSymbol)) continue;

        yield return typeSymbol;
      }
    }
  }

  private static IEnumerable<PythonPackageRequirement> ExtractRequirementsFrom(
    INamedTypeSymbol type,
    INamedTypeSymbol attributeSymbol
  )
  {
    foreach (var attr in type.GetAttributes())
    {
      if (!SymbolEqualityComparer.Default.Equals(attr.AttributeClass, attributeSymbol)) continue;
      if (!TryExtractRequirement(attr, type, out var requirement)) continue;
      yield return requirement;
    }
  }

  private static IEnumerable<INamedTypeSymbol> EnumerateTypes(INamespaceSymbol root)
  {
    var stack = new Stack<INamespaceOrTypeSymbol>();
    stack.Push(root);
    while (stack.Count > 0)
    {
      var current = stack.Pop();
      foreach (var member in current.GetMembers())
      {
        switch (member)
        {
          case INamespaceSymbol ns:
            stack.Push(ns);
            break;
          case INamedTypeSymbol type:
            yield return type;
            foreach (var nested in type.GetTypeMembers())
            {
              stack.Push(nested);
            }
            break;
        }
      }
    }
  }

  private static bool TryExtractRequirement(
    AttributeData attr,
    INamedTypeSymbol owner,
    out PythonPackageRequirement requirement
  )
  {
    requirement = default!;
    var args = attr.ConstructorArguments;
    if (args.Length == 0) return false;

    var package = args[0].Value as string;
    if (string.IsNullOrWhiteSpace(package)) return false;

    string? constraint;
    string reason;

    if (args.Length == 3)
    {
      // (string package, string? versionConstraint, string reason)
      constraint = args[1].Value as string;
      reason = args[2].Value as string ?? owner.Name;
    }
    else if (args.Length == 2)
    {
      // (string package, string reason) — convenience overload
      constraint = null;
      reason = args[1].Value as string ?? owner.Name;
    }
    else
    {
      return false;
    }

    requirement = new PythonPackageRequirement(package!, constraint, reason);
    return true;
  }
}
