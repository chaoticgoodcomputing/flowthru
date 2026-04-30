using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Flowthru.Core.SourceGenerators.StepAnalysis;

/// <summary>
/// Analyzer that emits <c>FT4002</c> when a <c>[FlowthruStep]</c> class has a
/// service-typed <c>Create(...)</c> parameter for which no
/// <c>services.AddFlowthruInspect&lt;T&gt;(...)</c> registration is visible in the
/// host project's compilation. Best-effort static analysis — the runtime preflight
/// backstop catches missed registrations definitively.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class StepInspectorAnalyzer : DiagnosticAnalyzer
{
  internal const string FlowthruStepAttributeFullName = "Flowthru.Core.Steps.FlowthruStepAttribute";
  internal const string FlowthruSchemaAttributeFullName =
    "Flowthru.Core.Abstractions.FlowthruSchemaAttribute";
  internal const string FlowthruConfigAttributeFullName =
    "Flowthru.Core.Abstractions.FlowthruConfigAttribute";
  internal const string AddFlowthruInspectMethodName = "AddFlowthruInspect";
  internal const string CreateMethodName = "Create";

  private static readonly string[] InfrastructureAllowList =
  {
    "Microsoft.Extensions.Logging.ILogger",
    "Microsoft.Extensions.Logging.ILoggerFactory",
  };

  /// <inheritdoc/>
  public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
    ImmutableArray.Create(StepDiagnostics.MissingFlowthruInspector);

  /// <inheritdoc/>
  public override void Initialize(AnalysisContext context)
  {
    context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
    context.EnableConcurrentExecution();

    // CompilationStart begins per-syntax-tree collection of AddFlowthruInspect<T>
    // registrations. CompilationEnd does the cross-reference against [FlowthruStep]
    // classes — running last guarantees all registrations have been collected first.
    // (Symbol actions and syntax actions run concurrently with no completion ordering;
    // CompilationEnd is the only safe place for cross-referencing.)
    context.RegisterCompilationStartAction(static compilationContext =>
    {
      var registeredInspectorTypes = new ConcurrentBag<INamedTypeSymbol>();
      var stepClasses = new ConcurrentBag<INamedTypeSymbol>();

      compilationContext.RegisterSyntaxNodeAction(
        ctx => CollectInspectorRegistrations(ctx, registeredInspectorTypes),
        SyntaxKind.InvocationExpression
      );

      compilationContext.RegisterSymbolAction(
        ctx => CollectStepClass(ctx, stepClasses),
        SymbolKind.NamedType
      );

      compilationContext.RegisterCompilationEndAction(ctx =>
        EmitDiagnosticsForSteps(ctx, stepClasses, registeredInspectorTypes)
      );
    });
  }

  // ── Phase 1: collect inspector registrations ──────────────────────────────

  private static void CollectInspectorRegistrations(
    SyntaxNodeAnalysisContext context,
    ConcurrentBag<INamedTypeSymbol> registeredInspectorTypes
  )
  {
    var invocation = (InvocationExpressionSyntax)context.Node;

    // Cheap syntactic gate: only invocations whose method name is `AddFlowthruInspect`.
    if (!IsAddFlowthruInspectInvocation(invocation, out var typeArgList))
    {
      return;
    }

    foreach (var typeArg in typeArgList!.Arguments)
    {
      var typeInfo = context.SemanticModel.GetTypeInfo(typeArg, context.CancellationToken);
      if (typeInfo.Type is INamedTypeSymbol namedType)
      {
        registeredInspectorTypes.Add(namedType);
      }
    }
  }

  private static bool IsAddFlowthruInspectInvocation(
    InvocationExpressionSyntax invocation,
    out TypeArgumentListSyntax? typeArgList
  )
  {
    typeArgList = null;

    SimpleNameSyntax? nameSyntax = invocation.Expression switch
    {
      MemberAccessExpressionSyntax ma => ma.Name,
      SimpleNameSyntax sn => sn,
      _ => null,
    };

    if (nameSyntax is null || nameSyntax.Identifier.Text != AddFlowthruInspectMethodName)
    {
      return false;
    }

    if (nameSyntax is GenericNameSyntax generic)
    {
      typeArgList = generic.TypeArgumentList;
      return true;
    }

    return false;
  }

  // ── Phase 2: collect [FlowthruStep] classes ───────────────────────────────

  private static void CollectStepClass(
    SymbolAnalysisContext context,
    ConcurrentBag<INamedTypeSymbol> stepClasses
  )
  {
    var typeSymbol = (INamedTypeSymbol)context.Symbol;

    bool isStep = typeSymbol
      .GetAttributes()
      .Any(a => a.AttributeClass?.ToDisplayString() == FlowthruStepAttributeFullName);
    if (isStep)
    {
      stepClasses.Add(typeSymbol);
    }
  }

  // ── Phase 3: cross-reference at CompilationEnd ───────────────────────────

  private static void EmitDiagnosticsForSteps(
    CompilationAnalysisContext context,
    ConcurrentBag<INamedTypeSymbol> stepClasses,
    ConcurrentBag<INamedTypeSymbol> registeredInspectorTypes
  )
  {
    var registeredSet = new HashSet<INamedTypeSymbol>(
      registeredInspectorTypes,
      SymbolEqualityComparer.Default
    );

    foreach (var stepClass in stepClasses)
    {
      var createMethods = stepClass
        .GetMembers(CreateMethodName)
        .OfType<IMethodSymbol>()
        .Where(m => m.IsStatic && m.DeclaredAccessibility == Accessibility.Public)
        .ToList();

      if (createMethods.Count == 0)
      {
        continue;
      }

      var serviceCandidates = new List<ITypeSymbol>();
      foreach (var method in createMethods)
      {
        foreach (var param in method.Parameters)
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

      var location = stepClass.Locations.FirstOrDefault() ?? Location.None;
      foreach (var candidate in serviceCandidates)
      {
        bool isRegistered =
          candidate is INamedTypeSymbol named && registeredSet.Contains(named);
        if (!isRegistered)
        {
          context.ReportDiagnostic(
            Diagnostic.Create(
              StepDiagnostics.MissingFlowthruInspector,
              location,
              stepClass.Name,
              candidate.ToDisplayString()
            )
          );
        }
      }
    }
  }

  // ── Service-candidate classification ──────────────────────────────────────

  /// <summary>
  /// A parameter is treated as a service candidate iff its type is an interface that
  /// is NOT in the universal infrastructure allow-list and NOT marked with
  /// <c>[FlowthruSchema]</c> / <c>[FlowthruConfig]</c>. This is the heuristic shared
  /// across <see cref="StepInspectorAnalyzer"/>, <see cref="StepTraitsAnalyzer"/>, and
  /// <see cref="StepMetadataGenerator"/>.
  /// </summary>
  /// <remarks>
  /// <para>
  /// The interface-only restriction matches realistic DI registration patterns —
  /// services are registered against interfaces (<c>AddSingleton&lt;IFoo, Foo&gt;()</c>),
  /// and steps inject the interface, not the implementation. Plain classes, records,
  /// tuples, and nullable primitives that appear as <c>Create(...)</c> parameters are
  /// data parameters (configuration, options, input bundles), not services.
  /// </para>
  /// </remarks>
  internal static bool IsServiceCandidate(ITypeSymbol type)
  {
    // Only interfaces qualify as service candidates — concrete classes/records/structs
    // are data params (options, configuration, input bundles).
    if (type.TypeKind != TypeKind.Interface)
    {
      return false;
    }

    // Allow-list infrastructure interfaces that DI auto-registers but cannot be
    // meaningfully preflight-inspected.
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

    // Types annotated as Flowthru data are never services.
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
}
