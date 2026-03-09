using System.Reflection;
using Apache.Arrow;
using Apache.Arrow.Ipc;
using Apache.Arrow.Types;
using Flowthru.Data.Storage;
using Flowthru.Data.Storage.Format;

namespace Flowthru.Extensions.Python.Marshalling;

/// <summary>
/// Marshals tabular data between C# IEnumerable&lt;T&gt; and Apache Arrow RecordBatch.
/// </summary>
/// <remarks>
/// <para>
/// <strong>Purpose:</strong> Bidirectional conversion for DataFrame interchange between
/// C# and Python via Arrow IPC (Inter-Process Communication) format.
/// </para>
/// <para>
/// <strong>C# → Arrow Flow:</strong>
/// </para>
/// <code>
/// IEnumerable&lt;T&gt; → RecordBatch → IPC buffer → Python pyarrow.Table → pd.DataFrame
/// </code>
/// <para>
/// <strong>Arrow → C# Flow:</strong>
/// </para>
/// <code>
/// pd.DataFrame → pyarrow.Table → IPC buffer → RecordBatch → IEnumerable&lt;T&gt;
/// </code>
/// <para>
/// <strong>Performance:</strong> Uses columnar processing (column-by-column, not row-by-row)
/// for efficient Arrow array construction.
/// </para>
/// </remarks>
public static class ArrowMarshaller
{
  /// <summary>
  /// Converts an IEnumerable of C# objects to an Arrow RecordBatch.
  /// </summary>
  /// <typeparam name="T">The C# schema type</typeparam>
  /// <param name="rows">The rows to convert</param>
  /// <returns>Arrow RecordBatch containing the data</returns>
  /// <exception cref="ArgumentNullException">Thrown when rows is null</exception>
  /// <exception cref="NotSupportedException">
  /// Thrown when a property type cannot be marshalled to Arrow.
  /// </exception>
  /// <remarks>
  /// Data is processed column-wise for efficiency. All rows are materialized into memory
  /// to build the RecordBatch.
  /// </remarks>
  public static RecordBatch ToRecordBatch<T>(IEnumerable<T> rows)
    where T : notnull
  {
    if (rows == null)
    {
      throw new ArgumentNullException(nameof(rows));
    }

    var schema = ArrowSchemaMapper.BuildArrowSchema<T>();
    var materializedRows = rows.ToList();
    var rowCount = materializedRows.Count;

    // Get property info for each field
    var properties = typeof(T)
      .GetProperties(BindingFlags.Public | BindingFlags.Instance)
      .Where(p => p.GetIndexParameters().Length == 0)
      .ToList();

    // Build Arrow arrays column-by-column
    var arrays = new List<IArrowArray>();

    foreach (var field in schema.FieldsList)
    {
      // Find corresponding property
      var property = properties.First(p => PropertyMappingHelper.GetFieldName(p) == field.Name);

      // Build array for this column
      var array = BuildArrayForProperty(property, field, materializedRows, rowCount);
      arrays.Add(array);
    }

    var batch = new RecordBatch(schema, arrays, rowCount);
    return batch;
  }

  /// <summary>
  /// Converts an Arrow RecordBatch to an IEnumerable of C# objects.
  /// </summary>
  /// <typeparam name="T">The C# schema type</typeparam>
  /// <param name="batch">The Arrow RecordBatch to convert</param>
  /// <returns>IEnumerable of C# objects</returns>
  /// <exception cref="ArgumentNullException">Thrown when batch is null</exception>
  /// <exception cref="InvalidOperationException">
  /// Thrown when schema mismatch or type conversion fails.
  /// </exception>
  /// <remarks>
  /// Uses SchemaActivator for instantiation to support required members.
  /// Data is converted row-by-row (column values → object properties).
  /// </remarks>
  public static IEnumerable<T> FromRecordBatch<T>(RecordBatch batch)
    where T : notnull
  {
    if (batch == null)
    {
      throw new ArgumentNullException(nameof(batch));
    }

    var type = typeof(T);
    var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
      .Where(p => p.GetIndexParameters().Length == 0)
      .ToList();

    // Build property → column mapping
    var propertyColumns = new Dictionary<PropertyInfo, IArrowArray>();

    foreach (var property in properties)
    {
      var fieldName = PropertyMappingHelper.GetFieldName(property);
      var columnIndex = batch.Schema.GetFieldIndex(fieldName);

      if (columnIndex < 0)
      {
        throw new InvalidOperationException(
          $"Arrow RecordBatch does not contain field '{fieldName}' "
            + $"required by property '{property.Name}' on type '{type.Name}'."
        );
      }

      propertyColumns[property] = batch.Column(columnIndex);
    }

    // Convert row-by-row
    var results = new List<T>();

    for (int rowIndex = 0; rowIndex < batch.Length; rowIndex++)
    {
      // Phase 5 will integrate SchemaActivator for required members support
      var instance = (T)Activator.CreateInstance(type)!;

      foreach (var kvp in propertyColumns)
      {
        var property = kvp.Key;
        var column = kvp.Value;

        var value = GetValueFromArray(column, rowIndex, property.PropertyType);
        property.SetValue(instance, value);
      }

      results.Add(instance);
    }

    return results;
  }

  /// <summary>
  /// Serializes an Arrow RecordBatch to an IPC buffer for Python.NET transfer.
  /// </summary>
  /// <param name="batch">The RecordBatch to serialize</param>
  /// <returns>Byte array containing Arrow IPC stream</returns>
  /// <exception cref="ArgumentNullException">Thrown when batch is null</exception>
  public static byte[] ToIpcBuffer(RecordBatch batch)
  {
    if (batch == null)
    {
      throw new ArgumentNullException(nameof(batch));
    }

    using var stream = new MemoryStream();
    using var writer = new ArrowStreamWriter(stream, batch.Schema);

    writer.WriteRecordBatch(batch);
    writer.WriteEnd();

    return stream.ToArray();
  }

  /// <summary>
  /// Deserializes an Arrow IPC buffer (from Python) to a RecordBatch.
  /// </summary>
  /// <param name="buffer">Byte array containing Arrow IPC stream</param>
  /// <returns>Arrow RecordBatch</returns>
  /// <exception cref="ArgumentNullException">Thrown when buffer is null</exception>
  /// <exception cref="InvalidDataException">
  /// Thrown when buffer is not a valid Arrow IPC stream.
  /// </exception>
  public static RecordBatch FromIpcBuffer(byte[] buffer)
  {
    if (buffer == null)
    {
      throw new ArgumentNullException(nameof(buffer));
    }

    if (buffer.Length == 0)
    {
      throw new InvalidDataException("Arrow IPC buffer is empty.");
    }

    using var stream = new MemoryStream(buffer);
    using var reader = new ArrowStreamReader(stream);

    var batch = reader.ReadNextRecordBatch();

    if (batch == null)
    {
      throw new InvalidDataException(
        "Arrow IPC buffer does not contain a valid RecordBatch. "
          + "The stream may be empty or corrupted."
      );
    }

    return batch;
  }

  // ──────────────────────────────────────────────────────────────
  // Array Building (C# → Arrow)
  // ──────────────────────────────────────────────────────────────

  /// <summary>
  /// Builds an Arrow array for a single property across all rows.
  /// </summary>
  private static IArrowArray BuildArrayForProperty<T>(
    PropertyInfo property,
    Field field,
    List<T> rows,
    int rowCount
  )
  {
    var propertyType = property.PropertyType;
    var underlyingType = Nullable.GetUnderlyingType(propertyType) ?? propertyType;

    // Delegate to type-specific builder
    if (underlyingType == typeof(int))
    {
      return BuildInt32Array(property, rows, rowCount);
    }

    if (underlyingType == typeof(long))
    {
      return BuildInt64Array(property, rows, rowCount);
    }

    if (underlyingType == typeof(float))
    {
      return BuildFloatArray(property, rows, rowCount);
    }

    if (underlyingType == typeof(double))
    {
      return BuildDoubleArray(property, rows, rowCount);
    }

    if (underlyingType == typeof(bool))
    {
      return BuildBooleanArray(property, rows, rowCount);
    }

    if (underlyingType == typeof(string))
    {
      return BuildStringArray(property, rows, rowCount);
    }

    if (underlyingType == typeof(DateTime))
    {
      return BuildDateTimeArray(property, rows, rowCount);
    }

    if (underlyingType == typeof(DateTimeOffset))
    {
      return BuildDateTimeOffsetArray(property, rows, rowCount);
    }

    if (underlyingType == typeof(TimeSpan))
    {
      return BuildDurationArray(property, rows, rowCount);
    }

    if (underlyingType == typeof(Guid))
    {
      return BuildGuidArray(property, rows, rowCount);
    }

    if (underlyingType == typeof(byte[]))
    {
      return BuildBinaryArray(property, rows, rowCount);
    }

    throw new NotSupportedException(
      $"Property '{property.Name}' has type '{propertyType.Name}' which is not supported for Arrow marshalling."
    );
  }

  private static IArrowArray BuildInt32Array<T>(PropertyInfo property, List<T> rows, int rowCount)
  {
    var builder = new Int32Array.Builder();
    foreach (var row in rows)
    {
      var value = property.GetValue(row);
      if (value == null)
      {
        builder.AppendNull();
      }
      else
      {
        builder.Append((int)value);
      }
    }
    return builder.Build();
  }

  private static IArrowArray BuildInt64Array<T>(PropertyInfo property, List<T> rows, int rowCount)
  {
    var builder = new Int64Array.Builder();
    foreach (var row in rows)
    {
      var value = property.GetValue(row);
      if (value == null)
      {
        builder.AppendNull();
      }
      else
      {
        builder.Append((long)value);
      }
    }
    return builder.Build();
  }

  private static IArrowArray BuildFloatArray<T>(PropertyInfo property, List<T> rows, int rowCount)
  {
    var builder = new FloatArray.Builder();
    foreach (var row in rows)
    {
      var value = property.GetValue(row);
      if (value == null)
      {
        builder.AppendNull();
      }
      else
      {
        builder.Append((float)value);
      }
    }
    return builder.Build();
  }

  private static IArrowArray BuildDoubleArray<T>(PropertyInfo property, List<T> rows, int rowCount)
  {
    var builder = new DoubleArray.Builder();
    foreach (var row in rows)
    {
      var value = property.GetValue(row);
      if (value == null)
      {
        builder.AppendNull();
      }
      else
      {
        builder.Append((double)value);
      }
    }
    return builder.Build();
  }

  private static IArrowArray BuildBooleanArray<T>(PropertyInfo property, List<T> rows, int rowCount)
  {
    var builder = new BooleanArray.Builder();
    foreach (var row in rows)
    {
      var value = property.GetValue(row);
      if (value == null)
      {
        builder.AppendNull();
      }
      else
      {
        builder.Append((bool)value);
      }
    }
    return builder.Build();
  }

  private static IArrowArray BuildStringArray<T>(PropertyInfo property, List<T> rows, int rowCount)
  {
    var builder = new StringArray.Builder();
    foreach (var row in rows)
    {
      var value = property.GetValue(row);
      if (value == null)
      {
        builder.AppendNull();
      }
      else
      {
        builder.Append((string)value);
      }
    }
    return builder.Build();
  }

  private static IArrowArray BuildDateTimeArray<T>(
    PropertyInfo property,
    List<T> rows,
    int rowCount
  )
  {
    var timestampType = new TimestampType(TimeUnit.Microsecond, (string?)null);
    var builder = new TimestampArray.Builder(timestampType);

    foreach (var row in rows)
    {
      var value = property.GetValue(row);
      if (value == null)
      {
        builder.AppendNull();
      }
      else
      {
        var dt = (DateTime)value;
        // Convert to UTC if not already
        var utcDt = dt.Kind == DateTimeKind.Utc ? dt : dt.ToUniversalTime();
        // Builder handles conversion to microseconds since Unix epoch
        builder.Append(new DateTimeOffset(utcDt, TimeSpan.Zero));
      }
    }

    return builder.Build();
  }

  private static IArrowArray BuildDateTimeOffsetArray<T>(
    PropertyInfo property,
    List<T> rows,
    int rowCount
  )
  {
    var timestampType = new TimestampType(TimeUnit.Microsecond, timezone: "UTC");
    var builder = new TimestampArray.Builder(timestampType);

    foreach (var row in rows)
    {
      var value = property.GetValue(row);
      if (value == null)
      {
        builder.AppendNull();
      }
      else
      {
        var dto = (DateTimeOffset)value;
        // Convert to UTC
        var utcDto = dto.ToUniversalTime();
        // Builder handles conversion to microseconds since Unix epoch
        builder.Append(utcDto);
      }
    }

    return builder.Build();
  }

  private static IArrowArray BuildDurationArray<T>(
    PropertyInfo property,
    List<T> rows,
    int rowCount
  )
  {
    var durationType = DurationType.Microsecond;
    var builder = new DurationArray.Builder(durationType);

    foreach (var row in rows)
    {
      var value = property.GetValue(row);
      if (value == null)
      {
        builder.AppendNull();
      }
      else
      {
        var timeSpan = (TimeSpan)value;
        // Builder handles conversion to microseconds
        builder.Append(timeSpan);
      }
    }

    return builder.Build();
  }

  private static IArrowArray BuildGuidArray<T>(PropertyInfo property, List<T> rows, int rowCount)
  {
    // Guid stored as string in Arrow
    var builder = new StringArray.Builder();
    foreach (var row in rows)
    {
      var value = property.GetValue(row);
      if (value == null)
      {
        builder.AppendNull();
      }
      else
      {
        builder.Append(((Guid)value).ToString("D")); // Standard format: 8-4-4-4-12
      }
    }
    return builder.Build();
  }

  private static IArrowArray BuildBinaryArray<T>(PropertyInfo property, List<T> rows, int rowCount)
  {
    var builder = new BinaryArray.Builder();
    foreach (var row in rows)
    {
      var value = property.GetValue(row);
      if (value == null)
      {
        builder.AppendNull();
      }
      else
      {
        builder.Append((byte[])value);
      }
    }
    return builder.Build();
  }

  // ──────────────────────────────────────────────────────────────
  // Value Extraction (Arrow → C#)
  // ──────────────────────────────────────────────────────────────

  /// <summary>
  /// Extracts a value from an Arrow array at a specific index, converting to the target CLR type.
  /// </summary>
  private static object? GetValueFromArray(IArrowArray array, int index, Type targetType)
  {
    // Check for null
    if (array.IsNull(index))
    {
      return null;
    }

    var underlyingType = Nullable.GetUnderlyingType(targetType) ?? targetType;

    // Primitive types
    if (underlyingType == typeof(int) && array is Int32Array int32Array)
    {
      return int32Array.GetValue(index);
    }

    if (underlyingType == typeof(long) && array is Int64Array int64Array)
    {
      return int64Array.GetValue(index);
    }

    if (underlyingType == typeof(float) && array is FloatArray floatArray)
    {
      return floatArray.GetValue(index);
    }

    if (underlyingType == typeof(double) && array is DoubleArray doubleArray)
    {
      return doubleArray.GetValue(index);
    }

    if (underlyingType == typeof(bool) && array is BooleanArray boolArray)
    {
      return boolArray.GetValue(index);
    }

    if (underlyingType == typeof(string))
    {
      return array switch
      {
        StringArray stringArray => stringArray.GetString(index),
        LargeStringArray largeStringArray => largeStringArray.GetString(index),
        _ => throw new NotSupportedException(
          $"Cannot convert Arrow array of type '{array.Data.DataType.Name}' to C# type 'String'."
        ),
      };
    }

    // Temporal types
    if (underlyingType == typeof(DateTime) && array is TimestampArray timestampArray)
    {
      var dto = timestampArray.GetTimestamp(index);
      if (dto == null)
        return null;
      return dto.Value.UtcDateTime;
    }

    if (underlyingType == typeof(DateTimeOffset) && array is TimestampArray tsArray)
    {
      return tsArray.GetTimestamp(index);
    }

    if (underlyingType == typeof(TimeSpan) && array is DurationArray durationArray)
    {
      return durationArray.GetTimeSpan(index);
    }

    // Special types
    if (underlyingType == typeof(Guid))
    {
      string? guidString = array switch
      {
        StringArray stringArray => stringArray.GetString(index),
        LargeStringArray largeStringArray => largeStringArray.GetString(index),
        _ => null,
      };

      if (guidString != null)
      {
        return Guid.Parse(guidString);
      }

      return null;
    }

    if (underlyingType == typeof(byte[]))
    {
      return array switch
      {
        BinaryArray binaryArray => binaryArray.GetBytes(index).ToArray(),
        LargeBinaryArray largeBinaryArray => largeBinaryArray.GetBytes(index).ToArray(),
        _ => null,
      };
    }

    // Numeric type coercion (pandas compatibility)
    // Keep safe widening conversions that preserve precision
    if (underlyingType == typeof(long))
    {
      if (array is Int32Array int32ToInt64Array)
      {
        return (long?)int32ToInt64Array.GetValue(index);
      }
    }

    if (underlyingType == typeof(double))
    {
      if (array is FloatArray floatToDoubleArray)
      {
        return (double?)floatToDoubleArray.GetValue(index);
      }
      if (array is Int32Array int32ToDoubleArray)
      {
        return (double?)int32ToDoubleArray.GetValue(index);
      }
      if (array is Int64Array int64ToDoubleArray)
      {
        return (double?)int64ToDoubleArray.GetValue(index);
      }
    }

    throw new NotSupportedException(
      $"Cannot convert Arrow array of type '{array.Data.DataType.Name}' to C# type '{targetType.Name}'."
    );
  }
}
