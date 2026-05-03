using System.Reflection;

namespace Flowthru.Core.Data.Serialization;

/// <summary>
/// A planner-emitted snapshot of a single property's serialization profile. Format
/// extensions iterate over <see cref="PropertyMappingPlan{TRow}.Bindings"/> and inspect
/// each binding to wire format-specific encoders/decoders without re-implementing the
/// universal property-classification cascade.
/// </summary>
/// <remarks>
/// <para>
/// Bindings are produced once per <see cref="PropertyMappingPlanner.Build{TRow}"/> call
/// at catalog wire-up time. They are immutable, thread-safe, and intended to be
/// consumed via a <c>switch</c> on <see cref="Kind"/>.
/// </para>
/// <para>
/// <strong>Kind-specific accessors:</strong> <see cref="Enum"/> is populated when
/// <see cref="Kind"/> is <see cref="PropertyKind.Enum"/>; <see cref="IScalar"/> is
/// populated when <see cref="Kind"/> is <see cref="PropertyKind.IScalar"/>. For the
/// other kinds both are <see langword="null"/>.
/// </para>
/// </remarks>
public sealed class PropertyBinding
{
  /// <summary>
  /// Reflection handle for the schema property this binding describes.
  /// </summary>
  public PropertyInfo Property { get; }

  /// <summary>
  /// External field name for the property — from
  /// <see cref="Abstractions.SerializedLabelAttribute"/> if present, otherwise the
  /// property name verbatim. Used by both reader and writer paths.
  /// </summary>
  public string FieldName { get; }

  /// <summary>
  /// Structural classification of the property's effective type.
  /// </summary>
  public PropertyKind Kind { get; }

  /// <summary>
  /// Whether the property's declared type is nullable. For value types this is
  /// <c>Nullable&lt;T&gt;</c>; for reference types, the C# 8 nullability annotation.
  /// Format extensions consult this to decide whether to honor null sentinels and to
  /// configure their underlying library's null-handling.
  /// </summary>
  public bool IsNullable { get; }

  /// <summary>
  /// The non-nullable form of the property's type. For a property typed
  /// <c>int?</c> this is <c>int</c>; for <c>string?</c> this is <c>string</c>; for a
  /// non-nullable property this is identical to <see cref="PropertyInfo.PropertyType"/>.
  /// Format converters key off this to pick converter generics or reflection paths.
  /// </summary>
  public Type EffectiveType { get; }

  /// <summary>
  /// String values that should be treated as <c>null</c> on read for nullable
  /// properties. Empty when <see cref="IsNullable"/> is <see langword="false"/>.
  /// </summary>
  public IReadOnlyList<string> NullSentinels { get; }

  /// <summary>
  /// Populated when <see cref="Kind"/> is <see cref="PropertyKind.Enum"/>. Otherwise
  /// <see langword="null"/>.
  /// </summary>
  public EnumBindingInfo? Enum { get; }

  /// <summary>
  /// Populated when <see cref="Kind"/> is <see cref="PropertyKind.IScalar"/>.
  /// Otherwise <see langword="null"/>.
  /// </summary>
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
