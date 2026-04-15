using System.Collections.Immutable;
using System.Linq;
using Flowthru.Extensions.Spark.Shared;
using Flowthru.Misc.DataFrames.Analyzers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Flowthru.Extensions.Spark.Analyzers;

/// <summary>
/// Validates that method calls inside <c>TypedFrameExtensions</c> lambdas are within the
/// subset that <c>SparkExpressionVisitor</c> can translate.
/// </summary>
/// <remarks>
/// The translatable subset is defined in
/// <see cref="SparkTranslatableOperations"/>, which is the single source of truth shared
/// between this analyzer and the runtime visitor. Adding a method to the visitor without
/// updating <c>SparkTranslatableOperations</c> will be caught by the sync-validation test
/// in <c>Flowthru.Tests.Spark</c>.
/// </remarks>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class SparkExpressionAnalyzer : DiagnosticAnalyzer
{
    // Pre-formatted strings for the diagnostic message — computed once.
    private static readonly string _supportedStringList = string.Join(
      ", ",
      SparkTranslatableOperations.SupportedStringMethods
    );
    private static readonly string _supportedMathList = string.Join(
      ", ",
      SparkTranslatableOperations.SupportedMathMethods
    );

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
      ImmutableArray.Create(SparkDiagnostics.UnsupportedMethodCall);

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

        // Check all lambda arguments for unsupported method calls in their bodies.
        foreach (var lambda in match.LambdaArguments)
        {
            foreach (var inner in lambda.DescendantNodes().OfType<InvocationExpressionSyntax>())
            {
                CheckInnerInvocation(context, inner);
            }
        }
    }

    private static void CheckInnerInvocation(
      SyntaxNodeAnalysisContext context,
      InvocationExpressionSyntax invocation
    )
    {
        if (context.SemanticModel.GetSymbolInfo(invocation).Symbol is not IMethodSymbol method)
        {
            return;
        }

        var declaringType = method.ContainingType;
        if (declaringType is null)
        {
            return;
        }

        // string instance methods: only fire if the declaring type is string and method is
        // not in the supported set.
        if (
          declaringType.SpecialType == SpecialType.System_String
          && !SparkTranslatableOperations.SupportedStringMethods.Contains(method.Name)
        )
        {
            ReportUnsupported(context, invocation, declaringType.Name, method.Name);
            return;
        }

        // Math static methods: only fire if declaring type is System.Math and method is not
        // in the supported set.
        if (
          declaringType.ContainingNamespace?.Name == "System"
          && declaringType.Name == "Math"
          && !SparkTranslatableOperations.SupportedMathMethods.Contains(method.Name)
        )
        {
            ReportUnsupported(context, invocation, declaringType.Name, method.Name);
        }
    }

    private static void ReportUnsupported(
      SyntaxNodeAnalysisContext context,
      InvocationExpressionSyntax invocation,
      string typeName,
      string methodName
    )
    {
        context.ReportDiagnostic(
          Diagnostic.Create(
            SparkDiagnostics.UnsupportedMethodCall,
            invocation.GetLocation(),
            typeName,
            methodName,
            _supportedStringList,
            _supportedMathList
          )
        );
    }
}
