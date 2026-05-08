using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Flowthru.Core.SourceGenerators.Schema;

/// <summary>
/// Incremental source generator that analyzes types annotated with
/// <c>[FlowthruSchema]</c> and emits the appropriate marker interfaces
/// (<c>IFlatSchema</c>/<c>INestedSchema</c>, <c>ITextSerializable</c>,
/// <c>IBinarySerializable</c>, <c>IStructuredSerializable</c>) based on
/// the type's property structure.
/// </summary>
[Generator]
public sealed class SchemaInterfaceGenerator : IIncrementalGenerator
{
  private const string AttributeFullName = "Flowthru.Data.Schema.FlowthruSchemaAttribute";
  private const string ColumnAttributeFullName =
    "Flowthru.Data.Schema.FlowthruColumnAttribute";

  private const string IFlatSchemaFullName = "Flowthru.Data.Schema.IFlatSchema";
  private const string INestedSchemaFullName = "Flowthru.Data.Schema.INestedSchema";
  private const string ITextSerializableFullName = "Flowthru.Data.Schema.ITextSerializable";
  private const string IBinarySerializableFullName = "Flowthru.Data.Schema.IBinarySerializable";
  private const string IStructuredSerializableFullName = "Flowthru.Data.Schema.IStructuredSerializable";

  /// <inheritdoc/>
  public void Initialize(IncrementalGeneratorInitializationContext context)
  {
    // Find all type declarations with [FlowthruSchema].
    var candidates = context
      .SyntaxProvider.ForAttributeWithMetadataName(
        AttributeFullName,
        predicate: static (node, _) => node is TypeDeclarationSyntax,
        transform: static (ctx, _) => ExtractSchemaInfo(ctx)
      )
      .Where(static info => info != null)
      .Select(static (info, _) => info!);

    // Collect every NewType simple name declared via [FlowthruColumn].
    // The cross-assembly registry lets schemas reference NewTypes
    // declared elsewhere and still be classified as flat — even though
    // the generated NewType may not be visible in this generator's input
    // compilation.
    var columnNewTypeNames = context
      .SyntaxProvider.ForAttributeWithMetadataName(
        ColumnAttributeFullName,
        predicate: static (node, _) => node is PropertyDeclarationSyntax,
        transform: static (ctx, _) => ExtractColumnNewTypeName(ctx)
      )
      .Where(static name => !string.IsNullOrEmpty(name))
      .Collect();

    var combined = candidates.Combine(columnNewTypeNames);
    context.RegisterSourceOutput(
      combined,
      static (ctx, pair) => EmitSchemaInterfaces(ctx, pair.Left, pair.Right)
    );
  }

  private static string ExtractColumnNewTypeName(GeneratorAttributeSyntaxContext ctx)
  {
    if (ctx.TargetNode is not PropertyDeclarationSyntax property)
    {
      return string.Empty;
    }

    var name = property.Type.ToString();
    // Strip nullable annotation so `ShuttleId?` registers as `ShuttleId`.
    if (name.EndsWith("?"))
    {
      name = name.Substring(0, name.Length - 1);
    }
    return name;
  }

  private static SchemaGenerationInfo? ExtractSchemaInfo(GeneratorAttributeSyntaxContext ctx)
  {
    if (ctx.TargetSymbol is not INamedTypeSymbol typeSymbol)
    {
      return null;
    }

    var typeDeclaration = (TypeDeclarationSyntax)ctx.TargetNode;
    bool isPartial = typeDeclaration.Modifiers.Any(SyntaxKind.PartialKeyword);

    // Tentative classification using only locally-visible information.
    // May be promoted to flat at emission time once the cross-assembly
    // NewType registry is available.
    var tentative = SchemaPropertyClassifier.Classify(typeSymbol);

    var classifications = ImmutableArray.CreateBuilder<PropertyClassification>();
    foreach (var p in tentative.Properties)
    {
      var typeForName = p.Type;
      if (
        typeForName is INamedTypeSymbol
        {
          OriginalDefinition.SpecialType: SpecialType.System_Nullable_T
        } nullable
      )
      {
        typeForName = nullable.TypeArguments[0];
      }

      var isBasicallyFlat =
        SchemaPropertyClassifier.IsFlatPropertyType(p.Type)
        || HasFlowthruColumnAttribute(p);

      classifications.Add(new PropertyClassification(typeForName.Name, isBasicallyFlat));
    }

    var manualInterfaces = DetectManualInterfaces(typeSymbol);

    var namespaceName = typeSymbol.ContainingNamespace.IsGlobalNamespace
      ? ""
      : typeSymbol.ContainingNamespace.ToDisplayString();

    var typeKind = typeDeclaration switch
    {
      RecordDeclarationSyntax r when r.ClassOrStructKeyword.IsKind(SyntaxKind.StructKeyword) =>
        "record struct",
      RecordDeclarationSyntax _ => "record",
      StructDeclarationSyntax _ => "struct",
      _ => "class",
    };

    return new SchemaGenerationInfo(
      typeName: typeSymbol.Name,
      @namespace: namespaceName,
      typeKind: typeKind,
      isPartial: isPartial,
      isFlat: tentative.IsFlat,
      manualInterfaces: manualInterfaces,
      propertyClassifications: classifications.ToImmutable()
    );
  }

  private static bool HasFlowthruColumnAttribute(IPropertySymbol property) =>
    property.GetAttributes()
      .Any(a => a.AttributeClass?.ToDisplayString() == ColumnAttributeFullName);

  private static ImmutableArray<string> DetectManualInterfaces(INamedTypeSymbol typeSymbol)
  {
    var markerNames = new[]
    {
      IFlatSchemaFullName,
      INestedSchemaFullName,
      ITextSerializableFullName,
      IBinarySerializableFullName,
      IStructuredSerializableFullName,
    };

    var builder = ImmutableArray.CreateBuilder<string>();
    foreach (var iface in typeSymbol.Interfaces)
    {
      var fullName = iface.ToDisplayString();
      if (markerNames.Contains(fullName))
      {
        builder.Add(fullName);
      }
    }
    return builder.ToImmutable();
  }

  private static void EmitSchemaInterfaces(
    SourceProductionContext ctx,
    SchemaGenerationInfo info,
    ImmutableArray<string> knownNewTypeNames
  )
  {
    // Skip non-partial types; FlowthruSchemaAnalyzer emits FT1001.
    if (!info.IsPartial)
    {
      return;
    }

    // Finalize flat/nested using the cross-assembly NewType registry.
    var registry = knownNewTypeNames.IsDefaultOrEmpty
      ? ImmutableHashSet<string>.Empty
      : knownNewTypeNames.ToImmutableHashSet();

    bool isFlat = info.IsFlat
      || info.PropertyClassifications.All(p =>
        p.IsBasicallyFlat || registry.Contains(p.SimpleTypeName)
      );

    // Build the interface list. Skip any interfaces the user already
    // applied manually — duplicating would cause CS8646.
    var interfaces = new List<string>();

    if (isFlat)
    {
      if (!info.ManualInterfaces.Contains(IFlatSchemaFullName))
      {
        interfaces.Add(IFlatSchemaFullName);
      }
      if (!info.ManualInterfaces.Contains(ITextSerializableFullName))
      {
        interfaces.Add(ITextSerializableFullName);
      }
      if (!info.ManualInterfaces.Contains(IBinarySerializableFullName))
      {
        interfaces.Add(IBinarySerializableFullName);
      }
      if (!info.ManualInterfaces.Contains(IStructuredSerializableFullName))
      {
        interfaces.Add(IStructuredSerializableFullName);
      }
    }
    else
    {
      if (!info.ManualInterfaces.Contains(INestedSchemaFullName))
      {
        interfaces.Add(INestedSchemaFullName);
      }
      if (!info.ManualInterfaces.Contains(IStructuredSerializableFullName))
      {
        interfaces.Add(IStructuredSerializableFullName);
      }
    }

    if (interfaces.Count == 0)
    {
      return;
    }

    var sb = new StringBuilder();
    sb.AppendLine("// <auto-generated/>");
    sb.AppendLine("#nullable enable");
    sb.AppendLine();

    if (!string.IsNullOrEmpty(info.Namespace))
    {
      sb.AppendLine($"namespace {info.Namespace};");
      sb.AppendLine();
    }

    var interfaceList = string.Join(", ", interfaces.Select(i => $"global::{i}"));
    sb.AppendLine($"partial {info.TypeKind} {info.TypeName} : {interfaceList}");
    sb.AppendLine("{");
    sb.AppendLine("}");

    ctx.AddSource(
      $"{info.TypeName}.SchemaInterfaces.g.cs",
      SourceText.From(sb.ToString(), Encoding.UTF8)
    );
  }
}

/// <summary>Immutable schema info captured at extraction time.</summary>
internal sealed class SchemaGenerationInfo
{
  public string TypeName { get; }
  public string Namespace { get; }
  public string TypeKind { get; }
  public bool IsPartial { get; }
  public bool IsFlat { get; }
  public ImmutableArray<string> ManualInterfaces { get; }

  /// <summary>
  /// Per-property classification snapshot: simple type name + whether the
  /// property is definitely flat by local rules (CLR primitive / IScalar /
  /// [FlowthruColumn]). Used at emission time to finalize <see cref="IsFlat"/>
  /// against the cross-assembly NewType registry.
  /// </summary>
  public ImmutableArray<PropertyClassification> PropertyClassifications { get; }

  public SchemaGenerationInfo(
    string typeName,
    string @namespace,
    string typeKind,
    bool isPartial,
    bool isFlat,
    ImmutableArray<string> manualInterfaces,
    ImmutableArray<PropertyClassification> propertyClassifications
  )
  {
    TypeName = typeName;
    Namespace = @namespace;
    TypeKind = typeKind;
    IsPartial = isPartial;
    IsFlat = isFlat;
    ManualInterfaces = manualInterfaces;
    PropertyClassifications = propertyClassifications;
  }
}

/// <summary>
/// Snapshot of a single property's classification status, captured at
/// extraction time so the emission stage can finalize flat/nested
/// classification against the cross-assembly NewType registry.
/// </summary>
internal readonly record struct PropertyClassification(string SimpleTypeName, bool IsBasicallyFlat);
