using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace Flowthru.SourceGenerators.SchemaAnalysis;

/// <summary>
/// Classifies properties of a schema type to determine structural characteristics
/// (flat vs nested) and derive serialization compatibility.
/// </summary>
internal static class SchemaPropertyClassifier
{
  /// <summary>
  /// Determines whether a property type is a flat scalar — i.e., it serializes to a single
  /// JSON value (<c>"key": value</c>), not an object (<c>"key": {...}</c>) or array
  /// (<c>"key": [...]</c>).
  /// </summary>
  /// <remarks>
  /// Classification proceeds through five explicit tiers, each with a distinct rationale:
  /// <list type="number">
  /// <item>CLR SpecialType primitives — compiler-known scalars, no name matching needed.</item>
  /// <item>Enums — always single-value regardless of underlying type.</item>
  /// <item><c>byte[]</c> — structural exception: an array, but semantically an opaque blob
  ///   (e.g. a binary image column), not a traversable collection.</item>
  /// <item>Known BCL scalar structs (<c>Guid</c>, <c>TimeSpan</c>, etc.) — cannot self-declare
  ///   <see cref="IFlatScalar"/> because they are defined outside this library.</item>
  /// <item><see cref="IFlatScalar"/> implementors — user-defined NewTypes and value-object
  ///   wrappers that explicitly opt in to scalar treatment.</item>
  /// </list>
  /// If a type does not match any tier it is treated as a nested object.
  /// </remarks>
  public static bool IsFlatPropertyType(ITypeSymbol type)
  {
    // Unwrap nullable — the nullability wrapper does not change the scalar/nested nature
    // of the underlying type.
    if (
      type is INamedTypeSymbol
      {
        OriginalDefinition.SpecialType: SpecialType.System_Nullable_T
      } nullable
    )
    {
      type = nullable.TypeArguments[0];
    }

    // Tier 1: CLR SpecialType primitives
    // These are compiler-known and do not require name matching.
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

    // Tier 2: Enums
    // Always single-value regardless of underlying type.
    if (type.TypeKind == TypeKind.Enum)
    {
      return true;
    }

    // Tier 3: byte[]
    // Structurally an array, but semantically an opaque binary blob (e.g. an image column).
    // Treated as a single value, not a traversable collection.
    if (type is IArrayTypeSymbol { ElementType.SpecialType: SpecialType.System_Byte })
    {
      return true;
    }

    // Tier 4: Known BCL scalar structs
    // These cannot self-declare IFlatScalar because they are defined outside this library.
    // This list is intentionally bounded — it covers BCL types only, not user-defined types.
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

    // Tier 5: IFlatScalar implementors
    // User-defined NewTypes and value-object wrappers that explicitly declare they serialize
    // to a single value. This is the extension point for domain primitives.
    const string FlatScalarInterface = "Flowthru.Abstractions.IFlatScalar";
    if (
      type is INamedTypeSymbol namedType
      && namedType.AllInterfaces.Any(i => i.ToDisplayString() == FlatScalarInterface)
    )
    {
      return true;
    }

    return false;
  }

  /// <summary>
  /// Determines whether a property type is a collection (array, IEnumerable&lt;T&gt;, List, etc.),
  /// dictionary, or nested complex object.
  /// </summary>
  public static bool IsNestedPropertyType(ITypeSymbol type)
  {
    // Unwrap nullable — a Nullable<CustomStruct> is still nested if CustomStruct is nested
    if (
      type is INamedTypeSymbol
      {
        OriginalDefinition.SpecialType: SpecialType.System_Nullable_T
      } nullable
    )
    {
      type = nullable.TypeArguments[0];
    }

    // If it's flat, it's not nested
    if (IsFlatPropertyType(type))
    {
      return false;
    }

    // Arrays — byte[] was already handled as a flat blob by IsFlatPropertyType (Tier 3),
    // so any array reaching here is a traversable collection and is therefore nested.
    if (type is IArrayTypeSymbol)
    {
      return true;
    }

    // Everything else that isn't flat is nested: collections, dictionaries,
    // custom classes/records/structs, interfaces, etc.
    return true;
  }

  /// <summary>
  /// Analyzes all public instance properties of a type and determines if the schema is flat.
  /// A schema is flat if ALL of its properties are flat types.
  /// </summary>
  public static SchemaClassification Classify(INamedTypeSymbol typeSymbol)
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

    // A schema with no properties is considered flat (vacuously)
    if (properties.Count == 0)
    {
      return new SchemaClassification(isFlat: true, properties);
    }

    var isFlat = properties.All(p => IsFlatPropertyType(p.Type));
    return new SchemaClassification(isFlat, properties);
  }
}

/// <summary>
/// Result of classifying a schema's property structure.
/// </summary>
internal sealed class SchemaClassification
{
  public bool IsFlat { get; }
  public bool IsNested => !IsFlat;
  public IList<IPropertySymbol> Properties { get; }

  public SchemaClassification(bool isFlat, IList<IPropertySymbol> properties)
  {
    IsFlat = isFlat;
    Properties = properties;
  }
}
