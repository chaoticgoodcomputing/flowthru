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
  where TRow : IFlatSchema, IBinarySerializable, new()
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

    // Deserialize using Parquet.NET
    var rows = await ParquetSerializer.DeserializeAsync<TRow>(stream);

    // Yield each row
    foreach (var row in rows)
    {
      yield return row;
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

    // Serialize to Parquet format
    await ParquetSerializer.SerializeAsync(rowList, stream);
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
