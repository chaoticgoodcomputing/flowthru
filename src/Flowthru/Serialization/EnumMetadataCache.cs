using System.Collections.Frozen;
using System.Reflection;
using Flowthru.Abstractions;

namespace Flowthru.Serialization;

/// <summary>
/// Provides cached, high-performance bidirectional mapping between enum values and their
/// serialized string representations as defined by <see cref="SerializedEnumAttribute"/>.
/// </summary>
/// <typeparam name="TEnum">The enum type to create metadata for.</typeparam>
/// <remarks>
/// <para>
/// This class performs reflection once during construction to build frozen dictionaries
/// for O(1) lookups during serialization and deserialization. All metadata is immutable
/// and thread-safe.
/// </para>
/// <para>
/// Instances are typically created and cached by <see cref="EnumMetadataRegistry"/> rather
/// than instantiated directly.
/// </para>
/// </remarks>
internal sealed class EnumMetadataCache<TEnum>
  where TEnum : struct, Enum
{
  /// <summary>
  /// Maps enum values to their serialized string representations.
  /// </summary>
  private readonly FrozenDictionary<TEnum, string> _enumToString;

  /// <summary>
  /// Maps serialized string values back to enum values.
  /// </summary>
  private readonly FrozenDictionary<string, TEnum> _stringToEnum;

  /// <summary>
  /// Gets the enum type that this cache represents.
  /// </summary>
  public Type EnumType { get; }

  /// <summary>
  /// Initializes a new instance of the <see cref="EnumMetadataCache{TEnum}"/> class.
  /// </summary>
  /// <remarks>
  /// Performs reflection to discover all enum fields and their <see cref="SerializedEnumAttribute"/>
  /// annotations. Validation ensures all enum members have the attribute applied.
  /// </remarks>
  /// <exception cref="InvalidOperationException">
  /// Thrown when an enum member is missing <see cref="SerializedEnumAttribute"/> or when
  /// duplicate serialized values are detected.
  /// </exception>
  public EnumMetadataCache()
  {
    EnumType = typeof(TEnum);

    FieldInfo[] fields = EnumType.GetFields(BindingFlags.Public | BindingFlags.Static);

    var enumToString = new Dictionary<TEnum, string>(fields.Length);
    var stringToEnum = new Dictionary<string, TEnum>(fields.Length, StringComparer.Ordinal);

    foreach (FieldInfo field in fields)
    {
      // Get the enum value
      TEnum enumValue = (TEnum)field.GetValue(null)!;

      // Look for SerializedEnumAttribute
      SerializedEnumAttribute? attribute = field.GetCustomAttribute<SerializedEnumAttribute>();

      if (attribute == null)
      {
        throw new InvalidOperationException(
          $"Enum member '{EnumType.Name}.{field.Name}' is missing the required "
            + $"[SerializedEnum] attribute. All enum members used in Flowthru schemas must "
            + $"have explicit serialization mappings defined."
        );
      }

      string serializedValue = attribute.Value;

      // Check for duplicate serialized values
      if (stringToEnum.ContainsKey(serializedValue))
      {
        TEnum existingEnumValue = stringToEnum[serializedValue];
        throw new InvalidOperationException(
          $"Duplicate serialized enum value '{serializedValue}' found in enum '{EnumType.Name}'. "
            + $"Both '{field.Name}' and '{existingEnumValue}' map to the same serialized value. "
            + $"Each enum member must have a unique serialized value."
        );
      }

      // Add to both dictionaries
      enumToString[enumValue] = serializedValue;
      stringToEnum[serializedValue] = enumValue;
    }

    // Freeze dictionaries for optimal read performance
    _enumToString = enumToString.ToFrozenDictionary();
    _stringToEnum = stringToEnum.ToFrozenDictionary(StringComparer.Ordinal);
  }

  /// <summary>
  /// Converts an enum value to its serialized string representation.
  /// </summary>
  /// <param name="value">The enum value to convert.</param>
  /// <returns>The serialized string value.</returns>
  /// <exception cref="InvalidOperationException">
  /// Thrown when the enum value is not defined or lacks a <see cref="SerializedEnumAttribute"/>.
  /// </exception>
  public string ToString(TEnum value)
  {
    if (_enumToString.TryGetValue(value, out string? result))
    {
      return result;
    }

    throw new InvalidOperationException(
      $"Enum value '{value}' of type '{EnumType.Name}' is not defined or lacks a "
        + $"[SerializedEnum] attribute."
    );
  }

  /// <summary>
  /// Attempts to convert an enum value to its serialized string representation.
  /// </summary>
  /// <param name="value">The enum value to convert.</param>
  /// <param name="result">
  /// When this method returns, contains the serialized string value if the conversion succeeded,
  /// or null if the conversion failed.
  /// </param>
  /// <returns>true if the conversion succeeded; otherwise, false.</returns>
  public bool TryToString(TEnum value, out string? result)
  {
    return _enumToString.TryGetValue(value, out result);
  }

  /// <summary>
  /// Converts a serialized string value to its corresponding enum value.
  /// </summary>
  /// <param name="value">The serialized string value to convert.</param>
  /// <returns>The corresponding enum value.</returns>
  /// <exception cref="InvalidOperationException">
  /// Thrown when the serialized value does not match any enum member.
  /// </exception>
  public TEnum Parse(string value)
  {
    if (_stringToEnum.TryGetValue(value, out TEnum result))
    {
      return result;
    }

    throw new InvalidOperationException(
      $"Serialized value '{value}' is not a valid value for enum '{EnumType.Name}'. "
        + $"Valid values are: {string.Join(", ", _stringToEnum.Keys.Select(k => $"'{k}'"))}."
    );
  }

  /// <summary>
  /// Attempts to convert a serialized string value to its corresponding enum value.
  /// </summary>
  /// <param name="value">The serialized string value to convert.</param>
  /// <param name="result">
  /// When this method returns, contains the enum value if the conversion succeeded,
  /// or the default value if the conversion failed.
  /// </param>
  /// <returns>true if the conversion succeeded; otherwise, false.</returns>
  public bool TryParse(string value, out TEnum result)
  {
    return _stringToEnum.TryGetValue(value, out result);
  }

  /// <summary>
  /// Gets all defined enum values.
  /// </summary>
  /// <returns>An enumerable of all enum values that have serialization mappings.</returns>
  public IEnumerable<TEnum> GetValues()
  {
    return _enumToString.Keys;
  }

  /// <summary>
  /// Gets all serialized string values.
  /// </summary>
  /// <returns>An enumerable of all serialized string values.</returns>
  public IEnumerable<string> GetSerializedValues()
  {
    return _stringToEnum.Keys;
  }
}
