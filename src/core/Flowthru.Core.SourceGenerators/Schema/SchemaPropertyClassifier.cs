using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace Flowthru.Core.SourceGenerators.Schema;

/// <summary>
/// Classifies properties of a schema type to determine structural
/// characteristics (flat vs nested) under Flowthru's universal Tier 1–5
/// cascade. Used by the schema interface generator to derive the
/// appropriate marker interfaces.
/// </summary>
internal static class SchemaPropertyClassifier
{
  private const string IScalarFullName = "Flowthru.Data.Schema.IScalar";
  private const string ColumnAttributeFullName =
    "Flowthru.Data.Schema.FlowthruColumnAttribute";

  /// <summary>
  /// True if the property type is a flat scalar — serializes to a single
  /// JSON value, not an object or array. The cascade:
  /// <list type="number">
  /// <item>CLR SpecialType primitives — compiler-known scalars.</item>
  /// <item>Enums — always single-value regardless of underlying type.</item>
  /// <item><c>byte[]</c> — opaque blob, not a traversable collection.</item>
  /// <item>Known BCL scalar structs (Guid, TimeSpan, DateTimeOffset, …).</item>
  /// <item><c>IScalar</c> implementors — user-defined NewType wrappers.</item>
  /// </list>
  /// Anything else is treated as nested.
  /// </summary>
  public static bool IsFlatPropertyType(ITypeSymbol type)
  {
    // Unwrap Nullable<T>; nullability does not change scalar/nested.
    if (
      type is INamedTypeSymbol
      {
        OriginalDefinition.SpecialType: SpecialType.System_Nullable_T
      } nullable
    )
    {
      type = nullable.TypeArguments[0];
    }

    // Tier 1: CLR SpecialType primitives.
    switch (type.SpecialType)
    {
      case SpecialType.System_Boolean:
      case SpecialType.System_Byte:
      case SpecialType.System_SByte:
      case SpecialType.System_Int16:
      case SpecialType.System_UInt16:
      case SpecialType.System_Int32:
      case SpecialType.System_UInt32:
      case SpecialType.System_Int64:
      case SpecialType.System_UInt64:
      case SpecialType.System_Single:
      case SpecialType.System_Double:
      case SpecialType.System_Decimal:
      case SpecialType.System_Char:
      case SpecialType.System_String:
      case SpecialType.System_DateTime:
        return true;
    }

    // Tier 2: Enums.
    if (type.TypeKind == TypeKind.Enum)
    {
      return true;
    }

    // Tier 3: byte[] as opaque blob.
    if (type is IArrayTypeSymbol { ElementType.SpecialType: SpecialType.System_Byte })
    {
      return true;
    }

    // Tier 4: Known BCL scalar structs.
    var fullName = type.ToDisplayString();
    switch (fullName)
    {
      case "System.Guid":
      case "System.TimeSpan":
      case "System.DateTimeOffset":
      case "System.DateOnly":
      case "System.TimeOnly":
      case "System.Half":
      case "System.Int128":
      case "System.UInt128":
        return true;
    }

    // Tier 5: IScalar implementors.
    if (
      type is INamedTypeSymbol named
      && named.AllInterfaces.Any(i => i.ToDisplayString() == IScalarFullName)
    )
    {
      return true;
    }

    return false;
  }

  /// <summary>
  /// Classifies a schema type's public instance properties and decides
  /// flat vs nested overall. A schema is flat if every property is flat.
  /// </summary>
  /// <param name="typeSymbol">The schema type to classify.</param>
  /// <param name="knownNewTypeNames">
  /// Optional set of simple type names that <c>[FlowthruColumn]</c>
  /// elsewhere in the compilation will emit as <c>IScalar</c> NewTypes.
  /// Properties whose type's simple name matches are treated as flat —
  /// this admits "schema USES NewType, declared elsewhere" cases without
  /// requiring the generated NewType to be visible in this generator's
  /// input compilation.
  /// </param>
  public static SchemaClassification Classify(
    INamedTypeSymbol typeSymbol,
    ImmutableHashSet<string>? knownNewTypeNames = null
  )
  {
    var properties = typeSymbol
      .GetMembers()
      .OfType<IPropertySymbol>()
      .Where(p =>
        !p.IsStatic
        && !p.IsIndexer
        && p.DeclaredAccessibility == Accessibility.Public
        && p.GetMethod != null
      )
      .ToList();

    // Empty schemas are vacuously flat.
    if (properties.Count == 0)
    {
      return new SchemaClassification(isFlat: true, properties);
    }

    var isFlat = properties.All(p =>
      IsFlatPropertyType(p.Type)
      || HasFlowthruColumnAttribute(p)
      || IsRegisteredNewTypeReference(p, knownNewTypeNames)
    );
    return new SchemaClassification(isFlat, properties);
  }

  private static bool HasFlowthruColumnAttribute(IPropertySymbol property) =>
    property.GetAttributes()
      .Any(a => a.AttributeClass?.ToDisplayString() == ColumnAttributeFullName);

  private static bool IsRegisteredNewTypeReference(
    IPropertySymbol property,
    ImmutableHashSet<string>? knownNewTypeNames
  )
  {
    if (knownNewTypeNames is null || knownNewTypeNames.IsEmpty)
    {
      return false;
    }

    var type = property.Type;
    if (
      type is INamedTypeSymbol
      {
        OriginalDefinition.SpecialType: SpecialType.System_Nullable_T
      } nullable
    )
    {
      type = nullable.TypeArguments[0];
    }

    return knownNewTypeNames.Contains(type.Name);
  }
}

/// <summary>Result of classifying a schema's property structure.</summary>
internal sealed class SchemaClassification
{
  public bool IsFlat { get; }
  public IList<IPropertySymbol> Properties { get; }

  public SchemaClassification(bool isFlat, IList<IPropertySymbol> properties)
  {
    IsFlat = isFlat;
    Properties = properties;
  }
}
