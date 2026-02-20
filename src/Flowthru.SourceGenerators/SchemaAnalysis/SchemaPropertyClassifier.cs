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
  /// Determines whether a property type is "flat" — i.e., a primitive, enum,
  /// nullable primitive, string, or single-value value type (Guid, DateTime, etc.).
  /// </summary>
  public static bool IsFlatPropertyType(ITypeSymbol type)
  {
    // Unwrap nullable
    if (
      type is INamedTypeSymbol
      {
        OriginalDefinition.SpecialType: SpecialType.System_Nullable_T
      } nullable
    )
    {
      type = nullable.TypeArguments[0];
    }

    // Primitives and string
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

    // Enums
    if (type.TypeKind == TypeKind.Enum)
    {
      return true;
    }

    // Well-known single-value types
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

    // Arrays
    if (type is IArrayTypeSymbol)
    {
      return true;
    }

    // byte[] is treated as flat (binary blob), but we already catch that via IsFlatPropertyType
    // for named types. For arrays specifically, byte[] should be flat.
    if (
      type is IArrayTypeSymbol arrayType
      && arrayType.ElementType.SpecialType == SpecialType.System_Byte
    )
    {
      return false;
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
