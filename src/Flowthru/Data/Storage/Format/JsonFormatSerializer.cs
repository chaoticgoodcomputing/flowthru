using System.Runtime.CompilerServices;
using System.Text.Json;
using Flowthru.Abstractions;

namespace Flowthru.Data.Storage.Format;

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
  where TRow : IStructuredSerializable
{
  private readonly JsonSerializerOptions _options;

  /// <summary>
  /// Creates a new JSON format serializer with default configuration.
  /// </summary>
  public JsonFormatSerializer()
    : this(
      new JsonSerializerOptions
      {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
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
  }

  /// <summary>
  /// Gets the JSON serialization options for this serializer.
  /// </summary>
  public JsonSerializerOptions Options => _options;

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
}
