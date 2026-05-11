using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Flowthru.FUnit.SourceGenerators;

/// <summary>
/// Validates FUnit usage patterns and emits warnings:
/// <list type="bullet">
///   <item><c>FU001</c> — a <c>[FlowthruStep]</c> class has no
///     <c>[FUnitStepTest]</c> methods anywhere in the project.</item>
///   <item><c>FU002</c> — a <c>FUnitContext</c> subclass is not
///     wrapped in <c>#if FUNIT_ENABLED</c>.</item>
///   <item><c>FU100</c> — a <c>[FUnitStepTest]</c>'s step has a
///     service-typed <c>Create(...)</c> parameter that no
///     <c>[FUnitStubContainer]</c> registers.</item>
/// </list>
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class FUnitDiagnosticAnalyzer : DiagnosticAnalyzer
{
  internal const string FlowthruStepAttributeFullName = "Flowthru.Step.FlowthruStepAttribute";
  internal const string StepTestAttributeFullName = "Flowthru.Step.Testing.FUnitStepTestAttribute";
  internal const string FUnitContextFullName = "Flowthru.Step.Testing.FUnitContext";
  internal const string FUnitStubContainerAttributeFullName =
    "Flowthru.Step.Testing.FUnitStubContainerAttribute";
  internal const string FlowthruSchemaAttributeFullName =
    "Flowthru.Data.Schema.FlowthruSchemaAttribute";
  internal const string FlowthruConfigAttributeFullName =
    "Flowthru.Data.Config.FlowthruConfigAttribute";
  internal const string FUnitEnabledGuard = "FUNIT_ENABLED";

  // Service-collection registration method names recognised by the FU100 scan.
  private static readonly HashSet<string> RegistrationMethodNames = new()
  {
    "AddSingleton",
    "AddScoped",
    "AddTransient",
    "AddFlowthruInspect",
    "TryAddSingleton",
    "TryAddScoped",
    "TryAddTransient",
  };

  // Universal infrastructure interfaces — DI auto-registers but not service candidates.
  private static readonly string[] InfrastructureAllowList =
  {
    "Microsoft.Extensions.Logging.ILogger",
    "Microsoft.Extensions.Logging.ILoggerFactory",
  };

  /// <summary>FU001: a <c>[FlowthruStep]</c> class has no <c>[FUnitStepTest]</c> coverage.</summary>
  public static readonly DiagnosticDescriptor Fu001 = new(
    id: "FU001",
    title: "Step has no tests",
    messageFormat:
      "'{0}' is annotated with [FlowthruStep] but has no [FUnitStepTest] methods in this project. "
        + "Pure function nodes without tests are potential failure hotspots.",
    category: "Flowthru.FUnit",
    defaultSeverity: DiagnosticSeverity.Warning,
    isEnabledByDefault: true
  );

  /// <summary>FU002: a <c>FUnitContext</c> subclass is not guarded by <c>#if FUNIT_ENABLED</c>.</summary>
  public static readonly DiagnosticDescriptor Fu002 = new(
    id: "FU002",
    title: "FUnitContext subclass not guarded by #if FUNIT_ENABLED",
    messageFormat:
      "'{0}' inherits from FUnitContext but is not inside a '#if FUNIT_ENABLED' block. "
        + "Without this guard, the class cannot be excluded from Release builds.",
    category: "Flowthru.FUnit",
    defaultSeverity: DiagnosticSeverity.Warning,
    isEnabledByDefault: true
  );

  /// <summary>
  /// FU100: a <c>[FUnitStepTest]</c>'s step has a service-typed <c>Create(...)</c>
  /// parameter that is NOT registered in any visible <c>[FUnitStubContainer]</c>.
  /// Tests would otherwise resolve the service from DI at runtime and fail
  /// (or worse, hit production endpoints) — the diagnostic surfaces the gap at
  /// compile time.
  /// </summary>
  public static readonly DiagnosticDescriptor Fu100 = new(
    id: "FU100",
    title: "Step service has no registered stub for FUnit test",
    messageFormat:
      "[FUnitStepTest] for '{0}' references step '{1}' which takes service parameter '{2}', but no "
        + "[FUnitStubContainer] in this project registers it. Add a registration in a stub "
        + "container's Configure(IServiceCollection) method, or inject directly via Services.",
    category: "Flowthru.FUnit",
    defaultSeverity: DiagnosticSeverity.Warning,
    isEnabledByDefault: true,
    description: "Best-effort scan — registrations factored into helper methods or guarded by "
      + "conditional code may produce false positives. The runtime DI resolution at "
      + "GetRequiredService time will throw if the service is genuinely unregistered.",
    customTags: WellKnownDiagnosticTags.CompilationEnd
  );

  /// <inheritdoc/>
  public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
    ImmutableArray.Create(Fu001, Fu002, Fu100);

  /// <inheritdoc/>
  public override void Initialize(AnalysisContext context)
  {
    context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
    context.EnableConcurrentExecution();

    // FU001 + FU002 — symbol-level checks; safe to run independently per type.
    context.RegisterSymbolAction(AnalyzeType, SymbolKind.NamedType);

    // FU100 — requires whole-compilation cross-reference (stub registrations
    // vs. step service deps). RegisterCompilationStartAction lets us collect
    // across syntax + symbol passes, then emit at CompilationEnd.
    context.RegisterCompilationStartAction(static compilationContext =>
    {
      var registeredServiceTypes = new ConcurrentBag<INamedTypeSymbol>();
      var stepTestMethods = new ConcurrentBag<IMethodSymbol>();

      compilationContext.RegisterSyntaxNodeAction(
        ctx => CollectStubRegistrations(ctx, registeredServiceTypes),
        SyntaxKind.InvocationExpression
      );

      compilationContext.RegisterSymbolAction(
        ctx => CollectStepTestMethod(ctx, stepTestMethods),
        SymbolKind.Method
      );

      compilationContext.RegisterCompilationEndAction(ctx =>
        EmitFu100Diagnostics(ctx, stepTestMethods, registeredServiceTypes)
      );
    });
  }

  private static void AnalyzeType(SymbolAnalysisContext context)
  {
    var typeSymbol = (INamedTypeSymbol)context.Symbol;

    // FU001 — [FlowthruStep] without [FUnitStepTest] coverage.
    var isStep = typeSymbol
      .GetAttributes()
      .Any(a => a.AttributeClass?.ToDisplayString() == FlowthruStepAttributeFullName);
    if (isStep)
    {
      var hasTests = GetAllTypes(typeSymbol.ContainingModule.GlobalNamespace)
        .SelectMany(t => t.GetMembers().OfType<IMethodSymbol>())
        .Any(m =>
          m.GetAttributes().Any(a =>
            a.AttributeClass?.ToDisplayString() == StepTestAttributeFullName
            && a.ConstructorArguments.Length > 0
            && a.ConstructorArguments[0].Value is INamedTypeSymbol target
            && SymbolEqualityComparer.Default.Equals(target, typeSymbol)
          )
        );
      if (!hasTests)
      {
        var location = typeSymbol.Locations.FirstOrDefault() ?? Location.None;
        context.ReportDiagnostic(Diagnostic.Create(Fu001, location, typeSymbol.Name));
      }
    }

    // FU002 — FUnitContext subclass not inside #if FUNIT_ENABLED.
    var isFUnitContextSubclass = false;
    var baseType = typeSymbol.BaseType;
    while (baseType is not null)
    {
      if (baseType.ToDisplayString() == FUnitContextFullName)
      {
        isFUnitContextSubclass = true;
        break;
      }
      baseType = baseType.BaseType;
    }
    if (!isFUnitContextSubclass) return;

    var syntaxRef = typeSymbol.DeclaringSyntaxReferences.FirstOrDefault();
    if (syntaxRef?.GetSyntax() is not ClassDeclarationSyntax classDecl) return;

    if (!FUnitSyntaxHelpers.IsInsidePreprocessorGuard(classDecl, FUnitEnabledGuard))
    {
      context.ReportDiagnostic(
        Diagnostic.Create(Fu002, classDecl.Identifier.GetLocation(), typeSymbol.Name)
      );
    }
  }

  // ─────────────────────────────────────────────────────────────────────────
  // FU100 — collection passes
  // ─────────────────────────────────────────────────────────────────────────

  private static void CollectStubRegistrations(
    SyntaxNodeAnalysisContext context,
    ConcurrentBag<INamedTypeSymbol> registeredServiceTypes
  )
  {
    var invocation = (InvocationExpressionSyntax)context.Node;

    // Cheap syntactic gate: the method name must be a registration verb
    // (AddSingleton, AddScoped, AddTransient, AddFlowthruInspect, etc.) and
    // the call must be inside a [FUnitStubContainer] type's body.
    if (!IsRegistrationInvocation(invocation, out var typeArgList))
    {
      return;
    }
    if (!IsInsideStubContainer(invocation, context.SemanticModel, context.CancellationToken))
    {
      return;
    }

    foreach (var typeArg in typeArgList!.Arguments)
    {
      var typeInfo = context.SemanticModel.GetTypeInfo(typeArg, context.CancellationToken);
      if (typeInfo.Type is INamedTypeSymbol named)
      {
        registeredServiceTypes.Add(named);
      }
    }
  }

  private static bool IsRegistrationInvocation(
    InvocationExpressionSyntax invocation,
    out TypeArgumentListSyntax? typeArgList
  )
  {
    typeArgList = null;
    SimpleNameSyntax? name = invocation.Expression switch
    {
      MemberAccessExpressionSyntax ma => ma.Name,
      SimpleNameSyntax sn => sn,
      _ => null,
    };
    if (name is null || !RegistrationMethodNames.Contains(name.Identifier.Text))
    {
      return false;
    }
    if (name is GenericNameSyntax generic)
    {
      typeArgList = generic.TypeArgumentList;
      return true;
    }
    return false;
  }

  private static bool IsInsideStubContainer(
    SyntaxNode node,
    SemanticModel semanticModel,
    System.Threading.CancellationToken ct
  )
  {
    var enclosingType = node
      .Ancestors()
      .OfType<TypeDeclarationSyntax>()
      .FirstOrDefault();
    if (enclosingType is null)
    {
      return false;
    }

    var symbol = semanticModel.GetDeclaredSymbol(enclosingType, ct) as INamedTypeSymbol;
    if (symbol is null)
    {
      return false;
    }

    return symbol
      .GetAttributes()
      .Any(a => a.AttributeClass?.ToDisplayString() == FUnitStubContainerAttributeFullName);
  }

  private static void CollectStepTestMethod(
    SymbolAnalysisContext context,
    ConcurrentBag<IMethodSymbol> stepTestMethods
  )
  {
    var method = (IMethodSymbol)context.Symbol;
    var attr = method
      .GetAttributes()
      .FirstOrDefault(a => a.AttributeClass?.ToDisplayString() == StepTestAttributeFullName);
    if (attr is not null)
    {
      stepTestMethods.Add(method);
    }
  }

  // ─────────────────────────────────────────────────────────────────────────
  // FU100 — emission at CompilationEnd
  // ─────────────────────────────────────────────────────────────────────────

  private static void EmitFu100Diagnostics(
    CompilationAnalysisContext context,
    ConcurrentBag<IMethodSymbol> stepTestMethods,
    ConcurrentBag<INamedTypeSymbol> registeredServiceTypes
  )
  {
    var registeredSet = new HashSet<INamedTypeSymbol>(
      registeredServiceTypes,
      SymbolEqualityComparer.Default
    );

    foreach (var testMethod in stepTestMethods)
    {
      var stepAttr = testMethod
        .GetAttributes()
        .FirstOrDefault(a => a.AttributeClass?.ToDisplayString() == StepTestAttributeFullName);
      if (stepAttr is null || stepAttr.ConstructorArguments.Length == 0)
      {
        continue;
      }
      if (stepAttr.ConstructorArguments[0].Value is not INamedTypeSymbol stepType)
      {
        continue;
      }

      // Walk the step's Create() params for service candidates.
      var createMethods = stepType
        .GetMembers("Create")
        .OfType<IMethodSymbol>()
        .Where(m => m.IsStatic && m.DeclaredAccessibility == Accessibility.Public)
        .ToList();

      var serviceCandidates = new List<ITypeSymbol>();
      foreach (var create in createMethods)
      {
        foreach (var param in create.Parameters)
        {
          if (
            IsServiceCandidate(param.Type)
            && !serviceCandidates.Any(t =>
              SymbolEqualityComparer.Default.Equals(t, param.Type)
            )
          )
          {
            serviceCandidates.Add(param.Type);
          }
        }
      }

      if (serviceCandidates.Count == 0)
      {
        continue;
      }

      var location = testMethod.Locations.FirstOrDefault() ?? Location.None;
      foreach (var candidate in serviceCandidates)
      {
        bool isRegistered =
          candidate is INamedTypeSymbol named && registeredSet.Contains(named);
        if (!isRegistered)
        {
          // Stash the service type's full name in diagnostic properties so the
          // code-fix can recover it without re-walking the step's Create() params.
          var properties = ImmutableDictionary
            .Create<string, string?>()
            .Add("ServiceFullName", candidate.ToDisplayString());

          context.ReportDiagnostic(
            Diagnostic.Create(
              Fu100,
              location,
              properties,
              testMethod.Name,
              stepType.Name,
              candidate.ToDisplayString()
            )
          );
        }
      }
    }
  }

  /// <summary>
  /// A parameter is a service candidate iff its type is an interface NOT in
  /// the universal infrastructure allow-list and NOT marked with
  /// <c>[FlowthruSchema]</c> / <c>[FlowthruConfig]</c>.
  /// </summary>
  private static bool IsServiceCandidate(ITypeSymbol type)
  {
    if (type.TypeKind != TypeKind.Interface)
    {
      return false;
    }

    var originalFullName = type.OriginalDefinition.ToDisplayString(
      SymbolDisplayFormat.FullyQualifiedFormat.WithGlobalNamespaceStyle(
        SymbolDisplayGlobalNamespaceStyle.Omitted
      )
    );
    foreach (var allowed in InfrastructureAllowList)
    {
      if (originalFullName == allowed)
      {
        return false;
      }
    }

    foreach (var attr in type.GetAttributes())
    {
      var attrName = attr.AttributeClass?.ToDisplayString();
      if (
        attrName == FlowthruSchemaAttributeFullName
        || attrName == FlowthruConfigAttributeFullName
      )
      {
        return false;
      }
    }

    return true;
  }

  private static IEnumerable<INamedTypeSymbol> GetAllTypes(INamespaceSymbol ns)
  {
    foreach (var member in ns.GetMembers())
    {
      if (member is INamedTypeSymbol type)
      {
        foreach (var t in GetAllTypes(type)) yield return t;
      }
      else if (member is INamespaceSymbol nested)
      {
        foreach (var t in GetAllTypes(nested)) yield return t;
      }
    }
  }

  private static IEnumerable<INamedTypeSymbol> GetAllTypes(INamedTypeSymbol type)
  {
    yield return type;
    foreach (var nested in type.GetTypeMembers())
    {
      foreach (var t in GetAllTypes(nested)) yield return t;
    }
  }
}
