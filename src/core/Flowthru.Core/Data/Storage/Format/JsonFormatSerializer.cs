using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Serialization;
using Flowthru.Core.Abstractions;
using Flowthru.Core.Data.Capabilities;
using Flowthru.Core.Data.Serialization;
using Flowthru.Core.Serialization;

namespace Flowthru.Core.Data.Storage.Format;

/// <summary>
/// Format serializer for JSON (JavaScript Object Notation) files.
/// </summary>
/// <typeparam name="TRow">The row schema type</typeparam>
/// <remarks>
/// <para>
/// <strong>Type Constraints:</strong>
/// </para>
/// <para>
/// TRow must implement <see cref="IStructuredSerializable"/>, which supports both:
/// </para>
/// <list type="bullet">
/// <item><see cref="IFlatSchema"/> - Simple flat structures</item>
/// <item><see cref="INestedSchema"/> - Complex nested structures</item>
/// </list>
/// <para>
/// JSON is flexible and can handle any schema structure, making it suitable for:
/// - Configuration objects
/// - Model metadata and metrics
/// - Nested result structures
/// - Human-readable data files
/// </para>
/// <para>
/// <strong>Configuration:</strong>
/// </para>
/// <para>
/// Uses System.Text.Json with default configuration:
/// - WriteIndented = true (pretty printing)
/// - PropertyNamingPolicy = CamelCase
/// - DefaultIgnoreCondition = WhenWritingNull
/// </para>
/// <para>
/// Custom JsonSerializerOptions can be provided via constructor.
/// </para>
/// <para>
/// <strong>Streaming Behavior:</strong>
/// </para>
/// <para>
/// JSON serialization streams rows as a JSON array:
/// </para>
/// <code>
/// [
///   { "id": 1, "name": "Item 1" },
///   { "id": 2, "name": "Item 2" }
/// ]
/// </code>
/// <para>
/// Both deserialization and serialization are streaming, yielding/consuming
/// rows lazily for memory efficiency.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Flat schema
/// public record MetricsSchema(
///     double Accuracy,
///     double Precision,
///     double Recall
/// ) : IFlatSchema, IStructuredSerializable;
///
/// // Nested schema
/// public record ResultsSchema(
///     List&lt;MetricsSchema&gt; FoldMetrics,
///     double MeanAccuracy
/// ) : INestedSchema, IStructuredSerializable;
///
/// var serializer = new JsonFormatSerializer&lt;ResultsSchema&gt;();
///
/// // Serialize
/// var results = new[] {
///     new ResultsSchema(new List&lt;MetricsSchema&gt; { /* ... */ }, 0.95)
/// };
///
/// using var writeStream = File.Create("results.json");
/// await serializer.SerializeRows(writeStream, results.ToAsyncEnumerable());
/// </code>
/// </example>
public sealed class JsonFormatSerializer<TRow> : IFormatSerializer<TRow>
  where TRow : notnull, IStructuredSerializable
{
  private readonly JsonSerializerOptions _options;

  /// <summary>
  /// Creates a new JSON format serializer with default configuration.
  /// </summary>
  /// <remarks>
  /// <para>
  /// <strong>Property Naming:</strong> No default naming policy is applied.
  /// Use <see cref="SerializedLabelAttribute"/> to specify property names explicitly.
  /// If no SerializedLabel is present, the C# property name is used as-is.
  /// </para>
  /// </remarks>
  public JsonFormatSerializer()
    : this(
      new JsonSerializerOptions
      {
        WriteIndented = true,
        PropertyNamingPolicy = null, // No automatic naming transformation
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
      }
    ) { }

  /// <summary>
  /// Creates a new JSON format serializer with custom options.
  /// </summary>
  /// <param name="options">JSON serialization options</param>
  /// <exception cref="ArgumentNullException">Thrown if options is null</exception>
  public JsonFormatSerializer(JsonSerializerOptions options)
  {
    _options = options ?? throw new ArgumentNullException(nameof(options));

    // Add SerializedEnum-aware converter (must be first for enum types)
    _options.Converters.Add(new SerializedEnumJsonConverterFactory());

    // Add SerializedLabel-aware converter
    _options.Converters.Add(new SerializedLabelJsonConverterFactory());
  }

  /// <summary>
  /// Gets the JSON serialization options for this serializer.
  /// </summary>
  public JsonSerializerOptions Options => _options;

  /// <inheritdoc/>
  /// <remarks>
  /// JSON format requires buffering all rows before serialization (array format),
  /// so CanStream is false.
  /// </remarks>
  public StorageTraits Traits => new StorageTraits();

  /// <inheritdoc/>
  /// <remarks>
  /// JSON consumes the planner-driven <c>SerializedLabelJsonConverter&lt;T&gt;</c>
  /// (Phase B4) which dispatches on <see cref="Serialization.PropertyKind"/> to handle
  /// IScalar wrap/unwrap. Nested rows are JSON's natural shape — supported via
  /// System.Text.Json's recursive converter resolution.
  /// </remarks>
  public FormatRowFeatures RowFeatures => new()
  {
    SupportsIScalar = true,
    SupportsNested = true,
  };

  /// <inheritdoc/>
  public async IAsyncEnumerable<TRow> DeserializeRows(Stream stream)
  {
    if (stream == null)
    {
      throw new ArgumentNullException(nameof(stream));
    }

    // Stream array elements one at a time so early-break consumers (e.g. shallow inspection)
    // do not need to deserialize the entire file into memory.
    await foreach (var item in JsonSerializer.DeserializeAsyncEnumerable<TRow>(stream, _options))
    {
      if (item is null)
      {
        continue;
      }

      yield return item;
    }
  }

  /// <inheritdoc/>
  public async Task SerializeRows(Stream stream, IAsyncEnumerable<TRow> rows)
  {
    if (stream == null)
    {
      throw new ArgumentNullException(nameof(stream));
    }

    if (rows == null)
    {
      throw new ArgumentNullException(nameof(rows));
    }

    // Collect all rows first (JSON array requires full content)
    var rowList = new List<TRow>();
    await foreach (var row in rows)
    {
      rowList.Add(row);
    }

    // Serialize as JSON array
    await JsonSerializer.SerializeAsync(stream, rowList, _options);
  }

  /// <inheritdoc/>
  public PropertyMappingConfiguration GetPropertyMappingConfiguration()
  {
    return PropertyMappingConfiguration.FromSerializedLabel<TRow>();
  }
}

/// <summary>
/// JSON converter factory that creates converters respecting SerializedLabel attributes.
/// </summary>
internal sealed class SerializedLabelJsonConverterFactory : JsonConverterFactory
{
  public override bool CanConvert(Type typeToConvert)
  {
    // Don't convert arrays, collections, or value types
    if (typeToConvert.IsArray || typeToConvert.IsValueType)
    {
      return false;
    }

    // Don't convert collection types (IEnumerable, List, Dictionary, etc.)
    if (typeToConvert.IsGenericType)
    {
      var genericTypeDef = typeToConvert.GetGenericTypeDefinition();
      if (
        genericTypeDef == typeof(List<>)
        || genericTypeDef == typeof(IEnumerable<>)
        || genericTypeDef == typeof(ICollection<>)
        || genericTypeDef == typeof(IList<>)
        || genericTypeDef == typeof(Dictionary<,>)
        || genericTypeDef == typeof(IDictionary<,>)
      )
      {
        return false;
      }
    }

    // Don't convert string (even though it's IEnumerable<char>)
    if (typeToConvert == typeof(string))
    {
      return false;
    }

    // Don't convert System.Object (too generic, let default deserializer handle it)
    if (typeToConvert == typeof(object))
    {
      return false;
    }

    // Only convert class types (records, POCOs, etc.)
    return typeToConvert.IsClass;
  }

  public override JsonConverter? CreateConverter(Type typeToConvert, JsonSerializerOptions options)
  {
    var converterType = typeof(SerializedLabelJsonConverter<>).MakeGenericType(typeToConvert);
    return (JsonConverter?)Activator.CreateInstance(converterType);
  }
}

/// <summary>
/// JSON converter that respects SerializedLabel attributes for property mapping.
/// </summary>
/// <typeparam name="T">The type to convert</typeparam>
internal sealed class SerializedLabelJsonConverter<T> : JsonConverter<T>
  where T : notnull
{
  private readonly PropertyMappingPlan<T> _plan;

  public SerializedLabelJsonConverter()
  {
    _plan = PropertyMappingPlanner.Build<T>();
  }

  public override T? Read(
    ref Utf8JsonReader reader,
    Type typeToConvert,
    JsonSerializerOptions options
  )
  {
    if (reader.TokenType != JsonTokenType.StartObject)
    {
      throw new JsonException(
        $"Expected StartObject token but got {reader.TokenType} when deserializing type {typeToConvert.FullName}"
      );
    }

    var instance = SchemaActivator.CreateInstance<T>();

    // Create options without this specific converter instance to avoid infinite recursion
    // but keep the factory so it can be applied to nested IStructuredSerializable objects
    var optionsWithoutThisConverter = new JsonSerializerOptions(options);
    optionsWithoutThisConverter.Converters.Clear();
    foreach (var converter in options.Converters)
    {
      // Remove SerializedLabelJsonConverter<T> for THIS specific type only
      // Keep SerializedLabelJsonConverterFactory so it works for nested types
      if (converter.GetType() != typeof(SerializedLabelJsonConverter<T>))
      {
        optionsWithoutThisConverter.Converters.Add(converter);
      }
    }

    while (reader.Read())
    {
      if (reader.TokenType == JsonTokenType.EndObject)
      {
        return instance;
      }

      if (reader.TokenType != JsonTokenType.PropertyName)
      {
        throw new JsonException("Expected PropertyName token");
      }

      var propertyName = reader.GetString();
      reader.Read();

      if (propertyName != null && _plan.TryGetByFieldName(propertyName, out var binding))
      {
        var value = ReadPropertyValue(ref reader, binding!, optionsWithoutThisConverter);
        binding!.Property.SetValue(instance, value);
      }
      else
      {
        // Skip unknown properties by consuming them with JsonSerializer
        // This works correctly with streaming/partial JSON unlike reader.Skip()
        _ = JsonSerializer.Deserialize<object>(ref reader, optionsWithoutThisConverter);
      }
    }

    throw new JsonException("Unexpected end of JSON");
  }

  public override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options)
  {
    writer.WriteStartObject();

    // Create options without this specific converter instance to avoid infinite recursion
    // but keep the factory so it can be applied to nested objects
    var optionsWithoutThisConverter = new JsonSerializerOptions(options);
    optionsWithoutThisConverter.Converters.Clear();
    foreach (var converter in options.Converters)
    {
      // Remove SerializedLabelJsonConverter<T> for THIS specific type only
      // Keep SerializedLabelJsonConverterFactory so it works for nested types
      if (converter.GetType() != typeof(SerializedLabelJsonConverter<T>))
      {
        optionsWithoutThisConverter.Converters.Add(converter);
      }
    }

    foreach (var binding in _plan.Bindings)
    {
      object? propertyValue;
      try
      {
        propertyValue = binding.Property.GetValue(value);
      }
      catch
      {
        // Skip properties that can't be read
        continue;
      }

      if (
        propertyValue != null
        || options.DefaultIgnoreCondition
          != System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
      )
      {
        writer.WritePropertyName(binding.FieldName);

        if (propertyValue == null)
        {
          writer.WriteNullValue();
        }
        else
        {
          WritePropertyValue(writer, propertyValue, binding, optionsWithoutThisConverter);
        }
      }
    }

    writer.WriteEndObject();
  }

  // ── Per-binding read/write helpers ────────────────────────────────────────────

  // Reads a JSON token into the property's CLR value. For IScalar bindings, deserializes
  // the cell as the backing type and constructs the wrapper — closing the JSON-side
  // IScalar gap that motivated Phase B's planner work. Other kinds dispatch to the
  // standard JsonSerializer.Deserialize(propertyType) flow.
  private static object? ReadPropertyValue(
    ref Utf8JsonReader reader,
    PropertyBinding binding,
    JsonSerializerOptions options
  )
  {
    if (binding.Kind == PropertyKind.IScalar && reader.TokenType != JsonTokenType.Null)
    {
      var info = binding.IScalar!;
      var rawValue = JsonSerializer.Deserialize(ref reader, info.BackingType, options);
      if (rawValue is null)
      {
        return null;
      }
      return info.WrappingConstructor.Invoke(new[] { rawValue });
    }

    return JsonSerializer.Deserialize(
      ref reader,
      binding.Property.PropertyType,
      options
    );
  }

  // Writes the property's value as JSON. For IScalar bindings, unwraps to the backing
  // type before delegating to JsonSerializer.Serialize so the cell appears as the raw
  // primitive in the output. Other kinds dispatch to JsonSerializer.Serialize on the
  // declared property type.
  private static void WritePropertyValue(
    Utf8JsonWriter writer,
    object propertyValue,
    PropertyBinding binding,
    JsonSerializerOptions options
  )
  {
    if (binding.Kind == PropertyKind.IScalar)
    {
      var info = binding.IScalar!;
      var rawValue = info.ValueProperty.GetValue(propertyValue);
      JsonSerializer.Serialize(writer, rawValue, info.BackingType, options);
      return;
    }

    JsonSerializer.Serialize(
      writer,
      propertyValue,
      binding.Property.PropertyType,
      options
    );
  }
}

/// <summary>
/// JSON converter factory that creates converters for enum types respecting SerializedEnum attributes.
/// </summary>
internal sealed class SerializedEnumJsonConverterFactory : JsonConverterFactory
{
  public override bool CanConvert(Type typeToConvert)
  {
    return typeToConvert.IsEnum;
  }

  public override JsonConverter? CreateConverter(Type typeToConvert, JsonSerializerOptions options)
  {
    var converterType = typeof(SerializedEnumJsonConverter<>).MakeGenericType(typeToConvert);
    return (JsonConverter?)Activator.CreateInstance(converterType);
  }
}

/// <summary>
/// JSON converter that respects SerializedEnum attributes for enum value mapping.
/// </summary>
/// <typeparam name="TEnum">The enum type to convert.</typeparam>
internal sealed class SerializedEnumJsonConverter<TEnum> : JsonConverter<TEnum>
  where TEnum : struct, Enum
{
  private readonly EnumMetadataCache<TEnum> _metadata;

  public SerializedEnumJsonConverter()
  {
    _metadata = EnumMetadataRegistry.Create<TEnum>();
  }

  public override TEnum Read(
    ref Utf8JsonReader reader,
    Type typeToConvert,
    JsonSerializerOptions options
  )
  {
    if (reader.TokenType != JsonTokenType.String)
    {
      throw new JsonException(
        $"Expected string value for enum type '{typeof(TEnum).Name}', "
          + $"but got {reader.TokenType}. Enum values must be serialized as strings "
          + $"when using [SerializedEnum] attributes."
      );
    }

    string? value = reader.GetString();
    if (value == null)
    {
      throw new JsonException(
        $"Null string value encountered for enum type '{typeof(TEnum).Name}'."
      );
    }

    try
    {
      return _metadata.Parse(value);
    }
    catch (InvalidOperationException ex)
    {
      throw new JsonException(
        $"Failed to deserialize enum value '{value}' for type '{typeof(TEnum).Name}'. {ex.Message}",
        ex
      );
    }
  }

  public override void Write(Utf8JsonWriter writer, TEnum value, JsonSerializerOptions options)
  {
    try
    {
      string serializedValue = _metadata.ToString(value);
      writer.WriteStringValue(serializedValue);
    }
    catch (InvalidOperationException ex)
    {
      throw new JsonException(
        $"Failed to serialize enum value '{value}' of type '{typeof(TEnum).Name}'. {ex.Message}",
        ex
      );
    }
  }
}
