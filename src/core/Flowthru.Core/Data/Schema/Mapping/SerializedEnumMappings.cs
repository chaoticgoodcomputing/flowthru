using System.Collections.Frozen;
using System.Reflection;

namespace Flowthru.Data.Schema.Mapping;

/// <summary>
/// Builds the bidirectional <c>enum value ↔ serialized string</c>
/// mapping for an enum type by reflecting over its members'
/// <see cref="SerializedEnumAttribute"/> annotations. Used internally
/// by <see cref="PropertyMappingPlanner"/> to populate
/// <see cref="EnumBindingInfo.Forward"/> and
/// <see cref="EnumBindingInfo.Reverse"/>.
/// </summary>
/// <remarks>
/// <para>
/// <strong>Format-agnostic — single source of truth.</strong>
/// Every format extension that supports enums (CSV, Excel, Parquet,
/// XML) consumes these mappings off the binding rather than
/// reimplementing the reflection scan, so a missing
/// <see cref="SerializedEnumAttribute"/> or a duplicate serialized
/// value surfaces consistently across formats.
/// </para>
/// <para>
/// Failure modes raise <see cref="InvalidOperationException"/> at
/// planner-build time — i.e. when a format serializer is constructed
/// against the schema. This places the failure deterministically at
/// catalog wire-up rather than at first-row deserialization.
/// </para>
/// </remarks>
public static class SerializedEnumMappings
{
  /// <summary>
  /// Build the forward / reverse mappings for <paramref name="enumType"/>.
  /// </summary>
  /// <exception cref="ArgumentException">
  /// Thrown when <paramref name="enumType"/> is not an enum type.
  /// </exception>
  /// <exception cref="InvalidOperationException">
  /// Thrown when an enum member is missing
  /// <see cref="SerializedEnumAttribute"/>, or when two enum members
  /// declare the same serialized value.
  /// </exception>
  public static (
    IReadOnlyDictionary<object, string> Forward,
    IReadOnlyDictionary<string, object> Reverse
  ) Build(Type enumType)
  {
    if (enumType is null)
    {
      throw new ArgumentNullException(nameof(enumType));
    }
    if (!enumType.IsEnum)
    {
      throw new ArgumentException(
        $"Type '{enumType.FullName}' is not an enum.",
        nameof(enumType)
      );
    }

    var fields = enumType.GetFields(BindingFlags.Public | BindingFlags.Static);
    var forward = new Dictionary<object, string>(fields.Length);
    var reverse = new Dictionary<string, object>(fields.Length, StringComparer.Ordinal);

    foreach (var field in fields)
    {
      var enumValue = field.GetValue(null)!;
      var attribute = field.GetCustomAttribute<SerializedEnumAttribute>();
      if (attribute is null)
      {
        throw new InvalidOperationException(
          $"Enum member '{enumType.Name}.{field.Name}' is missing the required "
          + "[SerializedEnum] attribute. Every enum member used in a Flowthru schema "
          + "must declare an explicit serialized value."
        );
      }

      var serialized = attribute.Value;
      if (reverse.TryGetValue(serialized, out var existing))
      {
        throw new InvalidOperationException(
          $"Duplicate serialized value '{serialized}' for enum '{enumType.Name}': both "
          + $"'{field.Name}' and '{existing}' map to it."
        );
      }

      forward[enumValue] = serialized;
      reverse[serialized] = enumValue;
    }

    return (
      forward.ToFrozenDictionary(),
      reverse.ToFrozenDictionary(StringComparer.Ordinal)
    );
  }
}
