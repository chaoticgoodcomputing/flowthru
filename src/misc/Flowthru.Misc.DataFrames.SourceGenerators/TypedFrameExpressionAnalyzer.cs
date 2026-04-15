using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Flowthru.DataFrames.Analyzers;

/// <summary>
/// Validates that lambda expressions passed to <c>TypedFrameExtensions</c> methods have
/// structurally translatable bodies — constraints that apply regardless of the backing
/// DataFrame provider.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class TypedFrameExpressionAnalyzer : DiagnosticAnalyzer
{
  public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
    ImmutableArray.Create(DataFrameDiagnostics.InvalidProjectionBody);

  public override void Initialize(AnalysisContext context)
  {
    context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
    context.EnableConcurrentExecution();
    context.RegisterSyntaxNodeAction(AnalyzeInvocation, SyntaxKind.InvocationExpression);
  }

  private static void AnalyzeInvocation(SyntaxNodeAnalysisContext context)
  {
    var invocation = (InvocationExpressionSyntax)context.Node;
    var match = TypedFrameInvocationHelper.TryMatch(invocation, context.SemanticModel);

    if (match is null)
      return;

    // FDFRAME1001: only fires on Select — the one operation that projects into a new schema.
    if (match.Method.Name != "Select")
      return;

    // Select has one lambda argument: the selector.
    if (match.LambdaArguments.Count == 0)
      return;

    var selector = match.LambdaArguments[0];
    var body = GetLambdaBody(selector);

    if (body is null || IsValidProjectionBody(body))
      return;

    context.ReportDiagnostic(
      Diagnostic.Create(
        DataFrameDiagnostics.InvalidProjectionBody,
        body.GetLocation(),
        body.ToString()
      )
    );
  }

  /// <summary>
  /// Returns the expression body of a simple-lambda or parenthesized-lambda, or <c>null</c>
  /// if the lambda uses a block body (validation not applicable for block-bodied lambdas).
  /// </summary>
  private static ExpressionSyntax? GetLambdaBody(LambdaExpressionSyntax lambda) =>
    lambda switch
    {
      SimpleLambdaExpressionSyntax s => s.Body as ExpressionSyntax,
      ParenthesizedLambdaExpressionSyntax p => p.Body as ExpressionSyntax,
      _ => null,
    };

  /// <summary>
  /// Returns <c>true</c> if the projection body is one of the expression forms that every
  /// DataFrame provider can decompose into named column operations:
  /// <list type="bullet">
  /// <item><c>new OutputSchema { Prop = expr }</c> — object initializer</item>
  /// <item><c>new OutputSchema(expr, expr)</c> — positional constructor (records, anonymous types)</item>
  /// <item><c>new { Prop = expr }</c> — anonymous object creation</item>
  /// <item><c>x.Property</c> — single member access (identity/passthrough projection)</item>
  /// </list>
  /// </summary>
  private static bool IsValidProjectionBody(ExpressionSyntax body) =>
    body switch
    {
      // new OutputSchema { Prop = ... }
      ObjectCreationExpressionSyntax { Initializer: not null } => true,

      // new OutputSchema(...) — positional; only valid if it's a record/anonymous type
      // where the compiler supplies member metadata. We allow it here at the syntax level;
      // providers enforce member-metadata availability at runtime if needed.
      ObjectCreationExpressionSyntax { ArgumentList.Arguments.Count: > 0 } => true,

      // new { Prop = ... } — anonymous type
      AnonymousObjectCreationExpressionSyntax => true,

      // new OutputSchema() { Prop = ... } — implicit new with initializer (C# 9+)
      ImplicitObjectCreationExpressionSyntax { Initializer: not null } => true,

      // new OutputSchema(...) — implicit new positional
      ImplicitObjectCreationExpressionSyntax { ArgumentList.Arguments.Count: > 0 } => true,

      // x.Property — single member access passthrough
      MemberAccessExpressionSyntax => true,

      // x.Property — simple identifier (parameter passthrough)
      IdentifierNameSyntax => true,

      _ => false,
    };
}
