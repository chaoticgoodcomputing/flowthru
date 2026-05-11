using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Flowthru.Core.SourceGenerators.Algebra;

/// <summary>
/// FT0001 — closed-sum exhaustiveness. Flags <c>switch</c>
/// expressions over a Flowthru closed-sum type
/// (<c>PreFlightError</c>, <c>RuntimeError</c>,
/// <c>ServiceRef</c>, <c>Validated&lt;TError, TValue&gt;</c>,
/// <c>EffResult&lt;A&gt;</c>, <c>StepResult</c>,
/// <c>DependencyAnalyzer.Result</c>) that don't include every
/// nested-record case as an arm. The closed-sum invariant — "every
/// case is matched, additions to the sum surface as build failures
/// at every consumer" — is the load-bearing property of the FP shape
/// per §2.5.
/// </summary>
/// <remarks>
/// <para>
/// Heuristic: the analyzer recognises a closed sum as a non-record
/// abstract record (declared via <c>public abstract record X { …; }</c>)
/// with at least two nested <c>sealed record</c> derivations. This
/// matches every Flowthru closed-sum declaration shape — see
/// <c>PreFlightError</c>'s file for the canonical example.
/// </para>
/// <para>
/// Trigger: any <c>switch</c> expression whose governing-expression
/// type is the closed-sum base. The analyzer walks the arms and
/// reports if any nested <c>sealed record</c> case isn't covered by a
/// type-pattern arm.
/// </para>
/// </remarks>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class ClosedSumExhaustivenessAnalyzer : DiagnosticAnalyzer
{
  /// <summary>
  /// FT0001 diagnostic descriptor. Surfaced to consumers via the
  /// <c>Flowthru.Diagnostics.FlowthruDiagnosticCodes.ExhaustiveMatchRequired</c>
  /// constant.
  /// </summary>
  public static readonly DiagnosticDescriptor Ft0001 = new(
    id: "FT0001",
    title: "Closed-sum match must be exhaustive",
    messageFormat:
      "Switch expression over closed sum '{0}' is missing case '{1}'. "
        + "Closed sums in Flowthru require exhaustive matching — adding a new variant must "
        + "fail the build at every consumer until handled.",
    category: "Flowthru.Algebra",
    defaultSeverity: DiagnosticSeverity.Warning,
    isEnabledByDefault: true,
    description:
      "Flowthru's closed sums (PreFlightError, RuntimeError, ServiceRef, etc.) define every "
        + "valid case at the algebra boundary. Non-exhaustive matches defeat the design's "
        + "intent: that adding a new variant surfaces as a compile diagnostic everywhere it "
        + "must be handled."
  );

  /// <inheritdoc/>
  public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
    ImmutableArray.Create(Ft0001);

  /// <inheritdoc/>
  public override void Initialize(AnalysisContext context)
  {
    context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
    context.EnableConcurrentExecution();
    context.RegisterSyntaxNodeAction(AnalyzeSwitchExpression, SyntaxKind.SwitchExpression);
  }

  private static void AnalyzeSwitchExpression(SyntaxNodeAnalysisContext context)
  {
    var switchExpr = (SwitchExpressionSyntax)context.Node;
    var governingTypeInfo = context.SemanticModel.GetTypeInfo(
      switchExpr.GoverningExpression,
      context.CancellationToken
    );
    if (governingTypeInfo.Type is not INamedTypeSymbol governingType) return;

    // For nullable refs the symbol's type is the underlying.
    var sumType = UnwrapClosedSum(governingType);
    if (sumType is null) return;

    var nestedSealedRecords = sumType
      .GetTypeMembers()
      .Where(t => t.IsRecord && t.IsSealed && InheritsFrom(t, sumType))
      .ToList();
    if (nestedSealedRecords.Count < 2) return;

    var coveredTypes = new HashSet<INamedTypeSymbol>(SymbolEqualityComparer.Default);
    var hasDiscardOrDefault = false;
    foreach (var arm in switchExpr.Arms)
    {
      if (arm.Pattern is DiscardPatternSyntax)
      {
        hasDiscardOrDefault = true;
        continue;
      }
      var armType = ExtractArmType(arm.Pattern, context.SemanticModel, context.CancellationToken);
      if (armType is not null) coveredTypes.Add(armType);
    }

    if (hasDiscardOrDefault) return;

    foreach (var caseType in nestedSealedRecords)
    {
      if (!coveredTypes.Contains(caseType))
      {
        context.ReportDiagnostic(
          Diagnostic.Create(
            Ft0001,
            switchExpr.SwitchKeyword.GetLocation(),
            sumType.Name,
            caseType.Name
          )
        );
      }
    }
  }

  /// <summary>
  /// Recognise the closed-sum shape: an abstract non-sealed record
  /// (the umbrella) with at least two nested sealed-record cases.
  /// Generic instances (<c>Validated&lt;E, V&gt;</c>) are unwrapped to
  /// their <see cref="INamedTypeSymbol.OriginalDefinition"/> so the
  /// nested-cases lookup hits the type-parameter-bearing nested
  /// records the same way pattern matching does.
  /// </summary>
  private static INamedTypeSymbol? UnwrapClosedSum(INamedTypeSymbol type)
  {
    var original = type.OriginalDefinition;
    if (!original.IsRecord) return null;
    if (!original.IsAbstract) return null;
    if (original.IsSealed) return null;

    var nestedSealedRecords = original
      .GetTypeMembers()
      .Where(t => t.IsRecord && t.IsSealed && InheritsFrom(t, original))
      .ToList();
    if (nestedSealedRecords.Count < 2) return null;
    return original;
  }

  private static bool InheritsFrom(INamedTypeSymbol type, INamedTypeSymbol candidateBase)
  {
    var current = type.BaseType;
    while (current is not null)
    {
      if (SymbolEqualityComparer.Default.Equals(current.OriginalDefinition, candidateBase))
        return true;
      current = current.BaseType;
    }
    return false;
  }

  /// <summary>
  /// Pull the nested-record type out of a switch arm's pattern.
  /// Recognises <c>TypeName</c>, <c>TypeName var name</c>,
  /// <c>TypeName { … }</c>, and <c>TypeName(…)</c> patterns. Returns
  /// <c>null</c> for patterns the analyzer doesn't understand —
  /// those silently disable the check rather than flag false
  /// positives.
  /// </summary>
  private static INamedTypeSymbol? ExtractArmType(
    PatternSyntax pattern,
    SemanticModel model,
    System.Threading.CancellationToken ct
  )
  {
    TypeSyntax? typeSyntax = pattern switch
    {
      DeclarationPatternSyntax decl => decl.Type,
      RecursivePatternSyntax rec => rec.Type,
      TypePatternSyntax tp => tp.Type,
      _ => null,
    };
    if (typeSyntax is not null)
    {
      var typeSymbol = model.GetSymbolInfo(typeSyntax, ct).Symbol;
      if (typeSymbol is INamedTypeSymbol named) return named.OriginalDefinition;
    }

    // Patterns like `Outcome.Draw =>` (no variable binding, no record braces)
    // parse as ConstantPatternSyntax — the expression is a member access whose
    // symbol resolves to a type when the case has no constructor parameters.
    // Treat that as a type-matching arm so empty-variant cases register.
    if (pattern is ConstantPatternSyntax constant)
    {
      var exprSymbol = model.GetSymbolInfo(constant.Expression, ct).Symbol;
      if (exprSymbol is INamedTypeSymbol exprNamed) return exprNamed.OriginalDefinition;
      if (constant.Expression is TypeOfExpressionSyntax t)
      {
        var ofType = model.GetSymbolInfo(t.Type, ct).Symbol;
        if (ofType is INamedTypeSymbol ofNamed) return ofNamed.OriginalDefinition;
      }
    }
    return null;
  }
}
