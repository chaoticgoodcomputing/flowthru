using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Flowthru.Core.SourceGenerators.StepAnalysis;

/// <summary>
/// Incremental source generator that emits a sibling <c>{StepClassName}_Metadata</c>
/// static class for every <c>[FlowthruStep]</c>-attributed type. The emitted class
/// carries:
/// <list type="bullet">
/// <item><c>Traits</c> — populated from the attribute's
///   <c>IsIdempotent</c> / <c>HasSideEffects</c> properties.</item>
/// <item><c>ServiceDependencies</c> — the inferred service-typed parameters of the
///   step's <c>Create(...)</c> method, filtered through the universal infrastructure
///   allow-list and excluding <c>[FlowthruSchema]</c> / <c>[FlowthruConfig]</c> types.</item>
/// </list>
/// </summary>
[Generator]
public sealed class StepMetadataGenerator : IIncrementalGenerator
{
  private const string FlowthruStepAttributeFullName = "Flowthru.Core.Steps.FlowthruStepAttribute";
  private const string FlowthruSchemaAttributeFullName =
    "Flowthru.Core.Abstractions.FlowthruSchemaAttribute";
  private const string FlowthruConfigAttributeFullName =
    "Flowthru.Core.Abstractions.FlowthruConfigAttribute";
  private const string CreateMethodName = "Create";

  // Universal infrastructure allow-list — interface types that DI auto-registers but
  // cannot be meaningfully preflight-inspected.
  private static readonly string[] InfrastructureAllowList =
  {
    "Microsoft.Extensions.Logging.ILogger",
    "Microsoft.Extensions.Logging.ILoggerFactory",
  };

  /// <inheritdoc/>
  public void Initialize(IncrementalGeneratorInitializationContext context)
  {
    var candidates = context
      .SyntaxProvider.ForAttributeWithMetadataName(
        FlowthruStepAttributeFullName,
        predicate: static (node, _) => node is TypeDeclarationSyntax,
        transform: static (ctx, _) => ExtractStepInfo(ctx)
      )
      .Where(static info => info != null)
      .Select(static (info, _) => info!);

    context.RegisterSourceOutput(candidates, static (ctx, info) => EmitMetadata(ctx, info));
  }

  private static StepGenerationInfo? ExtractStepInfo(GeneratorAttributeSyntaxContext ctx)
  {
    if (ctx.TargetSymbol is not INamedTypeSymbol typeSymbol)
    {
      return null;
    }

    // Attribute parameters: read IsIdempotent and HasSideEffects from named arguments.
    bool isIdempotent = false;
    bool hasSideEffects = false;
    var attribute = typeSymbol
      .GetAttributes()
      .FirstOrDefault(a => a.AttributeClass?.ToDisplayString() == FlowthruStepAttributeFullName);
    if (attribute is not null)
    {
      foreach (var named in attribute.NamedArguments)
      {
        if (named.Key == "IsIdempotent" && named.Value.Value is bool i)
        {
          isIdempotent = i;
        }
        else if (named.Key == "HasSideEffects" && named.Value.Value is bool h)
        {
          hasSideEffects = h;
        }
      }
    }

    // Resolve Create(...) — the canonical step factory method. There may be multiple
    // overloads (e.g., parameterless + service-injected); we pick the one with the most
    // service parameters to capture the richest dependency surface.
    var createMethods = typeSymbol
      .GetMembers(CreateMethodName)
      .OfType<IMethodSymbol>()
      .Where(m => m.IsStatic && m.DeclaredAccessibility == Accessibility.Public)
      .ToList();

    var serviceTypes = new List<string>();
    if (createMethods.Count > 0)
    {
      // Pick the overload with the most service-candidate parameters.
      IMethodSymbol? best = null;
      var bestServiceTypes = new List<string>();
      foreach (var method in createMethods)
      {
        var candidates = ExtractServiceCandidates(method);
        if (best is null || candidates.Count > bestServiceTypes.Count)
        {
          best = method;
          bestServiceTypes = candidates;
        }
      }
      serviceTypes = bestServiceTypes;
    }

    var namespaceName = typeSymbol.ContainingNamespace.IsGlobalNamespace
      ? string.Empty
      : typeSymbol.ContainingNamespace.ToDisplayString();

    return new StepGenerationInfo(
      typeName: typeSymbol.Name,
      @namespace: namespaceName,
      isIdempotent: isIdempotent,
      hasSideEffects: hasSideEffects,
      serviceTypeFullNames: serviceTypes.ToImmutableArray()
    );
  }

  private static List<string> ExtractServiceCandidates(IMethodSymbol method)
  {
    var result = new List<string>();
    foreach (var param in method.Parameters)
    {
      if (IsServiceCandidate(param.Type))
      {
        var fullyQualified = param
          .Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        if (!result.Contains(fullyQualified))
        {
          result.Add(fullyQualified);
        }
      }
    }
    return result;
  }

  /// <summary>
  /// A parameter is treated as a service candidate iff its type is an interface that
  /// is NOT in the universal infrastructure allow-list and NOT marked with
  /// <c>[FlowthruSchema]</c> / <c>[FlowthruConfig]</c>. Mirrors the heuristic in
  /// <c>StepInspectorAnalyzer</c> and <c>StepTraitsAnalyzer</c>.
  /// </summary>
  private static bool IsServiceCandidate(ITypeSymbol type)
  {
    // Only interfaces qualify as service candidates.
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
      if (attrName == FlowthruSchemaAttributeFullName || attrName == FlowthruConfigAttributeFullName)
      {
        return false;
      }
    }

    return true;
  }

  private static void EmitMetadata(SourceProductionContext ctx, StepGenerationInfo info)
  {
    var sb = new StringBuilder();
    sb.AppendLine("// <auto-generated/>");
    sb.AppendLine("#nullable enable");
    sb.AppendLine();
    sb.AppendLine("using System;");
    sb.AppendLine("using System.CodeDom.Compiler;");
    sb.AppendLine("using System.Collections.Generic;");
    sb.AppendLine();

    if (!string.IsNullOrEmpty(info.Namespace))
    {
      sb.AppendLine($"namespace {info.Namespace};");
      sb.AppendLine();
    }

    sb.AppendLine(
      "[GeneratedCode(\"Flowthru.Core.SourceGenerators.StepMetadataGenerator\", \"1.0\")]"
    );
    sb.AppendLine($"internal static class {info.TypeName}_Metadata");
    sb.AppendLine("{");
    sb.AppendLine($"  public static readonly global::Flowthru.Core.Steps.StepTraits Traits =");
    sb.AppendLine(
      $"    new global::Flowthru.Core.Steps.StepTraits(IsIdempotent: {(info.IsIdempotent ? "true" : "false")}, HasSideEffects: {(info.HasSideEffects ? "true" : "false")});"
    );
    sb.AppendLine();
    sb.AppendLine("  public static readonly IReadOnlyList<Type> ServiceDependencies =");
    if (info.ServiceTypeFullNames.IsDefaultOrEmpty)
    {
      sb.AppendLine("    Array.Empty<Type>();");
    }
    else
    {
      sb.AppendLine("    new Type[]");
      sb.AppendLine("    {");
      foreach (var typeFullName in info.ServiceTypeFullNames)
      {
        sb.AppendLine($"      typeof({typeFullName}),");
      }
      sb.AppendLine("    };");
    }
    sb.AppendLine("}");

    var hintName = string.IsNullOrEmpty(info.Namespace)
      ? $"{info.TypeName}.StepMetadata.g.cs"
      : $"{info.Namespace}.{info.TypeName}.StepMetadata.g.cs";
    ctx.AddSource(hintName, SourceText.From(sb.ToString(), Encoding.UTF8));
  }

  private sealed class StepGenerationInfo
  {
    public string TypeName { get; }
    public string Namespace { get; }
    public bool IsIdempotent { get; }
    public bool HasSideEffects { get; }
    public ImmutableArray<string> ServiceTypeFullNames { get; }

    public StepGenerationInfo(
      string typeName,
      string @namespace,
      bool isIdempotent,
      bool hasSideEffects,
      ImmutableArray<string> serviceTypeFullNames
    )
    {
      TypeName = typeName;
      Namespace = @namespace;
      IsIdempotent = isIdempotent;
      HasSideEffects = hasSideEffects;
      ServiceTypeFullNames = serviceTypeFullNames;
    }
  }
}
