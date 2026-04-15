using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Serialization;
using Flowthru.Core.Abstractions;
using Flowthru.Core.Data.Capabilities;

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
      )
    { }

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
    public async IAsyncEnumerable<TRow> DeserializeRows(Stream stream)
    {
        if (stream == null)
        {
            throw new ArgumentNullException(nameof(stream));
        }

        // Deserialize as array of TRow
        var items = await JsonSerializer.DeserializeAsync<TRow[]>(stream, _options);

        if (items == null)
        {
            yield break;
        }

        // Yield each item
        foreach (var item in items)
        {
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
    private readonly Dictionary<string, PropertyInfo> _propertyMap;

    public SerializedLabelJsonConverter()
    {
        _propertyMap = PropertyMappingHelper.BuildPropertyMap<T>();
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

            if (propertyName != null && _propertyMap.TryGetValue(propertyName, out var property))
            {
                var value = JsonSerializer.Deserialize(
                  ref reader,
                  property.PropertyType,
                  optionsWithoutThisConverter
                );
                property.SetValue(instance, value);
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

        foreach (var (fieldName, property) in _propertyMap)
        {
            object? propertyValue;
            try
            {
                propertyValue = property.GetValue(value);
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
                writer.WritePropertyName(fieldName);

                // Serialize the property value using the appropriate overload
                if (propertyValue == null)
                {
                    writer.WriteNullValue();
                }
                else
                {
                    // Use the property type to ensure correct converter selection for nested objects
                    JsonSerializer.Serialize(
                      writer,
                      propertyValue,
                      property.PropertyType,
                      optionsWithoutThisConverter
                    );
                }
            }
        }

        writer.WriteEndObject();
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
    private readonly Serialization.EnumMetadataCache<TEnum> _metadata;

    public SerializedEnumJsonConverter()
    {
        _metadata = Serialization.EnumMetadataRegistry.Create<TEnum>();
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
