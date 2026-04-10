using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Flowthru.FUnit.SourceGenerators;

/// <summary>
/// Analyzer that validates FUnit usage patterns.
/// Emits:
/// <list type="bullet">
/// <item><c>FU001</c> — a <c>[FlowthruStep]</c> class has no <c>[StepTest]</c> methods.</item>
/// <item><c>FU002</c> — a <c>FunitContext</c> subclass is not wrapped in <c>#if FUNIT_ENABLED</c>.</item>
/// </list>
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class FunitDiagnosticAnalyzer : DiagnosticAnalyzer
{
  internal const string FlowthruStepAttributeFullName = "Flowthru.Core.Steps.FlowthruStepAttribute";
  internal const string StepTestAttributeFullName = "Flowthru.FUnit.StepTestAttribute";
  internal const string FunitContextFullName = "Flowthru.FUnit.FunitContext";
  internal const string FunitEnabledGuard = "FUNIT_ENABLED";

  public static readonly DiagnosticDescriptor Fu001 = new DiagnosticDescriptor(
    id: "FU001",
    title: "Step has no tests",
    messageFormat: "'{0}' is annotated with [FlowthruStep] but has no [StepTest] methods in this project. "
      + "Pure function nodes without tests are potential failure hotspots.",
    category: "Flowthru.FUnit",
    defaultSeverity: DiagnosticSeverity.Warning,
    isEnabledByDefault: true
  );

  public static readonly DiagnosticDescriptor Fu002 = new DiagnosticDescriptor(
    id: "FU002",
    title: "FunitContext subclass not guarded by #if FUNIT_ENABLED",
    messageFormat: "'{0}' inherits from FunitContext but is not inside a '#if FUNIT_ENABLED' block. "
      + "Without this guard, the class cannot be excluded from Release builds.",
    category: "Flowthru.FUnit",
    defaultSeverity: DiagnosticSeverity.Warning,
    isEnabledByDefault: true
  );

  public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
    ImmutableArray.Create(Fu001, Fu002);

  public override void Initialize(AnalysisContext context)
  {
    context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
    context.EnableConcurrentExecution();
    context.RegisterSymbolAction(AnalyzeType, SymbolKind.NamedType);
  }

  private static void AnalyzeType(SymbolAnalysisContext context)
  {
    var typeSymbol = (INamedTypeSymbol)context.Symbol;

    // FU001 — [FlowthruStep] with no [StepTest] coverage
    bool isStep = typeSymbol
      .GetAttributes()
      .Any(a => a.AttributeClass?.ToDisplayString() == FlowthruStepAttributeFullName);

    if (isStep)
    {
      // Walk all methods in the compilation that carry [StepTest(typeof(this type))]
      bool hasTests = GetAllTypes(typeSymbol.ContainingModule.GlobalNamespace)
        .SelectMany(t => t.GetMembers().OfType<IMethodSymbol>())
        .Any(m =>
          m.GetAttributes()
            .Any(a =>
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

    // FU002 — FunitContext subclass not inside #if FUNIT_ENABLED
    bool isFunitContextSubclass = false;
    var baseType = typeSymbol.BaseType;
    while (baseType is not null)
    {
      if (baseType.ToDisplayString() == FunitContextFullName)
      {
        isFunitContextSubclass = true;
        break;
      }
      baseType = baseType.BaseType;
    }

    if (!isFunitContextSubclass)
      return;

    var syntaxRef = typeSymbol.DeclaringSyntaxReferences.FirstOrDefault();
    if (syntaxRef is null)
      return;

    if (syntaxRef.GetSyntax() is not ClassDeclarationSyntax classDecl)
      return;

    if (!FunitSyntaxHelpers.IsInsidePreprocessorGuard(classDecl, FunitEnabledGuard))
    {
      context.ReportDiagnostic(
        Diagnostic.Create(Fu002, classDecl.Identifier.GetLocation(), typeSymbol.Name)
      );
    }
  }

  // Walk all types in a namespace recursively, including nested types.
  private static System.Collections.Generic.IEnumerable<INamedTypeSymbol> GetAllTypes(
    INamespaceSymbol ns
  )
  {
    foreach (var member in ns.GetMembers())
    {
      if (member is INamedTypeSymbol type)
        foreach (var t in GetAllTypes(type))
          yield return t;
      else if (member is INamespaceSymbol nested)
        foreach (var t in GetAllTypes(nested))
          yield return t;
    }
  }

  private static System.Collections.Generic.IEnumerable<INamedTypeSymbol> GetAllTypes(
    INamedTypeSymbol type
  )
  {
    yield return type;
    foreach (var nested in type.GetTypeMembers())
    foreach (var t in GetAllTypes(nested))
      yield return t;
  }
}
