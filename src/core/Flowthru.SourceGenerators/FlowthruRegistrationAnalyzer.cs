using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace Flowthru.SourceGenerators;

/// <summary>
/// Roslyn analyzer that cross-references UseCatalog and RegisterPipeline calls
/// within an AddFlowthru configuration block.
///
/// Emits compile-time diagnostics when:
///   FT1001 — A pipeline delegate parameter extends DataCatalogBase but no
///            matching UseCatalog registration was found.
///   FT1002 — A UseCatalog registration is never referenced by any pipeline.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class FlowthruRegistrationAnalyzer : DiagnosticAnalyzer
{
  public const string MissingCatalogId = "FT2001";
  public const string UnusedCatalogId = "FT2002";

  private static readonly DiagnosticDescriptor MissingCatalogRule =
    new(
      MissingCatalogId,
      "Missing catalog registration",
      "Pipeline '{0}' requires catalog '{1}' but no matching UseCatalog registration was found",
      "Flowthru.Registration",
      DiagnosticSeverity.Error,
      isEnabledByDefault: true,
      description: "Every DataCatalogBase-derived parameter in a RegisterPipeline delegate must have a corresponding UseCatalog registration."
    );

  private static readonly DiagnosticDescriptor UnusedCatalogRule =
    new(
      UnusedCatalogId,
      "Unused catalog registration",
      "Catalog '{0}' is registered via UseCatalog but is not referenced by any pipeline",
      "Flowthru.Registration",
      DiagnosticSeverity.Warning,
      isEnabledByDefault: true,
      description: "A catalog was registered but no pipeline references it. This may indicate dead configuration."
    );

  public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
    ImmutableArray.Create(MissingCatalogRule, UnusedCatalogRule);

  public override void Initialize(AnalysisContext context)
  {
    context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
    context.EnableConcurrentExecution();
    context.RegisterOperationBlockAction(AnalyzeOperationBlock);
  }

  private static void AnalyzeOperationBlock(OperationBlockAnalysisContext context)
  {
    // We look for invocations of AddFlowthru that pass a lambda configuring the builder.
    // Within that lambda, we collect UseCatalog and RegisterPipeline calls.

    var dataCatalogBaseType = context.Compilation.GetTypeByMetadataName(
      "Flowthru.Data.DataCatalogBase"
    );
    if (dataCatalogBaseType == null)
      return;

    foreach (var block in context.OperationBlocks)
    {
      foreach (var operation in block.DescendantsAndSelf())
      {
        if (operation is not IInvocationOperation invocation)
          continue;

        // Match: services.AddFlowthru(flowthru => { ... })
        if (invocation.TargetMethod.Name != "AddFlowthru")
          continue;

        // Find the lambda argument
        var lambdaArg = invocation
          .Arguments.Select(a => a.Value)
          .OfType<IDelegateCreationOperation>()
          .Select(d => d.Target)
          .OfType<IAnonymousFunctionOperation>()
          .FirstOrDefault();

        if (lambdaArg?.Body == null)
          continue;

        AnalyzeFlowthruBlock(context, lambdaArg.Body, dataCatalogBaseType);
      }
    }
  }

  private static void AnalyzeFlowthruBlock(
    OperationBlockAnalysisContext context,
    IBlockOperation body,
    INamedTypeSymbol dataCatalogBaseType
  )
  {
    // Collect registered catalog types from UseCatalog calls
    var registeredCatalogs = new System.Collections.Generic.Dictionary<
      string,
      IInvocationOperation
    >();
    // Collect pipeline registrations with their required catalog types
    var pipelineRegistrations = new System.Collections.Generic.List<(
      string Label,
      IInvocationOperation Invocation,
      System.Collections.Generic.List<ITypeSymbol> RequiredCatalogs
    )>();

    foreach (var descendant in body.DescendantsAndSelf())
    {
      if (descendant is not IInvocationOperation call)
        continue;

      var methodName = call.TargetMethod.Name;

      // ── UseCatalog ──
      if (methodName == "UseCatalog")
      {
        var catalogType = ResolveCatalogTypeFromUseCatalog(call, dataCatalogBaseType);
        if (catalogType != null)
        {
          var key = catalogType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
          registeredCatalogs[key] = call;
        }
      }

      // ── RegisterPipeline ──
      if (methodName == "RegisterPipeline")
      {
        var (label, requiredCatalogs) = ResolvePipelineRequirements(call, dataCatalogBaseType);
        if (label != null)
        {
          pipelineRegistrations.Add((label, call, requiredCatalogs));
        }
      }
    }

    // Cross-reference: FT1001 — pipeline requires catalog not registered
    var allReferencedCatalogs = new System.Collections.Generic.HashSet<string>();
    foreach (var (label, invocation, requiredCatalogs) in pipelineRegistrations)
    {
      foreach (var required in requiredCatalogs)
      {
        var key = required.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        allReferencedCatalogs.Add(key);

        if (!registeredCatalogs.ContainsKey(key))
        {
          var diagnostic = Diagnostic.Create(
            MissingCatalogRule,
            invocation.Syntax.GetLocation(),
            label,
            required.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat)
          );
          context.ReportDiagnostic(diagnostic);
        }
      }
    }

    // Cross-reference: FT1002 — catalog registered but never referenced
    foreach (var kvp in registeredCatalogs)
    {
      if (!allReferencedCatalogs.Contains(kvp.Key))
      {
        var catalogName =
          kvp.Value.TargetMethod.TypeArguments.Length > 0
            ? kvp
              .Value.TargetMethod.TypeArguments[0]
              .ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat)
            : "unknown";

        var diagnostic = Diagnostic.Create(
          UnusedCatalogRule,
          kvp.Value.Syntax.GetLocation(),
          catalogName
        );
        context.ReportDiagnostic(diagnostic);
      }
    }
  }

  /// <summary>
  /// Extracts the catalog type from a UseCatalog call.
  /// Handles: UseCatalog&lt;T&gt;(), UseCatalog(instance), UseCatalog&lt;T&gt;(factory).
  /// </summary>
  private static ITypeSymbol? ResolveCatalogTypeFromUseCatalog(
    IInvocationOperation call,
    INamedTypeSymbol dataCatalogBaseType
  )
  {
    var method = call.TargetMethod;

    // Generic: UseCatalog<TCatalog>() or UseCatalog<TCatalog>(factory)
    if (method.TypeArguments.Length == 1)
    {
      var typeArg = method.TypeArguments[0];
      if (InheritsFrom(typeArg, dataCatalogBaseType))
        return typeArg;
    }

    // Non-generic: UseCatalog(catalogInstance) — infer from argument type.
    // Strip through implicit conversions: passing UpstreamCatalog to a DataCatalogBase
    // parameter wraps the expression in IConversionOperation; the concrete type lives
    // on the innermost operand, not on the outer conversion.
    if (method.TypeArguments.Length == 0 && call.Arguments.Length >= 1)
    {
      IOperation argValue = call.Arguments[0].Value;
      while (argValue is IConversionOperation conv && conv.IsImplicit)
        argValue = conv.Operand;
      var argType = argValue.Type;
      if (argType != null && InheritsFrom(argType, dataCatalogBaseType))
        return argType;
    }

    // Infer from lambda return type: UseCatalog(_ => new MyCatalog(...))
    if (method.TypeArguments.Length == 1)
      return method.TypeArguments[0];

    return null;
  }

  /// <summary>
  /// Extracts the pipeline label and required catalog types from a RegisterPipeline call.
  /// Resolves the Delegate argument to its method signature and inspects parameter types.
  /// </summary>
  private static (
    string? Label,
    System.Collections.Generic.List<ITypeSymbol> RequiredCatalogs
  ) ResolvePipelineRequirements(IInvocationOperation call, INamedTypeSymbol dataCatalogBaseType)
  {
    string? label = null;
    var requiredCatalogs = new System.Collections.Generic.List<ITypeSymbol>();

    // Extract label from first string argument
    foreach (var arg in call.Arguments)
    {
      if (arg.Parameter?.Name == "label" && arg.Value.ConstantValue.HasValue)
      {
        label = arg.Value.ConstantValue.Value as string;
        break;
      }
    }

    // Extract delegate parameter — find the 'pipeline' argument
    foreach (var arg in call.Arguments)
    {
      if (arg.Parameter?.Name != "pipeline")
        continue;

      var delegateType = ResolveMethodSignatureFromArgument(arg.Value);
      if (delegateType == null)
        continue;

      // The delegate's invoke method parameters are the pipeline's dependencies
      var invokeMethod = delegateType.DelegateInvokeMethod;
      if (invokeMethod == null)
        continue;

      foreach (var param in invokeMethod.Parameters)
      {
        if (InheritsFrom(param.Type, dataCatalogBaseType))
        {
          requiredCatalogs.Add(param.Type);
        }
      }
      break;
    }

    // Also try resolving from method group directly
    if (requiredCatalogs.Count == 0)
    {
      foreach (var arg in call.Arguments)
      {
        if (arg.Parameter?.Name != "pipeline")
          continue;

        // For method group conversions, walk to the referenced method
        if (arg.Value is IMethodReferenceOperation methodRef)
        {
          foreach (var param in methodRef.Method.Parameters)
          {
            if (InheritsFrom(param.Type, dataCatalogBaseType))
            {
              requiredCatalogs.Add(param.Type);
            }
          }
        }
        else if (
          arg.Value is IConversionOperation conversion
          && conversion.Operand is IMethodReferenceOperation innerRef
        )
        {
          foreach (var param in innerRef.Method.Parameters)
          {
            if (InheritsFrom(param.Type, dataCatalogBaseType))
            {
              requiredCatalogs.Add(param.Type);
            }
          }
        }
        break;
      }
    }

    return (label, requiredCatalogs);
  }

  private static INamedTypeSymbol? ResolveMethodSignatureFromArgument(IOperation value)
  {
    // Unwrap conversions (method group → Delegate)
    while (value is IConversionOperation conversion)
      value = conversion.Operand;

    if (value is IDelegateCreationOperation delegateCreation)
    {
      return delegateCreation.Type as INamedTypeSymbol;
    }

    return value.Type as INamedTypeSymbol;
  }

  private static bool InheritsFrom(ITypeSymbol? type, INamedTypeSymbol baseType)
  {
    if (type == null)
      return false;

    var current = type.BaseType;
    while (current != null)
    {
      if (SymbolEqualityComparer.Default.Equals(current, baseType))
        return true;
      current = current.BaseType;
    }
    return false;
  }
}
