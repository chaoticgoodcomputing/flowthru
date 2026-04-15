using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Flowthru.DataFrames.Analyzers;

/// <summary>
/// Sentinel returned when an invocation is identified as targeting
/// <c>TypedFrameExtensions</c> or <c>GroupedFrameExtensions</c>.
/// </summary>
public sealed class TypedFrameInvocation
{
  /// <summary>The resolved method symbol.</summary>
  public IMethodSymbol Method { get; }

  /// <summary>The full invocation expression syntax node.</summary>
  public InvocationExpressionSyntax Invocation { get; }

  /// <summary>
  /// The lambda argument syntax nodes, in argument-list order. Most operations
  /// have one lambda; Join has three; terminal ops (Count, Distinct) have zero.
  /// </summary>
  public IReadOnlyList<LambdaExpressionSyntax> LambdaArguments { get; }

  internal TypedFrameInvocation(
    IMethodSymbol method,
    InvocationExpressionSyntax invocation,
    IReadOnlyList<LambdaExpressionSyntax> lambdaArguments
  )
  {
    Method = method;
    Invocation = invocation;
    LambdaArguments = lambdaArguments;
  }
}

/// <summary>
/// Identifies invocations of <c>TypedFrameExtensions</c> and <c>GroupedFrameExtensions</c>
/// methods and extracts their lambda arguments for downstream analysis.
/// </summary>
public static class TypedFrameInvocationHelper
{
  private const string TypedFrameExtensionsName = "TypedFrameExtensions";
  private const string GroupedFrameExtensionsName = "GroupedFrameExtensions";

  /// <summary>
  /// Attempts to recognise <paramref name="invocation"/> as a call to a
  /// <c>TypedFrameExtensions</c> or <c>GroupedFrameExtensions</c> method.
  /// Returns <c>null</c> if it is not such a call.
  /// </summary>
  public static TypedFrameInvocation? TryMatch(
    InvocationExpressionSyntax invocation,
    SemanticModel semanticModel
  )
  {
    if (semanticModel.GetSymbolInfo(invocation).Symbol is not IMethodSymbol methodSymbol)
      return null;

    var containingType = methodSymbol.ContainingType?.Name;
    if (containingType != TypedFrameExtensionsName && containingType != GroupedFrameExtensionsName)
      return null;

    // Extract all lambda arguments from the argument list.
    var lambdas = new List<LambdaExpressionSyntax>();
    foreach (var argument in invocation.ArgumentList.Arguments)
    {
      if (argument.Expression is LambdaExpressionSyntax lambda)
        lambdas.Add(lambda);
    }

    return new TypedFrameInvocation(methodSymbol, invocation, lambdas);
  }
}
