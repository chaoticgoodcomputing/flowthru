using System.Reflection;

namespace Flowthru.Data.Schema.Mapping;

/// <summary>
/// Planner-emitted snapshot of a single property's serialization profile.
/// Format extensions iterate over <see cref="PropertyMappingPlan{TRow}.Bindings"/>
/// and dispatch on <see cref="Kind"/> to wire format-specific
/// encoders/decoders without re-implementing the universal
/// property-classification cascade.
/// </summary>
/// <remarks>
/// Bindings are produced once per
/// <see cref="PropertyMappingPlanner.Build{TRow}()"/> call and are
/// immutable, thread-safe, and intended to be consumed via a
/// <c>switch</c> on <see cref="Kind"/>. <see cref="Enum"/> is populated
/// when <see cref="Kind"/> is <see cref="PropertyKind.Enum"/>;
/// <see cref="IScalar"/> is populated when <see cref="Kind"/> is
/// <see cref="PropertyKind.IScalar"/>. Both are <c>null</c> otherwise.
/// </remarks>
public sealed class PropertyBinding
{
  /// <summary>Reflection handle for the schema property this binding describes.</summary>
  public PropertyInfo Property { get; }

  /// <summary>
  /// External field name — from <see cref="SerializedLabelAttribute.Label"/> if
  /// present, otherwise the property name verbatim.
  /// </summary>
  public string FieldName { get; }

  /// <summary>Structural classification of the property's effective type.</summary>
  public PropertyKind Kind { get; }

  /// <summary>True if the declared property type is nullable.</summary>
  public bool IsNullable { get; }

  /// <summary>
  /// Non-nullable form of the property's type. For a property typed
  /// <c>int?</c> this is <c>int</c>; for <c>string?</c> this is
  /// <c>string</c>; for non-nullable properties this is identical to
  /// <see cref="PropertyInfo.PropertyType"/>.
  /// </summary>
  public Type EffectiveType { get; }

  /// <summary>
  /// String values that should be treated as <c>null</c> on read for
  /// nullable properties. Empty when <see cref="IsNullable"/> is false.
  /// </summary>
  public IReadOnlyList<string> NullSentinels { get; }

  /// <summary>Populated when <see cref="Kind"/> is <see cref="PropertyKind.Enum"/>.</summary>
  public EnumBindingInfo? Enum { get; }

  /// <summary>Populated when <see cref="Kind"/> is <see cref="PropertyKind.IScalar"/>.</summary>
  public IScalarBindingInfo? IScalar { get; }

  internal PropertyBinding(
    PropertyInfo property,
    string fieldName,
    PropertyKind kind,
    bool isNullable,
    Type effectiveType,
    IReadOnlyList<string> nullSentinels,
    EnumBindingInfo? @enum,
    IScalarBindingInfo? iScalar
  )
  {
    Property = property;
    FieldName = fieldName;
    Kind = kind;
    IsNullable = isNullable;
    EffectiveType = effectiveType;
    NullSentinels = nullSentinels;
    Enum = @enum;
    IScalar = iScalar;
  }
}
