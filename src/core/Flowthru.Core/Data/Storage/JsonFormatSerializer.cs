using System.Text.Json;
using System.Text.Json.Serialization;
using Flowthru.Data.Schema;
using Flowthru.Data.Schema.Mapping;

namespace Flowthru.Data.Storage;

/// <summary>
/// JSON format serializer using <c>System.Text.Json</c>, wired through
/// <see cref="PropertyMappingPlanner"/> to honor
/// <see cref="SerializedLabelAttribute"/> and <see cref="IScalar"/> NewType
/// wrapping. Supports both flat and nested schemas (declared via
/// <see cref="IStructuredSerializable"/>, emitted by the schema source
/// generator).
/// </summary>
/// <typeparam name="TRow">The row schema type.</typeparam>
public sealed class JsonFormatSerializer<TRow>
  : IFormatSerializer<TRow>, ISupportsIScalar, ISupportsNested
  where TRow : notnull, IStructuredSerializable
{
  private readonly JsonSerializerOptions _options;

  public JsonFormatSerializer()
    : this(
      new JsonSerializerOptions
      {
        WriteIndented = true,
        PropertyNamingPolicy = null,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
      }
    ) { }

  public JsonFormatSerializer(JsonSerializerOptions options)
  {
    _options = options ?? throw new ArgumentNullException(nameof(options));
    // Planner-driven label converter handles [SerializedLabel] property
    // name mapping AND IScalar wrapping. Runs for any class type that
    // isn't a collection or string.
    _options.Converters.Add(new SerializedLabelJsonConverterFactory());
  }

  /// <summary>The JSON serialization options in use.</summary>
  public JsonSerializerOptions Options => _options;

  /// <inheritdoc/>
  public StorageTraits Traits => new() { CanStream = false };

  /// <inheritdoc/>
  public async IAsyncEnumerable<TRow> DeserializeRows(Stream stream)
  {
    if (stream is null)
    {
      throw new ArgumentNullException(nameof(stream));
    }

    await foreach (var item in JsonSerializer.DeserializeAsyncEnumerable<TRow>(stream, _options).ConfigureAwait(false))
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
    if (stream is null)
    {
      throw new ArgumentNullException(nameof(stream));
    }
    if (rows is null)
    {
      throw new ArgumentNullException(nameof(rows));
    }

    var rowList = new List<TRow>();
    await foreach (var row in rows.ConfigureAwait(false))
    {
      rowList.Add(row);
    }
    await JsonSerializer.SerializeAsync(stream, rowList, _options).ConfigureAwait(false);
  }
}

/// <summary>
/// Factory that produces planner-driven JSON converters for class types
/// — uses <see cref="PropertyMappingPlanner"/> to honor
/// <see cref="SerializedLabelAttribute"/> and <see cref="IScalar"/>
/// wrapping. Skips collections, strings, and value types so they fall
/// through to <c>System.Text.Json</c> defaults.
/// </summary>
internal sealed class SerializedLabelJsonConverterFactory : JsonConverterFactory
{
  public override bool CanConvert(Type typeToConvert)
  {
    if (typeToConvert.IsArray || typeToConvert.IsValueType)
    {
      return false;
    }
    if (typeToConvert.IsGenericType)
    {
      var def = typeToConvert.GetGenericTypeDefinition();
      if (
        def == typeof(List<>)
        || def == typeof(IEnumerable<>)
        || def == typeof(ICollection<>)
        || def == typeof(IList<>)
        || def == typeof(IReadOnlyList<>)
        || def == typeof(IReadOnlyCollection<>)
        || def == typeof(Dictionary<,>)
        || def == typeof(IDictionary<,>)
        || def == typeof(IReadOnlyDictionary<,>)
      )
      {
        return false;
      }
    }
    if (typeToConvert == typeof(string) || typeToConvert == typeof(object))
    {
      return false;
    }
    return typeToConvert.IsClass;
  }

  public override JsonConverter? CreateConverter(Type typeToConvert, JsonSerializerOptions options)
  {
    var converterType = typeof(SerializedLabelJsonConverter<>).MakeGenericType(typeToConvert);
    return (JsonConverter?)Activator.CreateInstance(converterType);
  }
}

/// <summary>
/// Per-type planner-driven JSON converter. Builds a
/// <see cref="PropertyMappingPlan{T}"/> at construction time and uses it
/// to map external field names to schema properties — including IScalar
/// wrap/unwrap on the cell value.
/// </summary>
internal sealed class SerializedLabelJsonConverter<T> : JsonConverter<T>
  where T : notnull
{
  private readonly PropertyMappingPlan<T> _plan = PropertyMappingPlanner.Build<T>();

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
    var optionsWithoutSelf = OptionsWithoutSelf(options);

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
        var value = ReadPropertyValue(ref reader, binding, optionsWithoutSelf);
        binding.Property.SetValue(instance, value);
      }
      else
      {
        // Unknown property — skip by deserializing through (works correctly
        // with streaming/partial JSON unlike reader.Skip()).
        _ = JsonSerializer.Deserialize<object>(ref reader, optionsWithoutSelf);
      }
    }

    throw new JsonException("Unexpected end of JSON");
  }

  public override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options)
  {
    writer.WriteStartObject();
    var optionsWithoutSelf = OptionsWithoutSelf(options);

    foreach (var binding in _plan.Bindings)
    {
      object? propertyValue;
      try
      {
        propertyValue = binding.Property.GetValue(value);
      }
      catch
      {
        continue;
      }

      if (
        propertyValue is not null
        || options.DefaultIgnoreCondition != JsonIgnoreCondition.WhenWritingNull
      )
      {
        writer.WritePropertyName(binding.FieldName);
        if (propertyValue is null)
        {
          writer.WriteNullValue();
        }
        else
        {
          WritePropertyValue(writer, propertyValue, binding, optionsWithoutSelf);
        }
      }
    }

    writer.WriteEndObject();
  }

  // Strip this concrete converter from the options to avoid infinite
  // recursion when STJ hits a property of the same type. Factory stays so
  // converters for nested types are still constructed.
  private static JsonSerializerOptions OptionsWithoutSelf(JsonSerializerOptions options)
  {
    var copy = new JsonSerializerOptions(options);
    copy.Converters.Clear();
    foreach (var converter in options.Converters)
    {
      if (converter.GetType() != typeof(SerializedLabelJsonConverter<T>))
      {
        copy.Converters.Add(converter);
      }
    }
    return copy;
  }

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
      return rawValue is null ? null : info.WrappingConstructor.Invoke(new[] { rawValue });
    }
    return JsonSerializer.Deserialize(ref reader, binding.Property.PropertyType, options);
  }

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
    JsonSerializer.Serialize(writer, propertyValue, binding.Property.PropertyType, options);
  }
}
