using System.Reflection;
using System.Runtime.CompilerServices;
using Flowthru.Abstractions;
using Parquet;
using Parquet.Schema;
using Parquet.Serialization;

namespace Flowthru.Data.Storage.Format;

/// <summary>
/// Format serializer for Parquet (columnar storage) files.
/// </summary>
/// <typeparam name="TRow">The row schema type</typeparam>
/// <remarks>
/// <para>
/// <strong>Type Constraints:</strong>
/// </para>
/// <para>
/// TRow must implement both:
/// </para>
/// <list type="bullet">
/// <item><see cref="IFlatSchema"/> - Flat structure (optimal for Parquet)</item>
/// <item><see cref="IBinarySerializable"/> - Can be serialized to binary format</item>
/// </list>
/// <para>
/// <strong>SerializedLabel Support:</strong>
/// </para>
/// <para>
/// This serializer respects <see cref="SerializedLabelAttribute"/> for field name mapping.
/// The Parquet.NET library's native column mapping is configured programmatically based on
/// the SerializedLabel attributes, ensuring consistent behavior with other Flowthru serializers.
/// </para>
/// <para>
/// Parquet is optimized for flat, columnar data and provides:
/// - Excellent compression ratios
/// - Fast columnar reads
/// - Type preservation (no string conversion)
/// - Predicate pushdown capabilities
/// </para>
/// <para>
/// <strong>When to Use Parquet:</strong>
/// </para>
/// <list type="bullet">
/// <item>Large datasets (&gt;10MB)</item>
/// <item>Analytical workloads</item>
/// <item>Data lake storage</item>
/// <item>ML feature stores</item>
/// </list>
/// <para>
/// <strong>Streaming Behavior:</strong>
/// </para>
/// <para>
/// Parquet serialization has different characteristics:
/// </para>
/// <list type="bullet">
/// <item><strong>Deserialization:</strong> Streams rows lazily</item>
/// <item><strong>Serialization:</strong> Buffers rows for optimal columnar encoding</item>
/// </list>
/// </remarks>
/// <example>
/// <code>
/// public record FeatureRow(
///     DateTime Timestamp,
///     double Feature1,
///     double Feature2,
///     int Label
/// ) : IFlatSchema, IBinarySerializable;
///
/// var serializer = new ParquetFormatSerializer&lt;FeatureRow&gt;();
///
/// // Serialize to Parquet
/// var features = GenerateFeatures();
/// using var writeStream = File.Create("features.parquet");
/// await serializer.SerializeRows(writeStream, features);
///
/// // Deserialize from Parquet
/// using var readStream = File.OpenRead("features.parquet");
/// await foreach (var row in serializer.DeserializeRows(readStream))
/// {
///     Console.WriteLine($"Feature1: {row.Feature1}, Label: {row.Label}");
/// }
/// </code>
/// </example>
public sealed class ParquetFormatSerializer<TRow> : IFormatSerializer<TRow>
  where TRow : notnull, IFlatSchema, IBinarySerializable
{
  /// <summary>
  /// Creates a new Parquet format serializer.
  /// </summary>
  /// <remarks>
  /// <para>
  /// <strong>Note on SerializedLabel:</strong> Parquet.NET's serialization API currently
  /// does not expose programmatic field name mapping. For Parquet files, property names
  /// must match the column names in the file. Consider using CSV or JSON formats if
  /// field name mapping via SerializedLabel is required, or ensure property names match
  /// Parquet column names exactly.
  /// </para>
  /// <para>
  /// <strong>Note on SerializedEnum:</strong> Parquet.NET's serialization does not provide
  /// hooks for custom enum conversion. Enums are serialized using their underlying integer
  /// values or .NET's default ToString() behavior. To use SerializedEnum attributes with
  /// Parquet files, consider:
  /// </para>
  /// <list type="bullet">
  /// <item>Pre-converting enum columns to strings before serialization</item>
  /// <item>Using CSV or JSON formats which support SerializedEnum</item>
  /// <item>Post-processing Parquet data after deserialization</item>
  /// </list>
  /// <para>
  /// These are known limitations that may be addressed in future versions as Parquet.NET
  /// evolves or if we implement custom serialization logic.
  /// </para>
  /// </remarks>
  public ParquetFormatSerializer() { }

  /// <inheritdoc/>
  public async IAsyncEnumerable<TRow> DeserializeRows(Stream stream)
  {
    if (stream == null)
    {
      throw new ArgumentNullException(nameof(stream));
    }

    // Use low-level ParquetReader API to support types without parameterless constructors
    using var reader = await ParquetReader.CreateAsync(stream);

    // Get type properties for mapping
    var properties = typeof(TRow)
      .GetProperties(BindingFlags.Public | BindingFlags.Instance)
      .Where(p => p.CanWrite)
      .ToList();

    // Build property-to-field mapping using SerializedLabel attributes
    var propertyMap = properties.ToDictionary(p => PropertyMappingHelper.GetFieldName(p), p => p);

    // Read all row groups
    for (int groupIndex = 0; groupIndex < reader.RowGroupCount; groupIndex++)
    {
      using var groupReader = reader.OpenRowGroupReader(groupIndex);

      // Read column data for all fields
      var columnData = new Dictionary<string, Array>();
      foreach (var fieldName in propertyMap.Keys)
      {
        try
        {
          // Find the field in the Parquet schema
          var field = reader.Schema.GetDataFields().FirstOrDefault(f => f.Name == fieldName);
          if (field != null)
          {
            var column = await groupReader.ReadColumnAsync(field);
            columnData[fieldName] = column.Data;
          }
        }
        catch
        {
          // Field not found in file - will remain uninitialized
          // Validation phase should catch missing required fields
        }
      }

      // Construct instances row by row
      var rowCount = groupReader.RowCount;
      for (int rowIndex = 0; rowIndex < rowCount; rowIndex++)
      {
        // Create instance using SchemaActivator (supports required members)
        var instance = SchemaActivator.CreateInstance<TRow>();

        // Populate properties from column data
        foreach (var (fieldName, property) in propertyMap)
        {
          if (columnData.TryGetValue(fieldName, out var colData) && rowIndex < colData.Length)
          {
            var value = colData.GetValue(rowIndex);

            // Handle property type conversions if needed
            if (value != null && property.PropertyType != value.GetType())
            {
              // For nullable types, extract the underlying type
              var targetType =
                Nullable.GetUnderlyingType(property.PropertyType) ?? property.PropertyType;

              try
              {
                value = Convert.ChangeType(value, targetType);
              }
              catch
              {
                // Type conversion failed - leave as null/default
                continue;
              }
            }

            property.SetValue(instance, value);
          }
        }

        yield return instance;
      }
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

    // Collect all rows (Parquet needs full dataset for optimal columnar encoding)
    var rowList = new List<TRow>();
    await foreach (var row in rows)
    {
      rowList.Add(row);
    }

    if (rowList.Count == 0)
    {
      // Write empty Parquet file with schema only
      var emptySchema = BuildParquetSchema();
      using var emptyWriter = await ParquetWriter.CreateAsync(emptySchema, stream);
      return;
    }

    // Build property map using SerializedLabel attributes
    var properties = typeof(TRow)
      .GetProperties(BindingFlags.Public | BindingFlags.Instance)
      .Where(p => p.CanRead)
      .ToList();

    var propertyMap = properties.ToDictionary(p => PropertyMappingHelper.GetFieldName(p), p => p);

    // Build Parquet schema from property map
    var schema = BuildParquetSchema(propertyMap);

    // Write data using low-level API
    using var writer = await ParquetWriter.CreateAsync(schema, stream);
    using var groupWriter = writer.CreateRowGroup();

    // Write each column
    foreach (var (fieldName, property) in propertyMap)
    {
      var field = schema.GetDataFields().First(f => f.Name == fieldName);

      // Create properly typed array using reflection
      var elementType = property.PropertyType;
      var columnData = Array.CreateInstance(elementType, rowList.Count);
      for (int i = 0; i < rowList.Count; i++)
      {
        columnData.SetValue(property.GetValue(rowList[i]), i);
      }

      await groupWriter.WriteColumnAsync(new Parquet.Data.DataColumn(field, columnData));
    }
  }

  /// <summary>
  /// Builds a Parquet schema from property mappings.
  /// </summary>
  private ParquetSchema BuildParquetSchema(Dictionary<string, PropertyInfo>? propertyMap = null)
  {
    if (propertyMap == null)
    {
      // Build default property map
      var properties = typeof(TRow)
        .GetProperties(BindingFlags.Public | BindingFlags.Instance)
        .Where(p => p.CanRead)
        .ToList();

      propertyMap = properties.ToDictionary(p => PropertyMappingHelper.GetFieldName(p), p => p);
    }

    var fields = new List<DataField>();
    foreach (var (fieldName, property) in propertyMap)
    {
      // Use property type directly - DataField constructor handles nullable/array detection
      var field = new DataField(fieldName, property.PropertyType);
      fields.Add(field);
    }

    return new ParquetSchema(fields);
  }

  /// <inheritdoc/>
  public PropertyMappingConfiguration GetPropertyMappingConfiguration()
  {
    return PropertyMappingConfiguration.LibraryControlled(
      "Parquet.NET does not expose programmatic field name mapping API. "
        + "Property names must match Parquet column names exactly. "
        + "[SerializedLabel] attributes are not supported."
    );
  }
}
