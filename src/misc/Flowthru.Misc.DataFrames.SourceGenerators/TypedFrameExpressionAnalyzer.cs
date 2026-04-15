using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Flowthru.Misc.DataFrames.Analyzers;

/// <summary>
/// Validates that lambda expressions passed to <c>TypedFrameExtensions</c> and
/// <c>GroupedFrameExtensions</c> methods have structurally translatable bodies —
/// constraints that apply regardless of the backing DataFrame provider.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class TypedFrameExpressionAnalyzer : DiagnosticAnalyzer
{
  public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
    ImmutableArray.Create(
      DataFrameDiagnostics.InvalidProjectionBody,
      DataFrameDiagnostics.NonAssignmentBinding,
      DataFrameDiagnostics.PositionalConstructorNonRecord,
      DataFrameDiagnostics.InvalidAggregateResultBody,
      DataFrameDiagnostics.InvalidAggregateBinding
    );

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
    {
      return;
    }

    switch (match.Method.Name)
    {
      case "Select":
        CheckSelectLambda(context, match);
        break;
      case "Aggregate":
        CheckAggregateLambda(context, match);
        break;
    }
  }

  // ─── Select checks (FDFRAMES1001, FDFRAMES1002, FDFRAMES1003) ──────────────────

  private static void CheckSelectLambda(
    SyntaxNodeAnalysisContext context,
    TypedFrameInvocation match
  )
  {
    if (match.LambdaArguments.Count == 0)
    {
      return;
    }

    var selector = match.LambdaArguments[0];
    var body = GetLambdaBody(selector);
    if (body is null)
    {
      return;
    }

    // FDFRAMES1001: body must be a recognised projection form.
    if (!IsValidProjectionBody(body))
    {
      context.ReportDiagnostic(
        Diagnostic.Create(
          DataFrameDiagnostics.InvalidProjectionBody,
          body.GetLocation(),
          body.ToString()
        )
      );
      return;
    }

    // FDFRAMES1002: every binding in an object initializer must be a plain assignment.
    var initializer = GetObjectInitializer(body);
    if (initializer is not null)
    {
      CheckInitializerBindings(context, initializer);
    }

    // FDFRAMES1003: positional constructor (no initializer) requires a record or anonymous type.
    if (HasPositionalConstructorWithoutInitializer(body))
    {
      CheckPositionalConstructorType(context, body);
    }
  }

  // ─── Aggregate checks (FDFRAMES1004, FDFRAMES1005) ────────────────────────────

  private static void CheckAggregateLambda(
    SyntaxNodeAnalysisContext context,
    TypedFrameInvocation match
  )
  {
    if (match.LambdaArguments.Count == 0)
    {
      return;
    }

    var resultSelector = match.LambdaArguments[0];
    var body = GetLambdaBody(resultSelector);
    if (body is null)
    {
      return;
    }

    // FDFRAMES1004: result selector body must be an object-creation expression.
    if (!IsValidAggregateResultBody(body))
    {
      context.ReportDiagnostic(
        Diagnostic.Create(
          DataFrameDiagnostics.InvalidAggregateResultBody,
          body.GetLocation(),
          body.ToString()
        )
      );
      return;
    }

    // FDFRAMES1005: every binding value must be ctx.Key or ctx.Method(...).
    CheckAggregateBindings(context, body, resultSelector);
  }

  // ─── Shared projection helpers ───────────────────────────────────────────────

  private static ExpressionSyntax? GetLambdaBody(LambdaExpressionSyntax lambda) =>
    lambda switch
    {
      SimpleLambdaExpressionSyntax s => s.Body as ExpressionSyntax,
      ParenthesizedLambdaExpressionSyntax p => p.Body as ExpressionSyntax,
      _ => null,
    };

  private static bool IsValidProjectionBody(ExpressionSyntax body) =>
    body switch
    {
      // new OutputSchema { Prop = ... }
      ObjectCreationExpressionSyntax { Initializer: not null } => true,
      // new OutputSchema(...) — positional
      ObjectCreationExpressionSyntax { ArgumentList.Arguments.Count: > 0 } => true,
      // new { Prop = ... } — anonymous type
      AnonymousObjectCreationExpressionSyntax => true,
      // new(...) { Prop = ... } — implicit new with initializer (C# 9+)
      ImplicitObjectCreationExpressionSyntax { Initializer: not null } => true,
      // new(...) — implicit new positional
      ImplicitObjectCreationExpressionSyntax { ArgumentList.Arguments.Count: > 0 } => true,
      // x.Property — member access passthrough
      MemberAccessExpressionSyntax => true,
      // x — identifier passthrough
      IdentifierNameSyntax => true,
      _ => false,
    };

  private static bool IsValidAggregateResultBody(ExpressionSyntax body) =>
    body switch
    {
      ObjectCreationExpressionSyntax { Initializer: not null } => true,
      ObjectCreationExpressionSyntax { ArgumentList.Arguments.Count: > 0 } => true,
      AnonymousObjectCreationExpressionSyntax => true,
      ImplicitObjectCreationExpressionSyntax { Initializer: not null } => true,
      ImplicitObjectCreationExpressionSyntax { ArgumentList.Arguments.Count: > 0 } => true,
      _ => false,
    };

  private static InitializerExpressionSyntax? GetObjectInitializer(ExpressionSyntax body) =>
    body switch
    {
      ObjectCreationExpressionSyntax oc => oc.Initializer,
      ImplicitObjectCreationExpressionSyntax ic => ic.Initializer,
      _ => null,
    };

  private static bool HasPositionalConstructorWithoutInitializer(ExpressionSyntax body) =>
    body switch
    {
      ObjectCreationExpressionSyntax { Initializer: null } oc => (
        oc.ArgumentList?.Arguments.Count ?? 0
      ) > 0,
      ImplicitObjectCreationExpressionSyntax { Initializer: null } ic => (
        ic.ArgumentList?.Arguments.Count ?? 0
      ) > 0,
      _ => false,
    };

  // ─── FDFRAMES1002 ─────────────────────────────────────────────────────────────

  private static void CheckInitializerBindings(
    SyntaxNodeAnalysisContext context,
    InitializerExpressionSyntax initializer
  )
  {
    foreach (var expr in initializer.Expressions)
    {
      // Fire when an assignment's RHS is itself an initializer expression —
      // this corresponds to a MemberListBinding (Items = { x }) or
      // MemberMemberBinding (Nested = { Prop = val }) in the expression tree.
      if (expr is AssignmentExpressionSyntax { Right: InitializerExpressionSyntax } assignment)
      {
        context.ReportDiagnostic(
          Diagnostic.Create(
            DataFrameDiagnostics.NonAssignmentBinding,
            assignment.GetLocation(),
            assignment.Left.ToString()
          )
        );
      }
    }
  }

  // ─── FDFRAMES1003 ─────────────────────────────────────────────────────────────

  private static void CheckPositionalConstructorType(
    SyntaxNodeAnalysisContext context,
    ExpressionSyntax body
  )
  {
    var typeInfo = context.SemanticModel.GetTypeInfo(body);
    if (typeInfo.Type is INamedTypeSymbol { IsRecord: false, IsAnonymousType: false } namedType)
    {
      context.ReportDiagnostic(
        Diagnostic.Create(
          DataFrameDiagnostics.PositionalConstructorNonRecord,
          body.GetLocation(),
          namedType.Name
        )
      );
    }
  }

  // ─── FDFRAMES1005 ─────────────────────────────────────────────────────────────

  private static void CheckAggregateBindings(
    SyntaxNodeAnalysisContext context,
    ExpressionSyntax body,
    LambdaExpressionSyntax lambda
  )
  {
    var ctxParamName = GetFirstParameterName(lambda);
    if (ctxParamName is null)
    {
      return;
    }

    var ctxSymbol = GetFirstParameterSymbol(lambda, context.SemanticModel);

    foreach (var valueExpr in GetAggregateBindingValues(body))
    {
      if (!IsValidAggregateBindingValue(valueExpr, ctxSymbol, ctxParamName, context.SemanticModel))
      {
        context.ReportDiagnostic(
          Diagnostic.Create(
            DataFrameDiagnostics.InvalidAggregateBinding,
            valueExpr.GetLocation(),
            valueExpr.ToString()
          )
        );
      }
    }
  }

  private static string? GetFirstParameterName(LambdaExpressionSyntax lambda) =>
    lambda switch
    {
      SimpleLambdaExpressionSyntax s => s.Parameter.Identifier.Text,
      ParenthesizedLambdaExpressionSyntax { ParameterList.Parameters.Count: > 0 } p =>
        p.ParameterList.Parameters[0].Identifier.Text,
      _ => null,
    };

  private static IParameterSymbol? GetFirstParameterSymbol(
    LambdaExpressionSyntax lambda,
    SemanticModel semanticModel
  ) =>
    lambda switch
    {
      SimpleLambdaExpressionSyntax s => semanticModel.GetDeclaredSymbol(s.Parameter),
      ParenthesizedLambdaExpressionSyntax { ParameterList.Parameters.Count: > 0 } p =>
        semanticModel.GetDeclaredSymbol(p.ParameterList.Parameters[0]),
      _ => null,
    };

  private static IReadOnlyList<ExpressionSyntax> GetAggregateBindingValues(ExpressionSyntax body)
  {
    var results = new List<ExpressionSyntax>();

    // Object initializer: new TResult { Prop = expr, ... } or new(...) { Prop = expr, ... }
    var init = GetObjectInitializer(body);
    if (init is not null)
    {
      foreach (var expr in init.Expressions)
      {
        if (expr is AssignmentExpressionSyntax a)
        {
          results.Add(a.Right);
        }
      }

      return results;
    }

    // Anonymous type: new { ctx.Key, Name = ctx.Avg(...) }
    if (body is AnonymousObjectCreationExpressionSyntax anon)
    {
      foreach (var d in anon.Initializers)
      {
        results.Add(d.Expression);
      }

      return results;
    }

    // Positional constructor (no initializer): new TResult(ctx.Key, ctx.Avg(...))
    ArgumentListSyntax? argList = body switch
    {
      ObjectCreationExpressionSyntax { Initializer: null } oc => oc.ArgumentList,
      ImplicitObjectCreationExpressionSyntax { Initializer: null } ic => ic.ArgumentList,
      _ => null,
    };

    if (argList is not null)
    {
      foreach (var arg in argList.Arguments)
      {
        results.Add(arg.Expression);
      }
    }

    return results;
  }

  private static bool IsValidAggregateBindingValue(
    ExpressionSyntax expr,
    IParameterSymbol? ctxSymbol,
    string ctxParamName,
    SemanticModel semanticModel
  )
  {
    // ctx.Key — member access on the context parameter.
    if (expr is MemberAccessExpressionSyntax ma)
    {
      return IsContextReceiver(ma.Expression, ctxSymbol, ctxParamName, semanticModel);
    }

    // ctx.Avg(...) / ctx.Sum(...) / ctx.Count() — invocation on the context parameter.
    if (expr is InvocationExpressionSyntax { Expression: MemberAccessExpressionSyntax invMa })
    {
      return IsContextReceiver(invMa.Expression, ctxSymbol, ctxParamName, semanticModel);
    }

    return false;
  }

  private static bool IsContextReceiver(
    ExpressionSyntax receiver,
    IParameterSymbol? ctxSymbol,
    string ctxParamName,
    SemanticModel semanticModel
  )
  {
    if (receiver is not IdentifierNameSyntax id || id.Identifier.Text != ctxParamName)
    {
      return false;
    }

    // Prefer symbol identity when available; fall back to name-only check.
    if (ctxSymbol is not null)
    {
      var symbol = semanticModel.GetSymbolInfo(receiver).Symbol;
      return SymbolEqualityComparer.Default.Equals(symbol, ctxSymbol);
    }

    return true;
  }
}
