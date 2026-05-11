using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using Flowthru.Data.Schema;
using Flowthru.Data.Schema.Mapping;

namespace Flowthru.Data.Storage;

/// <summary>
/// Factory that produces a <see cref="SerializedEnumJsonConverter{T}"/>
/// per enum type. Wired into <see cref="JsonFormatSerializer{TRow}"/>'s
/// default options so any enum property in a schema serializes via its
/// <see cref="SerializedEnumAttribute"/>-declared string instead of the
/// CLR member name or ordinal.
/// </summary>
/// <remarks>
/// <para>
/// This is the JSON side of Flowthru's <strong>format-agnostic enum
/// contract</strong>. Every format extension (CSV, Parquet, Excel,
/// EFCore, …) reads off the same
/// <see cref="EnumBindingInfo"/> populated by
/// <see cref="SerializedEnumMappings.Build"/>. Extensions that opt into
/// the enum contract use their format's native converter hook to
/// translate at the field boundary — the rule is "one declared mapping,
/// every format honors it identically".
/// </para>
/// <para>
/// The factory accepts every <see cref="System.Enum"/>; the per-type
/// converter calls <see cref="SerializedEnumMappings.Build"/> at
/// construction, so an enum missing
/// <see cref="SerializedEnumAttribute"/> on any member fails fast (at
/// adapter construction / first use) rather than silently round-tripping
/// to an ordinal. This is consistent with the planner's behavior for
/// schema-bound enums.
/// </para>
/// </remarks>
public sealed class SerializedEnumJsonConverterFactory : JsonConverterFactory
{
  /// <inheritdoc/>
  public override bool CanConvert(System.Type typeToConvert) =>
    typeToConvert.IsEnum;

  /// <inheritdoc/>
  public override JsonConverter CreateConverter(
    System.Type typeToConvert,
    JsonSerializerOptions options
  )
  {
    var converterType = typeof(SerializedEnumJsonConverter<>).MakeGenericType(typeToConvert);
    return (JsonConverter)System.Activator.CreateInstance(converterType)!;
  }
}

/// <summary>
/// Per-type JSON converter that maps <typeparamref name="TEnum"/> values
/// to their <see cref="SerializedEnumAttribute"/>-declared strings using
/// the bidirectional mapping from <see cref="SerializedEnumMappings.Build"/>.
/// </summary>
/// <typeparam name="TEnum">The enum type — every member must carry
/// <see cref="SerializedEnumAttribute"/>; missing or duplicate
/// annotations throw at converter construction.</typeparam>
public sealed class SerializedEnumJsonConverter<TEnum> : JsonConverter<TEnum>
  where TEnum : struct, System.Enum
{
  private readonly IReadOnlyDictionary<object, string> _forward;
  private readonly IReadOnlyDictionary<string, object> _reverse;

  /// <summary>
  /// Builds the bidirectional mapping for <typeparamref name="TEnum"/>
  /// at construction time. Throws via
  /// <see cref="SerializedEnumMappings.Build"/> if any member is missing
  /// <see cref="SerializedEnumAttribute"/> or duplicates a serialized
  /// value — surfacing the schema bug deterministically at adapter
  /// wire-up rather than at first-row deserialization.
  /// </summary>
  public SerializedEnumJsonConverter()
  {
    var (forward, reverse) = SerializedEnumMappings.Build(typeof(TEnum));
    _forward = forward;
    _reverse = reverse;
  }

  /// <inheritdoc/>
  public override TEnum Read(
    ref Utf8JsonReader reader,
    System.Type typeToConvert,
    JsonSerializerOptions options
  )
  {
    if (reader.TokenType != JsonTokenType.String)
    {
      throw new JsonException(
        $"Expected JSON string for enum '{typeof(TEnum).Name}', got {reader.TokenType}."
      );
    }
    var raw = reader.GetString();
    if (raw is null || !_reverse.TryGetValue(raw, out var boxed))
    {
      throw new JsonException(
        $"'{raw}' is not a declared serialized value for enum '{typeof(TEnum).Name}'. "
          + $"Known values: {string.Join(", ", _reverse.Keys)}."
      );
    }
    return (TEnum)boxed;
  }

  /// <inheritdoc/>
  public override void Write(
    Utf8JsonWriter writer,
    TEnum value,
    JsonSerializerOptions options
  )
  {
    if (!_forward.TryGetValue(value, out var serialized))
    {
      throw new JsonException(
        $"Enum value '{value}' on '{typeof(TEnum).Name}' has no declared "
          + "[SerializedEnum] mapping. Cast-from-int values outside the enum's "
          + "declared range cannot be serialized."
      );
    }
    writer.WriteStringValue(serialized);
  }

  /// <inheritdoc/>
  public override void WriteAsPropertyName(
    Utf8JsonWriter writer,
    TEnum value,
    JsonSerializerOptions options
  )
  {
    if (!_forward.TryGetValue(value, out var serialized))
    {
      throw new JsonException(
        $"Enum value '{value}' on '{typeof(TEnum).Name}' has no declared "
          + "[SerializedEnum] mapping."
      );
    }
    writer.WritePropertyName(serialized);
  }

  /// <inheritdoc/>
  public override TEnum ReadAsPropertyName(
    ref Utf8JsonReader reader,
    System.Type typeToConvert,
    JsonSerializerOptions options
  )
  {
    var raw = reader.GetString();
    if (raw is null || !_reverse.TryGetValue(raw, out var boxed))
    {
      throw new JsonException(
        $"Property-name '{raw}' is not a declared serialized value for enum '{typeof(TEnum).Name}'."
      );
    }
    return (TEnum)boxed;
  }
}
