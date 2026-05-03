using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Flowthru.Core.SourceGenerators.SchemaAnalysis;

/// <summary>
/// Incremental source generator that analyzes types annotated with [FlowthruSchema]
/// and emits the appropriate marker interface implementations (IFlatSchema/INestedSchema,
/// ITextSerializable, IBinarySerializable, IStructuredSerializable) based on the type's
/// actual property structure.
/// </summary>
[Generator]
public class SchemaInterfaceGenerator : IIncrementalGenerator
{
  private const string AttributeFullName = "Flowthru.Core.Abstractions.FlowthruSchemaAttribute";
  private const string AttributeShortName = "FlowthruSchema";
  private const string ColumnAttributeFullName =
    "Flowthru.Core.Abstractions.FlowthruColumnAttribute";

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
      .Select(static (info, _) => info);

    // Collect every NewType simple-name declared via [FlowthruColumn] anywhere in the
    // compilation. This registry lets schemas reference NewTypes declared elsewhere (in
    // any namespace) and still be classified as flat — even though the generated NewType
    // is invisible to this generator's input compilation.
    var columnNewTypeNames = context
      .SyntaxProvider.ForAttributeWithMetadataName(
        ColumnAttributeFullName,
        predicate: static (node, _) => node is PropertyDeclarationSyntax,
        transform: static (ctx, _) => ExtractColumnNewTypeName(ctx)
      )
      .Where(static name => !string.IsNullOrEmpty(name))
      .Collect();

    // Emit per-schema marker interfaces, finalizing flat/nested classification with the
    // collected NewType registry.
    var combined = candidates.Combine(columnNewTypeNames);
    context.RegisterSourceOutput(
      combined,
      static (ctx, pair) => EmitSchemaInterfaces(ctx, pair.Left, pair.Right)
    );

    // Phase 5: Collect all schemas and emit manifest for Python schema generation
    var allSchemas = candidates.Collect();
    var allSchemasWithRegistry = allSchemas.Combine(columnNewTypeNames);
    context.RegisterSourceOutput(
      allSchemasWithRegistry,
      static (ctx, pair) => EmitSchemaManifest(ctx, pair.Left, pair.Right)
    );
  }

  /// <summary>
  /// Extracts the simple type name of the property bearing a <c>[FlowthruColumn]</c>
  /// attribute — read directly from syntax so the lookup works even if the NewType
  /// hasn't yet been generated.
  /// </summary>
  private static string ExtractColumnNewTypeName(GeneratorAttributeSyntaxContext ctx)
  {
    if (ctx.TargetNode is not PropertyDeclarationSyntax propertyDeclaration)
    {
      return string.Empty;
    }

    var name = propertyDeclaration.Type.ToString();
    // Strip nullable annotation so a property typed `ShuttleId?` registers as `ShuttleId`.
    if (name.EndsWith("?"))
    {
      name = name.Substring(0, name.Length - 1);
    }
    return name;
  }

  /// <summary>
  /// Extracts the information needed for code generation from a syntax/semantic context.
  /// Returns null if the type is unsuitable (e.g., not partial — a diagnostic is reported separately).
  /// </summary>
  private static SchemaGenerationInfo ExtractSchemaInfo(GeneratorAttributeSyntaxContext ctx)
  {
#pragma warning disable CS8603
    if (!(ctx.TargetSymbol is INamedTypeSymbol typeSymbol))
    {
      return null;
    }
#pragma warning restore CS8603

    var typeDeclaration = (TypeDeclarationSyntax)ctx.TargetNode;

    // Check for partial modifier
    bool isPartial = typeDeclaration.Modifiers.Any(SyntaxKind.PartialKeyword);

    // Compute a tentative flat/nested classification using only locally-visible information
    // (CLR primitives, IScalar implementors, [FlowthruColumn] on this schema's own properties).
    // This may be promoted to flat at emission time once the cross-assembly NewType
    // registry is available.
    var tentativeClassification = SchemaPropertyClassifier.Classify(typeSymbol);

    // Capture the simple-name + tentative-flat snapshot of every property for late
    // classification finalization. Only properties that are NOT yet definitely flat need
    // to be re-checked against the registry — but we capture all of them for clarity.
    var classifications = ImmutableArray.CreateBuilder<PropertyClassification>();
    foreach (var p in tentativeClassification.Properties)
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

    // Detect manually-applied marker interfaces
    var manualInterfaces = DetectManualInterfaces(typeSymbol);

    // Phase 5: Extract property information for manifest generation
    var properties = ExtractProperties(typeSymbol);

    // Build namespace and type hierarchy for emission
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
      isFlat: tentativeClassification.IsFlat,
      manualInterfaces: manualInterfaces,
      location: typeDeclaration.Identifier.GetLocation(),
      properties: properties,
      propertyClassifications: classifications.ToImmutable()
    );
  }

  /// <summary>
  /// Whether the property carries the <c>[FlowthruColumn]</c> attribute. Mirrored from
  /// <see cref="SchemaPropertyClassifier"/> so this generator can compute a tentative
  /// classification snapshot without exposing internals.
  /// </summary>
  private static bool HasFlowthruColumnAttribute(IPropertySymbol property)
  {
    return property.GetAttributes()
      .Any(a => a.AttributeClass?.ToDisplayString() == ColumnAttributeFullName);
  }

  /// <summary>
  /// Extracts property information from a schema type (Phase 5).
  /// </summary>
  private static ImmutableArray<SchemaPropertyInfo> ExtractProperties(INamedTypeSymbol typeSymbol)
  {
    var builder = ImmutableArray.CreateBuilder<SchemaPropertyInfo>();

    foreach (var member in typeSymbol.GetMembers())
    {
      if (
        member is IPropertySymbol property
        && property.DeclaredAccessibility == Accessibility.Public
        && !property.IsStatic
      )
      {
        var typeName = property.Type.ToDisplayString(
          SymbolDisplayFormat.FullyQualifiedFormat.WithGlobalNamespaceStyle(
            SymbolDisplayGlobalNamespaceStyle.Omitted
          )
        );

        builder.Add(new SchemaPropertyInfo(property.Name, typeName));
      }
    }

    return builder.ToImmutable();
  }

  /// <summary>
  /// Checks if the type already manually implements any of the marker interfaces.
  /// </summary>
  private static ImmutableArray<string> DetectManualInterfaces(INamedTypeSymbol typeSymbol)
  {
    var markerNames = new[]
    {
      "Flowthru.Core.Abstractions.IFlatSchema",
      "Flowthru.Core.Abstractions.INestedSchema",
      "Flowthru.Core.Abstractions.ITextSerializable",
      "Flowthru.Core.Abstractions.IBinarySerializable",
      "Flowthru.Core.Abstractions.IStructuredSerializable",
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

  /// <summary>
  /// Emits the partial class/record with interface implementations.
  /// Diagnostics (FT1001, FT1002) are emitted by <see cref="FlowthruSchemaAnalyzer"/>,
  /// not here — generators should only generate code.
  /// </summary>
  private static void EmitSchemaInterfaces(
    SourceProductionContext ctx,
    SchemaGenerationInfo info,
    ImmutableArray<string> knownNewTypeNames
  )
  {
    // Skip generation for non-partial types; FlowthruSchemaAnalyzer emits FT1001.
    if (!info.IsPartial)
    {
      return;
    }

    // Finalize flat/nested classification using the cross-assembly NewType registry.
    // A property whose type simple name matches any [FlowthruColumn]-declared NewType is
    // treated as flat — even if the property itself doesn't carry [FlowthruColumn] (i.e.,
    // it references a NewType declared elsewhere via `using`).
    var registry = knownNewTypeNames.IsDefaultOrEmpty
      ? ImmutableHashSet<string>.Empty
      : knownNewTypeNames.ToImmutableHashSet();

    bool isFlat;
    if (info.IsFlat)
    {
      isFlat = true; // already flat per local classification
    }
    else
    {
      isFlat = info.PropertyClassifications.All(p =>
        p.IsBasicallyFlat || registry.Contains(p.SimpleTypeName)
      );
    }

    // Build the interface list based on classification.
    // Do NOT emit interfaces that the user already manually applied — that would cause
    // CS8646 (interface already listed in the interface list).
    var interfaces = new List<string>();

    if (isFlat)
    {
      if (!info.ManualInterfaces.Contains("Flowthru.Core.Abstractions.IFlatSchema"))
      {
        interfaces.Add("Flowthru.Core.Abstractions.IFlatSchema");
      }

      if (!info.ManualInterfaces.Contains("Flowthru.Core.Abstractions.ITextSerializable"))
      {
        interfaces.Add("Flowthru.Core.Abstractions.ITextSerializable");
      }

      if (!info.ManualInterfaces.Contains("Flowthru.Core.Abstractions.IBinarySerializable"))
      {
        interfaces.Add("Flowthru.Core.Abstractions.IBinarySerializable");
      }

      if (!info.ManualInterfaces.Contains("Flowthru.Core.Abstractions.IStructuredSerializable"))
      {
        interfaces.Add("Flowthru.Core.Abstractions.IStructuredSerializable");
      }
    }
    else
    {
      // Nested
      if (!info.ManualInterfaces.Contains("Flowthru.Core.Abstractions.INestedSchema"))
      {
        interfaces.Add("Flowthru.Core.Abstractions.INestedSchema");
      }

      if (!info.ManualInterfaces.Contains("Flowthru.Core.Abstractions.IStructuredSerializable"))
      {
        interfaces.Add("Flowthru.Core.Abstractions.IStructuredSerializable");
      }
    }

    // If all interfaces were already manually applied, no source to emit (but warning was reported)
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

    var interfaceList = string.Join(", ", interfaces.Select(QualifyInterface));
    sb.AppendLine($"partial {info.TypeKind} {info.TypeName} : {interfaceList}");
    sb.AppendLine("{");
    sb.AppendLine("}");

    ctx.AddSource(
      $"{info.TypeName}.SchemaInterfaces.g.cs",
      SourceText.From(sb.ToString(), Encoding.UTF8)
    );
  }

  /// <summary>
  /// Converts a fully-qualified interface name to a global-qualified reference
  /// to avoid namespace conflicts.
  /// </summary>
  private static string QualifyInterface(string fullName) => $"global::{fullName}";

  /// <summary>
  /// Emits a consolidated manifest of all schemas for Python schema generation (Phase 5).
  /// The NewType registry is accepted for signature consistency with the per-schema emitter
  /// but currently unused — manifest contents do not depend on flat/nested classification.
  /// </summary>
  private static void EmitSchemaManifest(
    SourceProductionContext ctx,
    ImmutableArray<SchemaGenerationInfo> schemas,
    ImmutableArray<string> knownNewTypeNames
  )
  {
    _ = knownNewTypeNames;
    // Only emit manifest if there are schemas
    if (schemas.Length == 0)
    {
      return;
    }

    var sb = new StringBuilder();
    sb.AppendLine("// <auto-generated/>");
    sb.AppendLine("// Phase 5: Schema manifest for Python schema generation");
    sb.AppendLine("#nullable enable");
    sb.AppendLine();
    sb.AppendLine("namespace Flowthru.Core.Generated.SchemaManifest;");
    sb.AppendLine();
    sb.AppendLine("/// <summary>");
    sb.AppendLine("/// Manifest of all [FlowthruSchema] types for Python schema generation.");
    sb.AppendLine("/// Used by MSBuild task to emit .py schema files.");
    sb.AppendLine("/// </summary>");
    sb.AppendLine("internal static class SchemaManifest");
    sb.AppendLine("{");
    sb.AppendLine("  public static readonly SchemaInfo[] Schemas = new[]");
    sb.AppendLine("  {");

    foreach (var schema in schemas)
    {
      // Skip schemas without valid metadata
      if (!schema.IsPartial)
      {
        continue;
      }

      sb.AppendLine($"    new SchemaInfo(");
      sb.AppendLine($"      \"{EscapeString(schema.TypeName)}\",");
      sb.AppendLine($"      \"{EscapeString(schema.Namespace)}\",");
      sb.AppendLine($"      {(schema.IsFlat ? "true" : "false")},");
      sb.AppendLine($"      new PropertyInfo[]");
      sb.AppendLine($"      {{");

      foreach (var prop in schema.Properties)
      {
        sb.AppendLine(
          $"        new PropertyInfo(\"{EscapeString(prop.Name)}\", \"{EscapeString(prop.TypeName)}\"),"
        );
      }

      sb.AppendLine($"      }}");
      sb.AppendLine($"    ),");
    }

    sb.AppendLine("  };");
    sb.AppendLine("}");
    sb.AppendLine();
    sb.AppendLine("/// <summary>");
    sb.AppendLine("/// Information about a schema type.");
    sb.AppendLine("/// </summary>");
    sb.AppendLine(
      "internal record SchemaInfo(string Name, string Namespace, bool IsFlat, PropertyInfo[] Properties);"
    );
    sb.AppendLine();
    sb.AppendLine("/// <summary>");
    sb.AppendLine("/// Information about a schema property.");
    sb.AppendLine("/// </summary>");
    sb.AppendLine("internal record PropertyInfo(string Name, string TypeName);");

    ctx.AddSource("FlowthruSchemas.Manifest.g.cs", SourceText.From(sb.ToString(), Encoding.UTF8));
  }

  /// <summary>
  /// Escapes a string for inclusion in C# string literals.
  /// </summary>
  private static string EscapeString(string str)
  {
    return str.Replace("\\", "\\\\").Replace("\"", "\\\"");
  }
}

/// <summary>
/// Immutable data extracted from a schema type for code generation.
/// </summary>
internal sealed class SchemaGenerationInfo
{
  public string TypeName { get; }
  public string Namespace { get; }
  public string TypeKind { get; }
  public bool IsPartial { get; }
  public bool IsFlat { get; }
  public ImmutableArray<string> ManualInterfaces { get; }
  public Location Location { get; }
  public ImmutableArray<SchemaPropertyInfo> Properties { get; } // Phase 5: For manifest generation

  /// <summary>
  /// Per-property classification snapshot: simple type name + whether the property is
  /// definitely flat by local rules (CLR primitive / IScalar / [FlowthruColumn] on this
  /// property). Used by the emission stage to finalize <see cref="IsFlat"/> against the
  /// cross-assembly NewType registry.
  /// </summary>
  public ImmutableArray<PropertyClassification> PropertyClassifications { get; }

  public SchemaGenerationInfo(
    string typeName,
    string @namespace,
    string typeKind,
    bool isPartial,
    bool isFlat,
    ImmutableArray<string> manualInterfaces,
    Location location,
    ImmutableArray<SchemaPropertyInfo> properties,
    ImmutableArray<PropertyClassification> propertyClassifications
  )
  {
    TypeName = typeName;
    Namespace = @namespace;
    TypeKind = typeKind;
    IsPartial = isPartial;
    IsFlat = isFlat;
    ManualInterfaces = manualInterfaces;
    Location = location;
    Properties = properties;
    PropertyClassifications = propertyClassifications;
  }
}

/// <summary>
/// Snapshot of a single property's classification status, captured at extraction time so
/// the emission stage can finalize flat/nested classification once the cross-assembly
/// NewType registry is available.
/// </summary>
internal readonly record struct PropertyClassification(string SimpleTypeName, bool IsBasicallyFlat);

/// <summary>
/// Information about a schema property (Phase 5).
/// </summary>
internal sealed class SchemaPropertyInfo
{
  public string Name { get; }
  public string TypeName { get; }

  public SchemaPropertyInfo(string name, string typeName)
  {
    Name = name;
    TypeName = typeName;
  }
}
