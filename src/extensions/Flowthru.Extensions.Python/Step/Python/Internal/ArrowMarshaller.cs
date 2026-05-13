using System.Reflection;
using Apache.Arrow;
using Apache.Arrow.Ipc;
using Apache.Arrow.Types;
using Flowthru.Data.Schema;
using Flowthru.Data.Storage;

namespace Flowthru.Step.Python.Internal;

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
      var property = properties.First(p => ArrowSchemaMapper.GetFieldName(p) == field.Name);

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
      var fieldName = ArrowSchemaMapper.GetFieldName(property);
      // Different Apache.Arrow versions disagree on missing-field semantics:
      // 18 throws InvalidOperationException, 23+ returns -1. Handle both so
      // the user always gets the friendly diagnostic naming the missing
      // field AND the property requiring it.
      int columnIndex;
      try { columnIndex = batch.Schema.GetFieldIndex(fieldName); }
      catch (InvalidOperationException)
      {
        columnIndex = -1;
      }
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
  /// Delegates to <see cref="BuildArrayFromValues"/> after extracting
  /// the per-row property values, so the same value-array builder
  /// can be re-used recursively for <see cref="ListType"/> children.
  /// </summary>
  private static IArrowArray BuildArrayForProperty<T>(
    PropertyInfo property,
    Field field,
    List<T> rows,
    int rowCount
  )
  {
    var propertyType = property.PropertyType;
    var values = new List<object?>(rowCount);
    foreach (var row in rows)
    {
      values.Add(property.GetValue(row));
    }
    try
    {
      return BuildArrayFromValues(values, propertyType, property);
    }
    catch (NotSupportedException ex)
    {
      // Re-throw with the property name so the user can locate the offender
      // — the inner builder only knows the element type.
      throw new NotSupportedException(
        $"Property '{property.Name}' has type '{propertyType.Name}' which is not supported for Arrow marshalling: {ex.Message}",
        ex
      );
    }
  }

  /// <summary>
  /// Builds an Arrow array from a flat list of CLR values and the declared
  /// element type. This dispatcher handles the recursive shapes (Nullable&lt;T&gt;,
  /// enum, list/array); leaf-type encoding is delegated to
  /// <see cref="ArrowMarshallingRegistry"/>. The optional
  /// <paramref name="property"/> threads attribute-driven type parameters
  /// (e.g. <c>[ArrowDecimal]</c>) into the rule's <c>CreateArrowType</c>.
  /// </summary>
  private static IArrowArray BuildArrayFromValues(
    List<object?> values,
    Type elementType,
    PropertyInfo? property = null
  )
  {
    var underlyingType = Nullable.GetUnderlyingType(elementType) ?? elementType;

    var rule = ArrowMarshallingRegistry.TryGet(underlyingType);
    if (rule is not null)
    {
      var arrowType = rule.CreateArrowType(property);
      return rule.Encode(arrowType, values);
    }

    if (underlyingType.IsEnum) return BuildEnumArray(values, underlyingType);

    var listElementType = ArrowSchemaMapper.TryGetListElementType(underlyingType);
    if (listElementType is not null)
    {
      return BuildListArray(values, listElementType);
    }

    throw new NotSupportedException(
      $"Element type '{elementType.Name}' is not supported for Arrow marshalling. "
        + "Supported types: int, long, float, double, bool, string, DateTime, "
        + "DateTimeOffset, TimeSpan, Guid, byte[], enum, list/array of any "
        + "supported type, and their nullable variants."
    );
  }

  /// <summary>
  /// Build a variable-length <c>ListArray</c> from a sequence of per-row
  /// list values. Each row either contributes its full element sequence to
  /// the flattened child array (with an offset bump) or contributes a null
  /// entry (offset stays put, validity bit cleared). Recurses into
  /// <see cref="BuildArrayFromValues"/> to construct the child array, so
  /// nested lists work without special-casing.
  /// </summary>
  private static IArrowArray BuildListArray(List<object?> values, Type elementType)
  {
    var offsets = new ArrowBuffer.Builder<int>();
    var validity = new ArrowBuffer.BitmapBuilder();
    var flatChildren = new List<object?>();
    var currentOffset = 0;
    var nullCount = 0;

    offsets.Append(0);
    foreach (var listValue in values)
    {
      if (listValue is null)
      {
        validity.Append(false);
        nullCount++;
        offsets.Append(currentOffset);
        continue;
      }

      validity.Append(true);
      foreach (var elem in (System.Collections.IEnumerable)listValue)
      {
        flatChildren.Add(elem);
        currentOffset++;
      }
      offsets.Append(currentOffset);
    }

    var childArray = BuildArrayFromValues(flatChildren, elementType);

    // Construct the ListType using the actual child Arrow data type so the
    // outer Field nullability matches what ArrowSchemaMapper produced.
    var elementArrowType = childArray.Data.DataType;
    var listType = new ListType(new Field("item", elementArrowType, nullable: true));

    var listData = new ArrayData(
      listType,
      length: values.Count,
      nullCount: nullCount,
      offset: 0,
      buffers: new[] { validity.Build(), offsets.Build() },
      children: new[] { childArray.Data }
    );
    return new ListArray(listData);
  }

  private static IArrowArray BuildEnumArray(List<object?> values, Type enumType)
  {
    // Resolve [SerializedEnum] attribute values for the enum type, matching all other format serializers.
    var serializedValues = GetSerializedEnumMap(enumType);
    var builder = new StringArray.Builder();
    foreach (var value in values)
    {
      if (value is null) builder.AppendNull();
      else builder.Append(serializedValues[value]);
    }
    return builder.Build();
  }

  // ──────────────────────────────────────────────────────────────
  // Value Extraction (Arrow → C#)
  // ──────────────────────────────────────────────────────────────

  /// <summary>
  /// Extracts a value from an Arrow array at a specific index, converting to
  /// the target CLR type. This dispatcher handles recursive shapes
  /// (Nullable&lt;T&gt;, enum, list/array) and cross-type numeric widening;
  /// leaf-type decoding is delegated to <see cref="ArrowMarshallingRegistry"/>.
  /// </summary>
  private static object? GetValueFromArray(IArrowArray array, int index, Type targetType)
  {
    // Check for null
    if (array.IsNull(index))
    {
      return null;
    }

    var underlyingType = Nullable.GetUnderlyingType(targetType) ?? targetType;

    var rule = ArrowMarshallingRegistry.TryGet(underlyingType);
    if (rule is not null && rule.Matches(array))
    {
      return rule.Decode(array, index);
    }

    // Enum types: deserialized from their [SerializedEnum] string value
    if (underlyingType.IsEnum)
    {
      var enumString = array switch
      {
        StringArray stringArray => stringArray.GetString(index),
        LargeStringArray largeStringArray => largeStringArray.GetString(index),
        _ => throw new NotSupportedException(
          $"Cannot convert Arrow array of type '{array.Data.DataType.Name}' to enum type '{underlyingType.Name}'."
        ),
      };

      if (enumString == null)
      {
        return null;
      }

      var reverseMap = GetReverseSerializedEnumMap(underlyingType);
      if (!reverseMap.TryGetValue(enumString, out var enumValue))
      {
        throw new InvalidOperationException(
          $"Arrow value '{enumString}' does not match any [SerializedEnum] value on enum type '{underlyingType.Name}'."
        );
      }
      return enumValue;
    }

    // Numeric type coercion (pandas compatibility). These are cross-type
    // widenings, not registry entries — the rule for `long` only matches
    // Int64Array, so an Int32Array→long? read lands here.
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

    // List / array types: recursively read the slice [offset, offset+length)
    // out of the child values array. The slice's elements are projected
    // back into the requested CLR container (T[] or List<T>) so the C#
    // schema property gets the exact type it asked for.
    var listElementType = ArrowSchemaMapper.TryGetListElementType(underlyingType);
    if (listElementType is not null && array is ListArray listArray)
    {
      return ExtractListValue(listArray, index, underlyingType, listElementType);
    }

    throw new NotSupportedException(
      $"Cannot convert Arrow array of type '{array.Data.DataType.Name}' to C# type '{targetType.Name}'."
    );
  }

  /// <summary>
  /// Extract a single list-typed cell at <paramref name="rowIndex"/> from a
  /// <see cref="ListArray"/>. The slice's child values are decoded via
  /// <see cref="GetValueFromArray"/> — recursive: a nested
  /// <see cref="ListArray"/> in <c>listArray.Values</c> dispatches back
  /// here through the recursive call, so arbitrary nesting depth is read
  /// the same way it was written.
  /// </summary>
  private static object ExtractListValue(
    ListArray listArray,
    int rowIndex,
    Type containerType,
    Type elementType
  )
  {
    var offsets = listArray.ValueOffsets;
    var start = offsets[rowIndex];
    var end = offsets[rowIndex + 1];
    var length = end - start;

    var childValues = listArray.Values;
    var decoded = new object?[length];
    for (int i = 0; i < length; i++)
    {
      decoded[i] = GetValueFromArray(childValues, start + i, elementType);
    }

    return ProjectIntoContainer(decoded, containerType, elementType);
  }

  /// <summary>
  /// Project a decoded element sequence into the CLR container type the
  /// schema property declared — <c>T[]</c>, <c>List&lt;T&gt;</c>, or a
  /// generic interface like <c>IEnumerable&lt;T&gt;</c> /
  /// <c>IReadOnlyList&lt;T&gt;</c> (defaults to <c>List&lt;T&gt;</c>).
  /// </summary>
  private static object ProjectIntoContainer(object?[] decoded, Type containerType, Type elementType)
  {
    if (containerType.IsArray)
    {
      var arr = System.Array.CreateInstance(elementType, decoded.Length);
      for (int i = 0; i < decoded.Length; i++) arr.SetValue(decoded[i], i);
      return arr;
    }

    // Build a List<elementType> via reflection — it implements every common
    // collection interface (IList<T>, IEnumerable<T>, IReadOnlyList<T>, etc.).
    var listType = typeof(List<>).MakeGenericType(elementType);
    var list = (System.Collections.IList)Activator.CreateInstance(listType, decoded.Length)!;
    foreach (var v in decoded) list.Add(v);

    if (containerType.IsAssignableFrom(listType)) return list;

    // The declared container isn't List<T> and isn't an array — e.g. a
    // user-declared IReadOnlyCollection<T>. List<T> still satisfies it
    // because List<T> implements every standard read-only interface.
    return list;
  }

  // ──────────────────────────────────────────────────────────────
  // Enum Helpers ([SerializedEnum] attribute-driven, matching all other format serializers)
  // ──────────────────────────────────────────────────────────────

  private static readonly System.Collections.Concurrent.ConcurrentDictionary<
    Type,
    Dictionary<object, string>
  > _enumToStringCache = new();
  private static readonly System.Collections.Concurrent.ConcurrentDictionary<
    Type,
    Dictionary<string, object>
  > _stringToEnumCache = new();

  /// <summary>
  /// Builds a forward map (enum value → serialized string) from [SerializedEnum] attributes.
  /// Results are cached per enum type.
  /// </summary>
  private static Dictionary<object, string> GetSerializedEnumMap(Type enumType)
  {
    return _enumToStringCache.GetOrAdd(
      enumType,
      t =>
      {
        var map = new Dictionary<object, string>();
        foreach (var field in t.GetFields(BindingFlags.Public | BindingFlags.Static))
        {
          var attr = field.GetCustomAttribute<SerializedEnumAttribute>();
          if (attr == null)
          {
            throw new InvalidOperationException(
              $"Enum member '{t.Name}.{field.Name}' is missing the required [SerializedEnum] attribute."
            );
          }
          map[field.GetValue(null)!] = attr.Value;
        }
        return map;
      }
    );
  }

  /// <summary>
  /// Builds a reverse map (serialized string → enum value) from [SerializedEnum] attributes.
  /// Results are cached per enum type.
  /// </summary>
  private static Dictionary<string, object> GetReverseSerializedEnumMap(Type enumType)
  {
    return _stringToEnumCache.GetOrAdd(
      enumType,
      t =>
      {
        var forward = GetSerializedEnumMap(t);
        return forward.ToDictionary(kvp => kvp.Value, kvp => kvp.Key);
      }
    );
  }
}
