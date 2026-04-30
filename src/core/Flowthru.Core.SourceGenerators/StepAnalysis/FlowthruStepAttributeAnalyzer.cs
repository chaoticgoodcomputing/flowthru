using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Flowthru.Core.SourceGenerators.StepAnalysis;

/// <summary>
/// Analyzer that emits <c>FT4001</c> when a <c>FlowBuilder.AddStep</c> invocation's
/// <c>transform:</c> argument references a step factory class lacking
/// <c>[FlowthruStep]</c>. Inline lambdas and anonymous methods are exempted.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class FlowthruStepAttributeAnalyzer : DiagnosticAnalyzer
{
  internal const string FlowBuilderFullName = "Flowthru.Core.Flows.FlowBuilder";
  internal const string FlowthruStepAttributeFullName = "Flowthru.Core.Steps.FlowthruStepAttribute";
  internal const string AddStepMethodName = "AddStep";
  internal const string TransformParameterName = "transform";

  /// <inheritdoc/>
  public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
    ImmutableArray.Create(StepDiagnostics.MissingFlowthruStepAttribute);

  /// <inheritdoc/>
  public override void Initialize(AnalysisContext context)
  {
    context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
    context.EnableConcurrentExecution();
    context.RegisterSyntaxNodeAction(AnalyzeInvocation, SyntaxKind.InvocationExpression);
  }

  private static void AnalyzeInvocation(SyntaxNodeAnalysisContext context)
  {
    var invocation = (InvocationExpressionSyntax)context.Node;

    // Cheap syntactic gate: only consider invocations whose method name is `AddStep`.
    if (!IsAddStepInvocation(invocation))
    {
      return;
    }

    // Confirm semantically that this is FlowBuilder.AddStep, not some other AddStep.
    var symbolInfo = context.SemanticModel.GetSymbolInfo(invocation, context.CancellationToken);
    var methodSymbol = symbolInfo.Symbol as IMethodSymbol
      ?? symbolInfo.CandidateSymbols.OfType<IMethodSymbol>().FirstOrDefault();
    if (methodSymbol is null)
    {
      return;
    }

    if (methodSymbol.ContainingType?.ToDisplayString() != FlowBuilderFullName
      || methodSymbol.Name != AddStepMethodName)
    {
      return;
    }

    var transformArg = FindTransformArgument(invocation, methodSymbol);
    if (transformArg is null)
    {
      return;
    }

    // Inline lambdas / anonymous methods are exempted — users extract to step classes
    // when the transform is non-trivial, and we don't second-guess that.
    if (transformArg.Expression is LambdaExpressionSyntax
      or AnonymousMethodExpressionSyntax)
    {
      return;
    }

    // Resolve the receiver type (the "step factory class") via the syntax shape.
    // Accepts both `SomeStep.Create()` (invocation) and `SomeStep.Create` (method group).
    var receiverType = ResolveReceiverType(transformArg.Expression, context.SemanticModel,
      context.CancellationToken);
    if (receiverType is null)
    {
      return;
    }

    // Skip if the receiver type is in source we can't modify (referenced assemblies).
    // Diagnostic with "add this attribute" is meaningless if the user can't author the class.
    if (receiverType.Locations.All(loc => !loc.IsInSource))
    {
      return;
    }

    // Skip when the type already carries [FlowthruStep] — happy path.
    bool hasAttribute = receiverType
      .GetAttributes()
      .Any(a => a.AttributeClass?.ToDisplayString() == FlowthruStepAttributeFullName);
    if (hasAttribute)
    {
      return;
    }

    context.ReportDiagnostic(
      Diagnostic.Create(
        StepDiagnostics.MissingFlowthruStepAttribute,
        transformArg.Expression.GetLocation(),
        receiverType.Name
      )
    );
  }

  // ── Helpers ─────────────────────────────────────────────────────────────

  private static bool IsAddStepInvocation(InvocationExpressionSyntax invocation) =>
    invocation.Expression switch
    {
      MemberAccessExpressionSyntax ma => ma.Name.Identifier.Text == AddStepMethodName,
      IdentifierNameSyntax id => id.Identifier.Text == AddStepMethodName,
      _ => false,
    };

  private static ArgumentSyntax? FindTransformArgument(
    InvocationExpressionSyntax invocation,
    IMethodSymbol method
  )
  {
    var args = invocation.ArgumentList.Arguments;

    // Prefer named-argument lookup — AddStep is overload-heavy and consumers typically
    // pass `transform: …` by name.
    var named = args.FirstOrDefault(a =>
      a.NameColon?.Name.Identifier.Text == TransformParameterName);
    if (named is not null)
    {
      return named;
    }

    // Fall back to positional matching against the resolved method's parameter list.
    var transformIndex = -1;
    for (int i = 0; i < method.Parameters.Length; i++)
    {
      if (method.Parameters[i].Name == TransformParameterName)
      {
        transformIndex = i;
        break;
      }
    }
    if (transformIndex < 0 || transformIndex >= args.Count)
    {
      return null;
    }

    // Don't mis-attribute if any earlier positional has a name-colon — the index would shift.
    for (int i = 0; i < transformIndex; i++)
    {
      if (args[i].NameColon is not null)
      {
        return null;
      }
    }

    return args[transformIndex];
  }

  private static INamedTypeSymbol? ResolveReceiverType(
    ExpressionSyntax expression,
    SemanticModel semanticModel,
    System.Threading.CancellationToken cancellationToken
  )
  {
    // Shape: `SomeStep.Create(...)` — peel off the invocation, fall through to the inner expression.
    if (expression is InvocationExpressionSyntax inner)
    {
      expression = inner.Expression;
    }

    // Shape now: `SomeStep.Create` (member access) or `Create` (bare identifier — same class).
    return expression switch
    {
      MemberAccessExpressionSyntax ma =>
        semanticModel.GetTypeInfo(ma.Expression, cancellationToken).Type as INamedTypeSymbol
          ?? semanticModel.GetSymbolInfo(ma.Expression, cancellationToken).Symbol as INamedTypeSymbol,
      IdentifierNameSyntax id =>
        // Bare identifier: method on the enclosing type. Walk up to find that type.
        semanticModel.GetSymbolInfo(id, cancellationToken).Symbol is IMethodSymbol method
          ? method.ContainingType
          : null,
      _ => null,
    };
  }
}
