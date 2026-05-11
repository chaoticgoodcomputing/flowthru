using System.Reflection;
using Apache.Arrow;
using Apache.Arrow.Types;
using Flowthru.Data.Schema;

namespace Flowthru.Step.Python.Internal;

/// <summary>
/// Maps C# schema types to Apache Arrow schemas for tabular data interchange.
/// </summary>
/// <remarks>
/// <para>
/// <strong>Purpose:</strong> Generates Arrow schemas from C# types annotated with [FlowthruSchema],
/// preserving field names (via [SerializedLabel]) and nullability for correct DataFrame marshalling.
/// </para>
/// <para>
/// <strong>Type Mapping (C# → Arrow):</strong>
/// </para>
/// <list type="bullet">
/// <item>int, int? → Int32Type</item>
/// <item>long, long? → Int64Type</item>
/// <item>float, float? → FloatType</item>
/// <item>double, double? → DoubleType</item>
/// <item>bool, bool? → BooleanType</item>
/// <item>string → StringType (always nullable in Arrow)</item>
/// <item>DateTime, DateTime? → TimestampType (microsecond, UTC)</item>
/// <item>DateTimeOffset, DateTimeOffset? → TimestampType (microsecond, UTC)</item>
/// <item>TimeSpan, TimeSpan? → DurationType (microsecond)</item>
/// <item>Guid, Guid? → StringType (serialized as string)</item>
/// <item>byte[] → BinaryType</item>
/// </list>
/// <para>
/// <strong>Field Naming:</strong> Resolves <c>[SerializedLabel]</c> attributes via
/// <see cref="GetFieldName"/>, the same resolution Core's
/// <see cref="Flowthru.Data.Schema.Mapping.PropertyMappingPlanner"/> applies for
/// CSV/Parquet/JSON serializers.
/// </para>
/// </remarks>
public static class ArrowSchemaMapper
{
  /// <summary>
  /// Generates an Apache Arrow schema from a C# schema type.
  /// </summary>
  /// <typeparam name="T">The C# schema type to map</typeparam>
  /// <returns>Arrow schema with fields matching the C# type's properties</returns>
  /// <exception cref="NotSupportedException">
  /// Thrown when a property type cannot be mapped to Arrow.
  /// </exception>
  /// <remarks>
  /// The schema includes all public instance properties, with field names determined by
  /// [SerializedLabel] attributes or property names. Nullability is preserved from the C# type.
  /// </remarks>
  public static Schema BuildArrowSchema<T>()
    where T : notnull
  {
    return BuildArrowSchema(typeof(T));
  }

  /// <summary>
  /// Generates an Apache Arrow schema from a C# type.
  /// </summary>
  /// <param name="type">The C# type to map</param>
  /// <returns>Arrow schema with fields matching the type's properties</returns>
  /// <exception cref="NotSupportedException">
  /// Thrown when a property type cannot be mapped to Arrow.
  /// </exception>
  public static Schema BuildArrowSchema(Type type)
  {
    var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);
    var fields = new List<Field>();

    foreach (var property in properties)
    {
      // Skip indexed properties (this[int index])
      if (property.GetIndexParameters().Length > 0)
      {
        continue;
      }

      // Get external field name (respects SerializedLabel)
      var fieldName = GetFieldName(property);

      // Map C# type to Arrow type
      var arrowType = MapToArrowType(property.PropertyType, out bool nullable);

      // Create Arrow field with nullability
      var field = new Field(fieldName, arrowType, nullable);
      fields.Add(field);
    }

    return new Schema(fields, metadata: null);
  }

  /// <summary>
  /// Maps a C# CLR type to an Apache Arrow type.
  /// </summary>
  /// <param name="clrType">The C# type to map</param>
  /// <param name="nullable">Output parameter indicating if the field is nullable</param>
  /// <returns>The corresponding Arrow type</returns>
  /// <exception cref="NotSupportedException">
  /// Thrown when the CLR type cannot be mapped to Arrow.
  /// </exception>
  private static IArrowType MapToArrowType(Type clrType, out bool nullable)
  {
    // Handle nullable value types (int?, double?, etc.)
    var underlyingType = Nullable.GetUnderlyingType(clrType);
    if (underlyingType != null)
    {
      nullable = true;
      return MapNonNullableType(underlyingType);
    }

    // Reference types are nullable by default in Arrow
    nullable = !clrType.IsValueType;

    return MapNonNullableType(clrType);
  }

  /// <summary>
  /// Maps a non-nullable C# type to an Arrow type.
  /// </summary>
  private static IArrowType MapNonNullableType(Type type)
  {
    // Primitive types
    if (type == typeof(int))
    {
      return Int32Type.Default;
    }

    if (type == typeof(long))
    {
      return Int64Type.Default;
    }

    if (type == typeof(float))
    {
      return FloatType.Default;
    }

    if (type == typeof(double))
    {
      return DoubleType.Default;
    }

    if (type == typeof(bool))
    {
      return BooleanType.Default;
    }

    if (type == typeof(string))
    {
      return StringType.Default;
    }

    // Temporal types
    if (type == typeof(DateTime))
    {
      // Store as microseconds since Unix epoch (timezone-naive)
      return new TimestampType(TimeUnit.Microsecond, (string?)null);
    }

    if (type == typeof(DateTimeOffset))
    {
      // Store as microseconds since Unix epoch (UTC)
      // Note: offset information is lost; convert to UTC before marshalling
      return new TimestampType(TimeUnit.Microsecond, timezone: "UTC");
    }

    if (type == typeof(TimeSpan))
    {
      // Store as microseconds
      return DurationType.Microsecond;
    }

    // Special types
    if (type == typeof(Guid))
    {
      // Arrow has no native Guid type; serialize as string
      return StringType.Default;
    }

    if (type == typeof(byte[]))
    {
      return BinaryType.Default;
    }

    // Enum types: serialize as string (name), matching the Python-side string representation
    if (type.IsEnum)
    {
      return StringType.Default;
    }

    // Unsupported type
    throw new NotSupportedException(
      $"Type '{type.FullName}' cannot be mapped to Apache Arrow. "
        + "Supported types: int, long, float, double, bool, string, DateTime, "
        + "DateTimeOffset, TimeSpan, Guid, byte[], enum, and their nullable variants."
    );
  }

  /// <summary>
  /// Gets the external field name for a property, respecting [SerializedLabel].
  /// </summary>
  /// <remarks>
  /// Inlined during Phase B5 — previously delegated to <c>PropertyMappingHelper.GetFieldName</c>
  /// in <c>Flowthru.Core.Data.Storage.Format</c>, which has been deleted.
  /// </remarks>
  internal static string GetFieldName(PropertyInfo property)
  {
    var label = property.GetCustomAttribute<SerializedLabelAttribute>();
    return label?.Label ?? property.Name;
  }

  /// <summary>
  /// Builds a C# dictionary mapping column names to pandas dtype strings,
  /// suitable for JSON serialization in subprocess protocol messages.
  /// </summary>
  /// <typeparam name="T">The C# schema type</typeparam>
  /// <returns>Dictionary with dtype specifications for the subprocess worker's df_to_ipc</returns>
  public static Dictionary<string, string> BuildDtypeSpecDictionary<T>()
    where T : notnull
  {
    var schema = BuildArrowSchema<T>();
    var dict = new Dictionary<string, string>();
    foreach (var field in schema.FieldsList)
    {
      var pandasDtype = field.DataType switch
      {
        Int32Type => "int32",
        Int64Type => "int64",
        Int16Type => "int16",
        UInt8Type => "uint8",
        FloatType => "float32",
        DoubleType => "float64",
        BooleanType => "bool",
        StringType => "object",
        TimestampType ts => ts.Timezone != null ? "datetime64[ns, UTC]" : "datetime64[ns]",
        DurationType => "timedelta64[ns]",
        BinaryType => "object",
        _ => "object",
      };
      dict[field.Name] = pandasDtype;
    }
    return dict;
  }

}
