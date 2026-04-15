using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace Flowthru.Core.SourceGenerators;

/// <summary>
/// Roslyn analyzer that cross-references RegisterCatalog and RegisterFlow calls
/// within an AddFlowthru configuration block.
///
/// Emits compile-time diagnostics when:
///   FT1001 — A Flow delegate parameter extends DataCatalogBase but no
///            matching RegisterCatalog registration was found.
///   FT1002 — A RegisterCatalog registration is never referenced by any flow.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class FlowthruRegistrationAnalyzer : DiagnosticAnalyzer
{
    /// <summary>
    /// No catalog registered for a Flow parameter that extends DataCatalogBase.
    /// </summary>
    public const string MissingCatalogId = "FT2001";

    /// <summary>
    /// A catalog was registered via RegisterCatalog but is not referenced by any flow.
    /// </summary>
    public const string UnusedCatalogId = "FT2002";

    /// <summary>
    /// A Flow has a concrete-class parameter that is not a catalog and is not covered by
    /// configurationSection, meaning it will be resolved from DI. This may be intentional,
    /// but if the parameter is a configuration POCO, the user should pass configurationSection
    /// to RegisterFlow to bind it from appsettings instead.
    /// </summary>
    public const string UnboundConcreteParamId = "FT2003";

    /// <summary>
    /// A Flow registration specifies a configurationSection but UseConfiguration() was never called
    /// on the builder. The Flow will throw at pre-flight time when it attempts to resolve the
    /// configurationSection.
    /// </summary>
    public const string MissingUseConfigurationId = "FT2004";

    private static readonly DiagnosticDescriptor _missingCatalogRule =
      new(
        MissingCatalogId,
        "Missing catalog registration",
        "Flow '{0}' requires catalog '{1}' but no matching RegisterCatalog registration was found",
        "Flowthru.Core.Registration",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "Every DataCatalogBase-derived parameter in a RegisterFlow delegate must have a corresponding RegisterCatalog registration."
      );

    private static readonly DiagnosticDescriptor _unusedCatalogRule =
      new(
        UnusedCatalogId,
        "Unused catalog registration",
        "Catalog '{0}' is registered via RegisterCatalog but is not referenced by any flow",
        "Flowthru.Core.Registration",
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "A catalog was registered but no flow references it. This may indicate dead configuration."
      );

    private static readonly DiagnosticDescriptor _unboundConcreteParamRule =
      new(
        UnboundConcreteParamId,
        "Unbound concrete parameter",
        "Flow '{0}' has parameter '{1}' of type '{2}' that will be resolved from DI. If it is a configuration object, pass configurationSection to RegisterFlow.",
        "Flowthru.Core.Registration",
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "A concrete-class flow parameter that is not a catalog will be resolved from DI at flow-build time. If it is a configuration POCO, pass configurationSection to RegisterFlow to bind it from appsettings instead."
      );

    private static readonly DiagnosticDescriptor _missingUseConfigurationRule =
      new(
        MissingUseConfigurationId,
        "Missing UseConfiguration call",
        "Flow '{0}' specifies configurationSection '{1}' but UseConfiguration() has not been called",
        "Flowthru.Core.Registration",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "A RegisterFlow call references a configurationSection, but UseConfiguration() was never called on the builder. The flow will throw at pre-flight time."
      );

    /// <summary>
    /// Supported diagnostics for this analyzer. Each descriptor corresponds to a specific
    /// registration issue.
    /// </summary>
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
      ImmutableArray.Create(
        _missingCatalogRule,
        _unusedCatalogRule,
        _unboundConcreteParamRule,
        _missingUseConfigurationRule
      );

    /// <summary>
    /// Initializes the analyzer by registering an operation block action that analyzes
    /// the body of the lambda passed to AddFlowthru. We look for RegisterCatalog and
    /// RegisterFlow calls, extract their relevant information, and cross-reference them to
    /// emit diagnostics for any inconsistencies.
    /// </summary>
    /// <param name="context"></param>
    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterOperationBlockAction(AnalyzeOperationBlock);
    }

    private static void AnalyzeOperationBlock(OperationBlockAnalysisContext context)
    {
        // We look for invocations of AddFlowthru that pass a lambda configuring the builder.
        // Within that lambda, we collect RegisterCatalog and RegisterFlow calls.

        var dataCatalogBaseType = context.Compilation.GetTypeByMetadataName(
          "Flowthru.Core.Data.DataCatalogBase"
        );
        if (dataCatalogBaseType == null)
        {
            return;
        }

        foreach (var block in context.OperationBlocks)
        {
            foreach (var operation in block.DescendantsAndSelf())
            {
                if (operation is not IInvocationOperation invocation)
                {
                    continue;
                }

                // Match: services.AddFlowthru(flowthru => { ... })
                if (invocation.TargetMethod.Name != "AddFlowthru")
                {
                    continue;
                }

                // Find the lambda argument
                var lambdaArg = invocation
                  .Arguments.Select(a => a.Value)
                  .OfType<IDelegateCreationOperation>()
                  .Select(d => d.Target)
                  .OfType<IAnonymousFunctionOperation>()
                  .FirstOrDefault();

                if (lambdaArg?.Body == null)
                {
                    continue;
                }

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
        // Collect registered catalog types from RegisterCatalog calls
        var registeredCatalogs = new System.Collections.Generic.Dictionary<
          string,
          IInvocationOperation
        >();
        // Collect Flow registrations with their required catalogs and any ambiguous concrete params
        var flowRegistrations = new System.Collections.Generic.List<(
          string Label,
          IInvocationOperation Invocation,
          System.Collections.Generic.List<ITypeSymbol> RequiredCatalogs,
          System.Collections.Generic.List<(ITypeSymbol Type, string ParamName)> AmbiguousConcreteParams
        )>();

        bool hasUseConfiguration = body.DescendantsAndSelf()
          .OfType<IInvocationOperation>()
          .Any(c => c.TargetMethod.Name == "UseConfiguration");

        foreach (var descendant in body.DescendantsAndSelf())
        {
            if (descendant is not IInvocationOperation call)
            {
                continue;
            }

            var methodName = call.TargetMethod.Name;

            // ── RegisterCatalog ──
            if (methodName == "RegisterCatalog")
            {
                var catalogType = ResolveCatalogTypeFromRegisterCatalog(call, dataCatalogBaseType);
                if (catalogType != null)
                {
                    var key = catalogType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                    registeredCatalogs[key] = call;
                }
            }

            // ── RegisterFlow ──
            if (methodName == "RegisterFlow")
            {
                var (label, requiredCatalogs, ambiguousConcreteParams) = ResolveFlowRequirements(
                  call,
                  dataCatalogBaseType
                );
                if (label != null)
                {
                    flowRegistrations.Add((label, call, requiredCatalogs, ambiguousConcreteParams));
                }
            }
        }

        // Cross-reference: FT2001 — Flow requires catalog not registered
        var allReferencedCatalogs = new System.Collections.Generic.HashSet<string>();
        foreach (var (label, invocation, requiredCatalogs, _) in flowRegistrations)
        {
            foreach (var required in requiredCatalogs)
            {
                var key = required.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                allReferencedCatalogs.Add(key);

                if (!registeredCatalogs.ContainsKey(key))
                {
                    var diagnostic = Diagnostic.Create(
                      _missingCatalogRule,
                      invocation.Syntax.GetLocation(),
                      label,
                      required.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat)
                    );
                    context.ReportDiagnostic(diagnostic);
                }
            }
        }

        // Cross-reference: FT2002 — catalog registered but never referenced
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
                  _unusedCatalogRule,
                  kvp.Value.Syntax.GetLocation(),
                  catalogName
                );
                context.ReportDiagnostic(diagnostic);
            }
        }

        // FT2003 — concrete non-catalog parameter not bound from configuration
        foreach (var (label, invocation, _, ambiguousConcreteParams) in flowRegistrations)
        {
            foreach (var (paramType, paramName) in ambiguousConcreteParams)
            {
                var diagnostic = Diagnostic.Create(
                  _unboundConcreteParamRule,
                  invocation.Syntax.GetLocation(),
                  label,
                  paramName,
                  paramType.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat)
                );
                context.ReportDiagnostic(diagnostic);
            }
        }

        // FT2004 — configurationSection supplied but UseConfiguration never called
        if (!hasUseConfiguration)
        {
            foreach (var (label, invocation, _, _) in flowRegistrations)
            {
                string? sectionValue = null;
                foreach (var arg in invocation.Arguments)
                {
                    if (
                      arg.Parameter?.Name == "configurationSection"
                      && arg.Value.ConstantValue.HasValue
                      && arg.Value.ConstantValue.Value is string s
                    )
                    {
                        sectionValue = s;
                        break;
                    }
                }

                if (sectionValue != null)
                {
                    var diagnostic = Diagnostic.Create(
                      _missingUseConfigurationRule,
                      invocation.Syntax.GetLocation(),
                      label,
                      sectionValue
                    );
                    context.ReportDiagnostic(diagnostic);
                }
            }
        }
    }

    /// <summary>
    /// Extracts the catalog type from a RegisterCatalog call.
    /// Handles: RegisterCatalog&lt;T&gt;(), RegisterCatalog(instance), RegisterCatalog&lt;T&gt;(factory).
    /// </summary>
    private static ITypeSymbol? ResolveCatalogTypeFromRegisterCatalog(
      IInvocationOperation call,
      INamedTypeSymbol dataCatalogBaseType
    )
    {
        var method = call.TargetMethod;

        // Generic: RegisterCatalog<TCatalog>() or RegisterCatalog<TCatalog>(factory)
        if (method.TypeArguments.Length == 1)
        {
            var typeArg = method.TypeArguments[0];
            if (InheritsFrom(typeArg, dataCatalogBaseType))
            {
                return typeArg;
            }
        }

        // Non-generic: RegisterCatalog(catalogInstance) — infer from argument type.
        // Strip through implicit conversions: passing UpstreamCatalog to a DataCatalogBase
        // parameter wraps the expression in IConversionOperation; the concrete type lives
        // on the innermost operand, not on the outer conversion.
        if (method.TypeArguments.Length == 0 && call.Arguments.Length >= 1)
        {
            IOperation argValue = call.Arguments[0].Value;
            while (argValue is IConversionOperation conv && conv.IsImplicit)
            {
                argValue = conv.Operand;
            }

            var argType = argValue.Type;
            if (argType != null && InheritsFrom(argType, dataCatalogBaseType))
            {
                return argType;
            }
        }

        // Infer from lambda return type: RegisterCatalog(_ => new MyCatalog(...))
        if (method.TypeArguments.Length == 1)
        {
            return method.TypeArguments[0];
        }

        return null;
    }

    /// <summary>
    /// Extracts the Flow label, required catalog types, and unbound concrete parameters
    /// from a RegisterFlow call.
    /// <para>
    /// Parameters are classified the same way the runtime resolver does:
    /// <list type="bullet">
    /// <item>Extends <c>DataCatalogBase</c> → required catalog (FT2001 if missing RegisterCatalog)</item>
    /// <item>Interface → DI-resolved service (no warning — extension territory)</item>
    /// <item>Concrete class, not covered by configurationSection → ambiguous (FT2003)</item>
    /// </list>
    /// </para>
    /// </summary>
    private static (
      string? Label,
      System.Collections.Generic.List<ITypeSymbol> RequiredCatalogs,
      System.Collections.Generic.List<(ITypeSymbol Type, string ParamName)> AmbiguousConcreteParams
    ) ResolveFlowRequirements(IInvocationOperation call, INamedTypeSymbol dataCatalogBaseType)
    {
        string? label = null;
        var requiredCatalogs = new System.Collections.Generic.List<ITypeSymbol>();
        var ambiguousConcreteParams = new System.Collections.Generic.List<(
          ITypeSymbol Type,
          string ParamName
        )>();

        // Extract label
        foreach (var arg in call.Arguments)
        {
            if (arg.Parameter?.Name == "label" && arg.Value.ConstantValue.HasValue)
            {
                label = arg.Value.ConstantValue.Value as string;
                break;
            }
        }

        // Detect whether configurationSection was supplied with a non-null string value.
        // The runtime binds the FIRST concrete non-catalog non-interface param from config;
        // all others fall through to DI.
        bool hasConfigSection = false;
        foreach (var arg in call.Arguments)
        {
            if (
              arg.Parameter?.Name == "configurationSection"
              && arg.Value.ConstantValue.HasValue
              && arg.Value.ConstantValue.Value is string
            )
            {
                hasConfigSection = true;
                break;
            }
        }

        // Resolve Flow parameters via delegate signature or method group.
        System.Collections.Generic.IEnumerable<IParameterSymbol>? flowParams = null;
        foreach (var arg in call.Arguments)
        {
            if (arg.Parameter?.Name != "flow")
            {
                continue;
            }

            // Path 1: lambda / typed Func — read from the delegate's Invoke method
            var delegateType = ResolveMethodSignatureFromArgument(arg.Value);
            if (delegateType?.DelegateInvokeMethod != null)
            {
                flowParams = delegateType.DelegateInvokeMethod.Parameters;
                break;
            }

            // Path 2: method group — unwrap any conversion wrapper and read Method.Parameters
            IOperation value = arg.Value;
            while (value is IConversionOperation conv)
            {
                value = conv.Operand;
            }

            if (value is IMethodReferenceOperation methodRef)
            {
                flowParams = methodRef.Method.Parameters;
            }

            break;
        }

        if (flowParams != null)
        {
            // The runtime consumes configurationSection on the first concrete non-catalog
            // non-interface param it encounters (left to right). Track that slot.
            bool configSectionConsumed = false;

            foreach (var param in flowParams)
            {
                if (InheritsFrom(param.Type, dataCatalogBaseType))
                {
                    // Catalog — must be registered via RegisterCatalog.
                    requiredCatalogs.Add(param.Type);
                }
                else if (param.Type.TypeKind == TypeKind.Interface)
                {
                    // Interface — DI-resolved service. Core has no visibility into what registers
                    // it; extensions own that contract. No diagnostic.
                }
                else
                {
                    // Concrete non-catalog class — could be a config POCO or an explicitly
                    // registered DI type. If configurationSection covers this slot, it's fine.
                    if (hasConfigSection && !configSectionConsumed)
                    {
                        configSectionConsumed = true;
                    }
                    else
                    {
                        ambiguousConcreteParams.Add((param.Type, param.Name));
                    }
                }
            }
        }

        return (label, requiredCatalogs, ambiguousConcreteParams);
    }

    private static INamedTypeSymbol? ResolveMethodSignatureFromArgument(IOperation value)
    {
        // Unwrap conversions (method group → Delegate)
        while (value is IConversionOperation conversion)
        {
            value = conversion.Operand;
        }

        if (value is IDelegateCreationOperation delegateCreation)
        {
            return delegateCreation.Type as INamedTypeSymbol;
        }

        return value.Type as INamedTypeSymbol;
    }

    private static bool InheritsFrom(ITypeSymbol? type, INamedTypeSymbol baseType)
    {
        if (type == null)
        {
            return false;
        }

        var current = type.BaseType;
        while (current != null)
        {
            if (SymbolEqualityComparer.Default.Equals(current, baseType))
            {
                return true;
            }

            current = current.BaseType;
        }
        return false;
    }
}
