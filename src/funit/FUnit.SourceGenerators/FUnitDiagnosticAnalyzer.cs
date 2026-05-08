using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Flowthru.FUnit.SourceGenerators;

/// <summary>
/// Validates FUnit usage patterns and emits warnings:
/// <list type="bullet">
///   <item><c>FU001</c> — a <c>[FlowthruStep]</c> class has no
///     <c>[FUnitStepTest]</c> methods anywhere in the project.</item>
///   <item><c>FU002</c> — a <c>FUnitContext</c> subclass is not
///     wrapped in <c>#if FUNIT_ENABLED</c>.</item>
/// </list>
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class FUnitDiagnosticAnalyzer : DiagnosticAnalyzer
{
  internal const string FlowthruStepAttributeFullName = "Flowthru.Step.FlowthruStepAttribute";
  internal const string StepTestAttributeFullName = "Flowthru.Step.Testing.FUnitStepTestAttribute";
  internal const string FUnitContextFullName = "Flowthru.Step.Testing.FUnitContext";
  internal const string FUnitEnabledGuard = "FUNIT_ENABLED";

  /// <summary>FU001: a <c>[FlowthruStep]</c> class has no <c>[FUnitStepTest]</c> coverage.</summary>
  public static readonly DiagnosticDescriptor Fu001 = new(
    id: "FU001",
    title: "Step has no tests",
    messageFormat:
      "'{0}' is annotated with [FlowthruStep] but has no [FUnitStepTest] methods in this project. "
        + "Pure function nodes without tests are potential failure hotspots.",
    category: "Flowthru.FUnit",
    defaultSeverity: DiagnosticSeverity.Warning,
    isEnabledByDefault: true
  );

  /// <summary>FU002: a <c>FUnitContext</c> subclass is not guarded by <c>#if FUNIT_ENABLED</c>.</summary>
  public static readonly DiagnosticDescriptor Fu002 = new(
    id: "FU002",
    title: "FUnitContext subclass not guarded by #if FUNIT_ENABLED",
    messageFormat:
      "'{0}' inherits from FUnitContext but is not inside a '#if FUNIT_ENABLED' block. "
        + "Without this guard, the class cannot be excluded from Release builds.",
    category: "Flowthru.FUnit",
    defaultSeverity: DiagnosticSeverity.Warning,
    isEnabledByDefault: true
  );

  /// <inheritdoc/>
  public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
    ImmutableArray.Create(Fu001, Fu002);

  /// <inheritdoc/>
  public override void Initialize(AnalysisContext context)
  {
    context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
    context.EnableConcurrentExecution();
    context.RegisterSymbolAction(AnalyzeType, SymbolKind.NamedType);
  }

  private static void AnalyzeType(SymbolAnalysisContext context)
  {
    var typeSymbol = (INamedTypeSymbol)context.Symbol;

    // FU001 — [FlowthruStep] without [FUnitStepTest] coverage.
    var isStep = typeSymbol
      .GetAttributes()
      .Any(a => a.AttributeClass?.ToDisplayString() == FlowthruStepAttributeFullName);
    if (isStep)
    {
      var hasTests = GetAllTypes(typeSymbol.ContainingModule.GlobalNamespace)
        .SelectMany(t => t.GetMembers().OfType<IMethodSymbol>())
        .Any(m =>
          m.GetAttributes().Any(a =>
            a.AttributeClass?.ToDisplayString() == StepTestAttributeFullName
            && a.ConstructorArguments.Length > 0
            && a.ConstructorArguments[0].Value is INamedTypeSymbol target
            && SymbolEqualityComparer.Default.Equals(target, typeSymbol)
          )
        );
      if (!hasTests)
      {
        var location = typeSymbol.Locations.FirstOrDefault() ?? Location.None;
        context.ReportDiagnostic(Diagnostic.Create(Fu001, location, typeSymbol.Name));
      }
    }

    // FU002 — FUnitContext subclass not inside #if FUNIT_ENABLED.
    var isFUnitContextSubclass = false;
    var baseType = typeSymbol.BaseType;
    while (baseType is not null)
    {
      if (baseType.ToDisplayString() == FUnitContextFullName)
      {
        isFUnitContextSubclass = true;
        break;
      }
      baseType = baseType.BaseType;
    }
    if (!isFUnitContextSubclass) return;

    var syntaxRef = typeSymbol.DeclaringSyntaxReferences.FirstOrDefault();
    if (syntaxRef?.GetSyntax() is not ClassDeclarationSyntax classDecl) return;

    if (!IsInsidePreprocessorGuard(classDecl, FUnitEnabledGuard))
    {
      context.ReportDiagnostic(
        Diagnostic.Create(Fu002, classDecl.Identifier.GetLocation(), typeSymbol.Name)
      );
    }
  }

  /// <summary>
  /// Walk a class declaration's leading trivia upward through its
  /// containing tree, looking for an enclosing
  /// <c>#if FUNIT_ENABLED</c>/<c>#endif</c> region. Heuristic — the
  /// goal is a build-time hint, not a load-bearing safety check.
  /// </summary>
  private static bool IsInsidePreprocessorGuard(ClassDeclarationSyntax classDecl, string symbol)
  {
    var node = classDecl.Parent;
    var directiveSpanStart = classDecl.SpanStart;

    var ifDirectives = classDecl.SyntaxTree.GetRoot()
      .DescendantTrivia()
      .Where(t => t.IsKind(SyntaxKind.IfDirectiveTrivia))
      .Select(t => t.GetStructure())
      .OfType<IfDirectiveTriviaSyntax>()
      .ToList();

    foreach (var directive in ifDirectives)
    {
      if (directive.SpanStart >= directiveSpanStart) continue;
      if (directive.Condition is IdentifierNameSyntax id && id.Identifier.Text == symbol)
      {
        // Find the matching #endif that occurs after the class.
        var endif = classDecl.SyntaxTree.GetRoot()
          .DescendantTrivia()
          .Where(t => t.IsKind(SyntaxKind.EndIfDirectiveTrivia))
          .Select(t => t.GetStructure())
          .OfType<EndIfDirectiveTriviaSyntax>()
          .FirstOrDefault(e => e.SpanStart > classDecl.Span.End);
        if (endif is not null) return true;
      }
    }
    return false;
  }

  private static IEnumerable<INamedTypeSymbol> GetAllTypes(INamespaceSymbol ns)
  {
    foreach (var member in ns.GetMembers())
    {
      if (member is INamedTypeSymbol type)
      {
        foreach (var t in GetAllTypes(type)) yield return t;
      }
      else if (member is INamespaceSymbol nested)
      {
        foreach (var t in GetAllTypes(nested)) yield return t;
      }
    }
  }

  private static IEnumerable<INamedTypeSymbol> GetAllTypes(INamedTypeSymbol type)
  {
    yield return type;
    foreach (var nested in type.GetTypeMembers())
    {
      foreach (var t in GetAllTypes(nested)) yield return t;
    }
  }
}
