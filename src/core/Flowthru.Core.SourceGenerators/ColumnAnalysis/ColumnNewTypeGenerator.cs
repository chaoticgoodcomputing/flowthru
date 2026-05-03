using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using Flowthru.Core.SourceGenerators.SchemaAnalysis;

namespace Flowthru.Core.SourceGenerators.ColumnAnalysis;

/// <summary>
/// Incremental source generator that analyzes properties annotated with [FlowthruColumn]
/// and emits corresponding NewType record structs implementing <c>IScalar</c>.
/// </summary>
/// <remarks>
/// <para>
/// The generator collects all <c>[FlowthruColumn]</c>-annotated properties across the
/// compilation, then deduplicates by <c>(namespace, type-name)</c> so a NewType referenced
/// by multiple schemas is only emitted once. If multiple uses disagree on the backing type,
/// <c>FT1004</c> is reported and no source is emitted.
/// </para>
/// <para>
/// Backing types are validated against <see cref="SchemaPropertyClassifier.IsFlatPropertyType"/>;
/// invalid backing types report <c>FT1003</c>.
/// </para>
/// </remarks>
[Generator]
public class ColumnNewTypeGenerator : IIncrementalGenerator
{
  private const string AttributeFullName = "Flowthru.Core.Abstractions.FlowthruColumnAttribute";

  /// <inheritdoc/>
  public void Initialize(IncrementalGeneratorInitializationContext context)
  {
    // Collect all properties annotated with [FlowthruColumn], including invalid ones,
    // so the output stage can report diagnostics for problems.
    var candidates = context
      .SyntaxProvider.ForAttributeWithMetadataName(
        AttributeFullName,
        predicate: static (node, _) => node is PropertyDeclarationSyntax,
        transform: static (ctx, _) => ExtractColumnInfo(ctx)
      )
      .Where(static info => info is not null)
      .Select(static (info, _) => info!);

    // Collect all candidates and deduplicate at the output stage.
    var allCandidates = candidates.Collect();
    context.RegisterSourceOutput(
      allCandidates,
      static (ctx, infos) => EmitDeduplicatedNewTypes(ctx, infos)
    );
  }

  /// <summary>
  /// Extracts column information from a property's [FlowthruColumn] attribute.
  /// Returns a <see cref="ColumnExtractionResult"/> capturing either valid information
  /// or an invalid-state marker for diagnostic reporting.
  /// </summary>
  private static ColumnExtractionResult? ExtractColumnInfo(GeneratorAttributeSyntaxContext ctx)
  {
    if (ctx.TargetSymbol is not IPropertySymbol propertySymbol)
    {
      return null;
    }

    var propertyDeclaration = (PropertyDeclarationSyntax)ctx.TargetNode;
    var containingType = propertySymbol.ContainingType;

    // Extract the property's type name from syntax (works even if the type doesn't exist yet).
    var newTypeName = propertyDeclaration.Type.ToString();

    // The NewType is emitted in the schema's containing namespace — the same convention as
    // System.Text.Json's JsonSerializable, the LoggerMessage source generator, and other
    // .NET Roslyn generators. The Catalog Developer's choice of where to place the schema
    // that declares the type (via [FlowthruColumn]) determines where the type lives;
    // downstream layers/schemas reference it via a `using` directive, like any normal type.
    var namespaceName = containingType.ContainingNamespace.IsGlobalNamespace
      ? string.Empty
      : containingType.ContainingNamespace.ToDisplayString();

    // Capture location for diagnostic reporting (file path + text span).
    var location = propertyDeclaration.Type.GetLocation();
    var locationInfo = LocationInfo.From(location);

    // Extract the backing type from the attribute's first constructor argument.
    if (ctx.Attributes.Length == 0 || ctx.Attributes[0].ConstructorArguments.Length == 0)
    {
      return null;
    }

    var backingTypeArg = ctx.Attributes[0].ConstructorArguments[0];
    if (backingTypeArg.Kind != TypedConstantKind.Type || backingTypeArg.Value is not ITypeSymbol backingType)
    {
      return null;
    }

    // Render the backing type using its full display name. For CLR primitives this comes
    // out as `int`, `string`, etc. (the language keyword); for everything else it is fully
    // qualified, which avoids ambiguity regardless of where the generated NewType lands.
    var backingTypeName = backingType.ToDisplayString();

    // Validate the backing type is a recognized scalar.
    var isValid = SchemaPropertyClassifier.IsFlatPropertyType(backingType);

    return new ColumnExtractionResult(
      NewTypeName: newTypeName,
      BackingTypeName: backingTypeName,
      Namespace: namespaceName,
      PropertyName: propertySymbol.Name,
      ContainingTypeName: containingType.Name,
      LocationInfo: locationInfo,
      IsValidBackingType: isValid
    );
  }

  /// <summary>
  /// Deduplicates collected column infos by <c>(namespace, type-name)</c> and emits
  /// either generated source or diagnostics for each unique NewType.
  /// </summary>
  private static void EmitDeduplicatedNewTypes(
    SourceProductionContext ctx,
    ImmutableArray<ColumnExtractionResult> candidates
  )
  {
    // Report FT1003 for invalid backing types — once per unique location to avoid noise.
    foreach (var invalid in candidates.Where(c => !c.IsValidBackingType))
    {
      ctx.ReportDiagnostic(
        Diagnostic.Create(
          ColumnGeneratorDiagnostics.InvalidBackingType,
          invalid.LocationInfo.ToLocation(),
          invalid.BackingTypeName
        )
      );
    }

    // Group valid candidates by (namespace, type-name) and emit once per group.
    var groups = candidates
      .Where(c => c.IsValidBackingType)
      .GroupBy(c => (c.Namespace, c.NewTypeName));

    foreach (var group in groups)
    {
      var infos = group.ToList();
      var first = infos[0];

      // Validate consistency: all uses of the same NewType must agree on the backing type.
      var distinctBackingTypes = infos.Select(c => c.BackingTypeName).Distinct().ToList();
      if (distinctBackingTypes.Count > 1)
      {
        // FT1004: inconsistent backing types — report on every conflicting site.
        var typeList = string.Join(", ", distinctBackingTypes);
        foreach (var conflict in infos)
        {
          ctx.ReportDiagnostic(
            Diagnostic.Create(
              ColumnGeneratorDiagnostics.InconsistentBackingType,
              conflict.LocationInfo.ToLocation(),
              first.NewTypeName,
              typeList
            )
          );
        }
        continue;
      }

      // Emit once for the deduplicated group.
      EmitNewTypeStruct(ctx, first);
    }
  }

  /// <summary>
  /// Emits the generated NewType record struct.
  /// </summary>
  private static void EmitNewTypeStruct(SourceProductionContext ctx, ColumnExtractionResult info)
  {
    var sb = new StringBuilder();

    sb.AppendLine("// <auto-generated/>");
    sb.AppendLine("#nullable enable");
    sb.AppendLine();
    if (!string.IsNullOrEmpty(info.Namespace))
    {
      sb.AppendLine($"namespace {info.Namespace};");
      sb.AppendLine();
    }

    sb.AppendLine("/// <summary>");
    sb.AppendLine($"/// Strong-typed wrapper around <see cref=\"{info.BackingTypeName}\"/>.");
    sb.AppendLine($"/// Generated from the <c>{info.PropertyName}</c> property of <c>{info.ContainingTypeName}</c>.");
    sb.AppendLine("/// </summary>");
    sb.AppendLine($"public readonly record struct {info.NewTypeName}({info.BackingTypeName} Value)");
    sb.AppendLine("    : global::Flowthru.Core.Abstractions.IScalar;");

    var source = SourceText.From(sb.ToString(), Encoding.UTF8);
    // Hint name includes namespace to avoid collisions when the same type name appears
    // in different namespaces.
    var hintName = string.IsNullOrEmpty(info.Namespace)
      ? $"{info.NewTypeName}.NewType.g.cs"
      : $"{info.Namespace.Replace('.', '_')}.{info.NewTypeName}.NewType.g.cs";
    ctx.AddSource(hintName, source);
  }
}

/// <summary>
/// Captured information about a single [FlowthruColumn] property. Implemented as a record
/// for value equality (required for incremental generator caching).
/// </summary>
internal sealed record ColumnExtractionResult(
  string NewTypeName,
  string BackingTypeName,
  string Namespace,
  string PropertyName,
  string ContainingTypeName,
  LocationInfo LocationInfo,
  bool IsValidBackingType
);

/// <summary>
/// Cacheable representation of a source <see cref="Location"/>. Roslyn <see cref="Location"/>
/// instances cannot be safely cached across incremental generator runs because they hold
/// references to syntax trees; this struct captures the file path and span so a fresh
/// <see cref="Location"/> can be recreated when needed for diagnostic reporting.
/// </summary>
internal readonly record struct LocationInfo(string FilePath, TextSpan TextSpan, LinePositionSpan LineSpan)
{
  public static LocationInfo From(Location location)
  {
    var span = location.SourceSpan;
    var lineSpan = location.GetLineSpan().Span;
    return new LocationInfo(location.SourceTree?.FilePath ?? string.Empty, span, lineSpan);
  }

  public Location ToLocation() => Location.Create(FilePath, TextSpan, LineSpan);
}
