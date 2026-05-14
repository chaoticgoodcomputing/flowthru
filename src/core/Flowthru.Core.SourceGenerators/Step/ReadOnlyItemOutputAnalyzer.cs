using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Flowthru.Core.SourceGenerators.Step;

/// <summary>
/// Analyzer that emits <c>FT1102</c> when a <c>FlowBuilder.AddStep</c>
/// invocation supplies an <c>outputs:</c> argument whose static type
/// implements <c>IReadOnlyItem&lt;T&gt;</c>. Per the Phase 5 RFC, the
/// canonical implementer is
/// <c>Flowthru.Data.Catalog.Configuration.ConfigurationItem&lt;T&gt;</c> —
/// configuration items must be inputs only.
/// </summary>
/// <remarks>
/// <para>
/// The analyzer walks tuple literals so multi-output steps that pass
/// outputs as <c>(o1, badConfig)</c> still surface the violation. It
/// uses a cheap syntactic gate on the method name <c>AddStep</c>
/// before consulting the semantic model, matching the pattern used
/// by <see cref="FlowthruStepAttributeAnalyzer"/>.
/// </para>
/// </remarks>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class ReadOnlyItemOutputAnalyzer : DiagnosticAnalyzer
{
  internal const string FlowBuilderFullName = "Flowthru.Flow.FlowBuilder";
  internal const string ReadOnlyItemInterfaceFullName = "Flowthru.Data.Catalog.IReadOnlyItem<T>";
  internal const string AddStepMethodName = "AddStep";
  internal const string OutputsParameterName = "outputs";

  /// <inheritdoc/>
  public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
    ImmutableArray.Create(StepDiagnostics.ReadOnlyItemInOutputPosition);

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

    var outputsArg = FindOutputsArgument(invocation, methodSymbol);
    if (outputsArg is null)
    {
      // Some AddStep overloads (sink steps) take no outputs parameter — no violation possible.
      return;
    }

    foreach (var (expression, location) in EnumerateLeafExpressions(outputsArg.Expression))
    {
      var typeInfo = context.SemanticModel.GetTypeInfo(expression, context.CancellationToken);
      var resolvedType = typeInfo.Type ?? typeInfo.ConvertedType;
      if (resolvedType is null)
      {
        continue;
      }

      if (ImplementsReadOnlyItem(resolvedType))
      {
        context.ReportDiagnostic(
          Diagnostic.Create(
            StepDiagnostics.ReadOnlyItemInOutputPosition,
            location,
            DisplayName(expression, resolvedType)
          )
        );
      }
    }
  }

  // ── Helpers ─────────────────────────────────────────────────────────────

  private static bool IsAddStepInvocation(InvocationExpressionSyntax invocation) =>
    invocation.Expression switch
    {
      MemberAccessExpressionSyntax ma => ma.Name.Identifier.Text == AddStepMethodName,
      IdentifierNameSyntax id => id.Identifier.Text == AddStepMethodName,
      _ => false,
    };

  private static ArgumentSyntax? FindOutputsArgument(
    InvocationExpressionSyntax invocation,
    IMethodSymbol method
  )
  {
    var args = invocation.ArgumentList.Arguments;

    // Prefer named-argument lookup — AddStep is overload-heavy.
    var named = args.FirstOrDefault(a =>
      a.NameColon?.Name.Identifier.Text == OutputsParameterName);
    if (named is not null)
    {
      return named;
    }

    // Fall back to positional matching against the resolved method's parameter list.
    var outputsIndex = -1;
    for (int i = 0; i < method.Parameters.Length; i++)
    {
      if (method.Parameters[i].Name == OutputsParameterName)
      {
        outputsIndex = i;
        break;
      }
    }
    if (outputsIndex < 0 || outputsIndex >= args.Count)
    {
      return null;
    }

    // Don't mis-attribute if an earlier positional has a name-colon (index shifts).
    for (int i = 0; i < outputsIndex; i++)
    {
      if (args[i].NameColon is not null)
      {
        return null;
      }
    }

    return args[outputsIndex];
  }

  /// <summary>
  /// Yield each leaf-position expression contained in <paramref name="expression"/>
  /// along with the diagnostic location to report. For a plain identifier
  /// or member access this is just the expression itself; for a tuple
  /// literal <c>(a, b, c)</c> we yield each element so a buried
  /// read-only item produces a precise squiggle.
  /// </summary>
  private static IEnumerable<(ExpressionSyntax Expression, Location Location)> EnumerateLeafExpressions(
    ExpressionSyntax expression
  )
  {
    if (expression is TupleExpressionSyntax tuple)
    {
      foreach (var element in tuple.Arguments)
      {
        foreach (var leaf in EnumerateLeafExpressions(element.Expression))
        {
          yield return leaf;
        }
      }
      yield break;
    }

    yield return (expression, expression.GetLocation());
  }

  /// <summary>
  /// True when <paramref name="type"/> implements
  /// <c>Flowthru.Data.Catalog.IReadOnlyItem&lt;T&gt;</c> for any
  /// <c>T</c>. Walks the full interface set (constructed-generic
  /// equality on the unbound definition).
  /// </summary>
  private static bool ImplementsReadOnlyItem(ITypeSymbol type)
  {
    foreach (var iface in type.AllInterfaces)
    {
      if (!iface.IsGenericType) continue;
      var unbound = iface.OriginalDefinition.ToDisplayString();
      // The default ToDisplayString uses the unbound form
      // "Flowthru.Data.Catalog.IReadOnlyItem<T>", matching our constant.
      if (unbound == ReadOnlyItemInterfaceFullName)
      {
        return true;
      }
    }

    // Also check the type itself if it is the interface directly
    // (analyzer test scenarios pass an IReadOnlyItem<T>-typed parameter).
    if (type is INamedTypeSymbol named && named.IsGenericType
      && named.OriginalDefinition.ToDisplayString() == ReadOnlyItemInterfaceFullName)
    {
      return true;
    }

    return false;
  }

  private static string DisplayName(ExpressionSyntax expression, ITypeSymbol resolvedType) =>
    expression switch
    {
      IdentifierNameSyntax id => id.Identifier.Text,
      MemberAccessExpressionSyntax ma => ma.Name.Identifier.Text,
      _ => resolvedType.Name,
    };
}
