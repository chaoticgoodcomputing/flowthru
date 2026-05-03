using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace Flowthru.Core.SourceGenerators.SchemaAnalysis;

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
  ///   <c>IScalar</c> because they are defined outside this library.</item>
  /// <item><c>IScalar</c> implementors — user-defined NewTypes and value-object
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
    // These cannot self-declare IScalar because they are defined outside this library.
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

    // Tier 5: IScalar implementors
    // User-defined NewTypes and value-object wrappers that explicitly declare they serialize
    // to a single value. This is the extension point for domain primitives.
    const string FlatScalarInterface = "Flowthru.Core.Abstractions.IScalar";
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
  /// Analyzes all public instance properties of a type and determines if the schema is flat.
  /// A schema is flat if ALL of its properties are flat types.
  /// </summary>
  /// <param name="typeSymbol">The schema type to classify.</param>
  /// <param name="knownNewTypeNames">
  /// Optional set of simple type names that the <c>[FlowthruColumn]</c> generator will emit
  /// as <c>IScalar</c> NewTypes. Properties whose type matches one of these names by simple
  /// name are treated as flat — this makes "schema USES NewType, declared elsewhere" cases
  /// classify correctly even though the generated NewType is invisible to this generator's
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

    // A schema with no properties is considered flat (vacuously)
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

  /// <summary>
  /// Determines whether a property is annotated with <c>[FlowthruColumn]</c>.
  /// If so, the property will generate a flat scalar NewType and should be treated as flat
  /// even before the NewType is generated.
  /// </summary>
  private static bool HasFlowthruColumnAttribute(IPropertySymbol property)
  {
    const string ColumnAttribute = "Flowthru.Core.Abstractions.FlowthruColumnAttribute";
    return property.GetAttributes()
      .Any(a => a.AttributeClass?.ToDisplayString() == ColumnAttribute);
  }

  /// <summary>
  /// Determines whether a property's type matches a NewType declared elsewhere in the
  /// compilation via <c>[FlowthruColumn]</c>. The match is by simple name, since the
  /// generated NewType may not yet exist in the input compilation.
  /// </summary>
  /// <remarks>
  /// <para>
  /// Both resolved and unresolved property types expose <see cref="ISymbol.Name"/> as the
  /// identifier the developer wrote — for an unresolved type that is still the simple name,
  /// for a resolved type it is the type's name.
  /// </para>
  /// <para>
  /// Nullable wrappers are unwrapped first so a property typed <c>ShuttleId?</c> matches
  /// the registered <c>ShuttleId</c>.
  /// </para>
  /// </remarks>
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

/// <summary>
/// Result of classifying a schema's property structure.
/// </summary>
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
