using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Flowthru.Core.SourceGenerators.Validation;

/// <summary>
/// FT5002 — fail-as-value discipline. Flags <c>throw</c> statements and
/// expressions inside methods that participate in Flowthru's fail-as-value
/// surface (return <c>Validated&lt;,&gt;</c>, <c>FlowIO&lt;&gt;</c>,
/// <c>EffResult&lt;&gt;</c>, or <c>ValidationResult</c>, possibly wrapped
/// in <c>Task&lt;&gt;</c>).
/// </summary>
/// <remarks>
/// <para>
/// The validation surface relies on errors being aggregated by the
/// pre-flight pipeline as typed values. A thrown exception bypasses the
/// aggregation pipeline and reaches the user as a stack trace instead of
/// an actionable FT3xxx diagnostic — exactly the regression LocalFileWriteProbe
/// had before its fail-as-value fix. This analyzer prevents future
/// regressions of the same shape.
/// </para>
/// <para>
/// Recognised return types (in <c>Flowthru.*</c>): <c>Validated&lt;,&gt;</c>,
/// <c>FlowIO&lt;&gt;</c>, <c>EffResult&lt;&gt;</c>, <c>ValidationResult</c>.
/// Wrapped in <c>System.Threading.Tasks.Task&lt;&gt;</c> is also recognised.
/// </para>
/// <para>
/// Escape hatches:
/// <list type="bullet">
///   <item>Standard Roslyn suppression: <c>#pragma warning disable FT5002</c>
///     or <c>[SuppressMessage("Flowthru.Discipline", "FT5002")]</c>.</item>
///   <item>The <c>"Unreachable: ..."</c> pattern for closed-sum exhaustiveness
///     fallthroughs is not flagged — it's the documented idiom and the
///     closed-sum analyzer (FT0001) already enforces the surrounding
///     exhaustiveness invariant.</item>
/// </list>
/// </para>
/// </remarks>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class FailAsValueThrowAnalyzer : DiagnosticAnalyzer
{
  /// <summary>FT5002 diagnostic descriptor.</summary>
  public static readonly DiagnosticDescriptor Ft5002 = new(
    id: "FT5002",
    title: "Throw in fail-as-value method",
    messageFormat:
      "Method '{0}' returns '{1}' and participates in Flowthru's fail-as-value surface — "
        + "thrown exceptions bypass pre-flight aggregation. Return a failure value "
        + "(e.g. ValidationResult.Failure, Validated.Fail, EffResult.Failure) instead.",
    category: "Flowthru.Discipline",
    defaultSeverity: DiagnosticSeverity.Warning,
    isEnabledByDefault: true,
    description:
      "Flowthru's validation pipeline aggregates errors as typed values so the user sees an "
        + "FT3xxx diagnostic, not a stack trace. Methods that return Validated, FlowIO, EffResult, "
        + "or ValidationResult must surface every failure as a value of that type."
  );

  private static readonly ImmutableHashSet<string> FailAsValueTypeNames = ImmutableHashSet.Create(
    "Validated",
    "FlowIO",
    "EffResult",
    "ValidationResult"
  );

  /// <inheritdoc/>
  public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
    ImmutableArray.Create(Ft5002);

  /// <inheritdoc/>
  public override void Initialize(AnalysisContext context)
  {
    context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
    context.EnableConcurrentExecution();
    context.RegisterSyntaxNodeAction(AnalyzeThrowStatement, SyntaxKind.ThrowStatement);
    context.RegisterSyntaxNodeAction(AnalyzeThrowExpression, SyntaxKind.ThrowExpression);
  }

  private static void AnalyzeThrowStatement(SyntaxNodeAnalysisContext context)
  {
    var throwStatement = (ThrowStatementSyntax)context.Node;

    // Bare `throw;` rethrow inside a catch block — that's a no-op for the
    // type system and doesn't introduce a new exception path. Skip it.
    if (throwStatement.Expression is null) return;

    AnalyzeThrow(context, throwStatement, throwStatement.Expression);
  }

  private static void AnalyzeThrowExpression(SyntaxNodeAnalysisContext context)
  {
    var throwExpression = (ThrowExpressionSyntax)context.Node;
    AnalyzeThrow(context, throwExpression, throwExpression.Expression);
  }

  private static void AnalyzeThrow(
    SyntaxNodeAnalysisContext context,
    SyntaxNode throwNode,
    ExpressionSyntax thrown
  )
  {
    // The discipline applies to throws in the method body directly. Throws
    // inside lambdas are typically inside lifting boundaries like
    // `FlowIO.Lift(() => { throw ... })` — `Lift` catches the throw and
    // converts it to a typed failure, so the throw never escapes the
    // fail-as-value carrier. Skipping lambda-internal throws avoids the
    // common false-positive without losing protection at the API boundary.
    if (IsInsideLambdaBoundary(throwNode)) return;

    // Resolve the enclosing method-shaped declaration (method, local
    // function). We only flag methods whose explicit return type signals
    // participation in the fail-as-value surface.
    var enclosingMethod = throwNode.FirstAncestorOrSelf<MethodDeclarationSyntax>();
    var enclosingLocalFunction = throwNode.FirstAncestorOrSelf<LocalFunctionStatementSyntax>();

    TypeSyntax? returnType = enclosingMethod?.ReturnType ?? enclosingLocalFunction?.ReturnType;
    string? methodName = enclosingMethod?.Identifier.ValueText ?? enclosingLocalFunction?.Identifier.ValueText;
    if (returnType is null || methodName is null) return;

    if (!ReturnTypeNamesFailAsValue(context, returnType, out var failAsValueTypeName)) return;

    if (IsUnreachableClosedSumFallthrough(thrown)) return;

    if (IsArgumentPreconditionGuard(thrown)) return;

    var diag = Diagnostic.Create(
      Ft5002,
      throwNode.GetLocation(),
      methodName,
      failAsValueTypeName
    );
    context.ReportDiagnostic(diag);
  }

  /// <summary>
  /// True iff the throw is nested inside a lambda or anonymous-method
  /// expression — those are typically "lifting boundaries" like
  /// <c>FlowIO.Lift(() =&gt; { throw ... })</c> that catch the throw and
  /// translate it to a typed failure.
  /// </summary>
  private static bool IsInsideLambdaBoundary(SyntaxNode throwNode)
  {
    for (var node = throwNode.Parent; node is not null; node = node.Parent)
    {
      switch (node)
      {
        case ParenthesizedLambdaExpressionSyntax:
        case SimpleLambdaExpressionSyntax:
        case AnonymousMethodExpressionSyntax:
          return true;
        case MethodDeclarationSyntax:
        case LocalFunctionStatementSyntax:
          return false;
      }
    }
    return false;
  }

  /// <summary>
  /// True iff the (possibly Task-wrapped) return type names a Flowthru
  /// fail-as-value carrier.
  /// </summary>
  private static bool ReturnTypeNamesFailAsValue(
    SyntaxNodeAnalysisContext context,
    TypeSyntax returnType,
    out string matchedTypeName
  )
  {
    matchedTypeName = string.Empty;
    var symbol = context.SemanticModel.GetSymbolInfo(returnType).Symbol as INamedTypeSymbol;
    if (symbol is null) return false;

    // Unwrap Task<T> / ValueTask<T>.
    if (symbol.OriginalDefinition.ToDisplayString() is "System.Threading.Tasks.Task<TResult>"
        or "System.Threading.Tasks.ValueTask<TResult>")
    {
      if (symbol.TypeArguments.Length != 1) return false;
      symbol = symbol.TypeArguments[0] as INamedTypeSymbol;
      if (symbol is null) return false;
    }

    var unboundName = symbol.OriginalDefinition.Name;
    if (!FailAsValueTypeNames.Contains(unboundName)) return false;

    // Namespace guard — match only Flowthru.* carriers (avoid colliding
    // with similarly named types from other libraries).
    var ns = symbol.ContainingNamespace?.ToDisplayString() ?? string.Empty;
    if (!ns.StartsWith("Flowthru", System.StringComparison.Ordinal)) return false;

    matchedTypeName = unboundName;
    return true;
  }

  /// <summary>
  /// Recognises the .NET-idiomatic argument-precondition guard:
  /// <c>throw new ArgumentNullException(...)</c>,
  /// <c>throw new ArgumentOutOfRangeException(...)</c>, or
  /// <c>throw new ArgumentException(...)</c>. These signal programming
  /// errors (caller violated the contract), not operational failures the
  /// pipeline should aggregate. The discipline targets exceptions thrown
  /// in response to runtime conditions, not API misuse.
  /// </summary>
  private static bool IsArgumentPreconditionGuard(ExpressionSyntax thrown)
  {
    if (thrown is not ObjectCreationExpressionSyntax oce) return false;
    var typeText = oce.Type.ToString();
    // Match unqualified or fully-qualified names ending in one of the
    // argument-validation exception names.
    return typeText.EndsWith("ArgumentNullException", System.StringComparison.Ordinal)
        || typeText.EndsWith("ArgumentOutOfRangeException", System.StringComparison.Ordinal)
        || typeText.EndsWith("ArgumentException", System.StringComparison.Ordinal);
  }

  /// <summary>
  /// Recognises the documented closed-sum fallthrough idiom:
  /// <c>throw new InvalidOperationException("Unreachable: …")</c>. These
  /// are paired with closed-sum switches whose exhaustiveness is enforced
  /// by FT0001; flagging them here would be noise.
  /// </summary>
  private static bool IsUnreachableClosedSumFallthrough(ExpressionSyntax thrown)
  {
    if (thrown is not ObjectCreationExpressionSyntax oce) return false;
    if (oce.Type is not IdentifierNameSyntax idType
        || idType.Identifier.ValueText != "InvalidOperationException")
    {
      // Could also be a fully-qualified name. Cheap match: ends with InvalidOperationException.
      var typeText = oce.Type.ToString();
      if (!typeText.EndsWith("InvalidOperationException", System.StringComparison.Ordinal))
        return false;
    }

    var firstArg = oce.ArgumentList?.Arguments.FirstOrDefault();
    if (firstArg?.Expression is not LiteralExpressionSyntax literal) return false;
    if (!literal.IsKind(SyntaxKind.StringLiteralExpression)) return false;

    var text = literal.Token.ValueText;
    return text.StartsWith("Unreachable", System.StringComparison.Ordinal);
  }
}
