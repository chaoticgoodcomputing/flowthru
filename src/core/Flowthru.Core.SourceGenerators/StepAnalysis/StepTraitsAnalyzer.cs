using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Flowthru.Core.SourceGenerators.StepAnalysis;

/// <summary>
/// Analyzer that emits <c>FT4003</c> when a <c>[FlowthruStep]</c> class with one or more
/// service-typed <c>Create(...)</c> parameters does not declare <c>IsIdempotent</c> or
/// <c>HasSideEffects</c> on its attribute. Suggestion-only (Hidden severity).
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class StepTraitsAnalyzer : DiagnosticAnalyzer
{
  /// <inheritdoc/>
  public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
    ImmutableArray.Create(StepDiagnostics.MissingStepTraits);

  /// <inheritdoc/>
  public override void Initialize(AnalysisContext context)
  {
    context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
    context.EnableConcurrentExecution();
    context.RegisterSymbolAction(AnalyzeStepClass, SymbolKind.NamedType);
  }

  private static void AnalyzeStepClass(SymbolAnalysisContext context)
  {
    var typeSymbol = (INamedTypeSymbol)context.Symbol;

    // Find the [FlowthruStep] attribute on this class.
    var attribute = typeSymbol
      .GetAttributes()
      .FirstOrDefault(a =>
        a.AttributeClass?.ToDisplayString()
        == StepInspectorAnalyzer.FlowthruStepAttributeFullName
      );
    if (attribute is null)
    {
      return;
    }

    // If either trait is already declared, skip.
    bool hasTraitsDeclared = attribute.NamedArguments.Any(named =>
      named.Key == "IsIdempotent" || named.Key == "HasSideEffects"
    );
    if (hasTraitsDeclared)
    {
      return;
    }

    // Only flag steps that have at least one service-candidate Create() parameter.
    bool hasServiceParam = HasServiceCandidateParameter(typeSymbol);
    if (!hasServiceParam)
    {
      return;
    }

    var location = typeSymbol.Locations.FirstOrDefault() ?? Location.None;
    context.ReportDiagnostic(
      Diagnostic.Create(StepDiagnostics.MissingStepTraits, location, typeSymbol.Name)
    );
  }

  private static bool HasServiceCandidateParameter(INamedTypeSymbol typeSymbol)
  {
    var createMethods = typeSymbol
      .GetMembers(StepInspectorAnalyzer.CreateMethodName)
      .OfType<IMethodSymbol>()
      .Where(m => m.IsStatic && m.DeclaredAccessibility == Accessibility.Public);

    foreach (var method in createMethods)
    {
      foreach (var param in method.Parameters)
      {
        if (StepInspectorAnalyzer.IsServiceCandidate(param.Type))
        {
          return true;
        }
      }
    }
    return false;
  }
}
