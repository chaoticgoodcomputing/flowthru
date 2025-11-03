using System.Globalization;
using System.Reflection;
using System.Runtime.CompilerServices;
using CsvHelper;
using CsvHelper.Configuration;
using Flowthru.Abstractions;

namespace Flowthru.Data.Storage.Format;

/// <summary>
/// Format serializer for CSV (Comma-Separated Values) files.
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
/// <item><see cref="IFlatSchema"/> - No nested structures (primitives only)</item>
/// <item><see cref="ITextSerializable"/> - Can be serialized to text</item>
/// </list>
/// <para>
/// These constraints are enforced at compile-time, preventing invalid usage:
/// </para>
/// <code>
/// // ✅ Compiles - flat schema with text serialization
/// var csv = new CsvFormatSerializer&lt;CompanySchema&gt;();
///
/// // ❌ Compile error - nested schema not allowed
/// var csv = new CsvFormatSerializer&lt;OrderWithItems&gt;(); // OrderWithItems : INestedSchema
/// </code>
/// <para>
/// <strong>Configuration:</strong>
/// </para>
/// <para>
/// Uses CsvHelper library with default configuration:
/// - HasHeaderRecord = true
/// - InvariantCulture
/// - Comma delimiter
/// </para>
/// <para>
/// Custom configuration can be provided via constructor.
/// </para>
/// <para>
/// <strong>Streaming Behavior:</strong>
/// </para>
/// <para>
/// Both deserialization and serialization use streaming:
/// - Rows are yielded/consumed lazily
/// - Low memory footprint for large files
/// - Backpressure support via IAsyncEnumerable
/// </para>
/// </remarks>
/// <example>
/// <code>
/// public record CompanySchema(
///     int Id,
///     string Name,
///     float Rating
/// ) : IFlatSchema, ITextSerializable;
///
/// var serializer = new CsvFormatSerializer&lt;CompanySchema&gt;();
///
/// // Deserialize
/// using var readStream = File.OpenRead("companies.csv");
/// await foreach (var row in serializer.DeserializeRows(readStream))
/// {
///     Console.WriteLine($"Company: {row.Name}, Rating: {row.Rating}");
/// }
///
/// // Serialize
/// var companies = new[] {
///     new CompanySchema(1, "Acme Corp", 4.5f),
///     new CompanySchema(2, "Tech Inc", 4.8f)
/// };
///
/// using var writeStream = File.Create("output.csv");
/// await serializer.SerializeRows(writeStream, companies.ToAsyncEnumerable());
/// </code>
/// </example>
public sealed class CsvFormatSerializer<TRow> : IFormatSerializer<TRow>
  where TRow : IFlatSchema, ITextSerializable
{
  private readonly CsvConfiguration _configuration;

  /// <summary>
  /// Creates a new CSV format serializer with default configuration.
  /// </summary>
  public CsvFormatSerializer()
    : this(
      new CsvConfiguration(CultureInfo.InvariantCulture, typeof(TRow)) { HasHeaderRecord = true }
    ) { }

  /// <summary>
  /// Creates a new CSV format serializer with custom configuration.
  /// </summary>
  /// <param name="configuration">CsvHelper configuration</param>
  /// <exception cref="ArgumentNullException">Thrown if configuration is null</exception>
  public CsvFormatSerializer(CsvConfiguration configuration)
  {
    _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
  }

  /// <summary>
  /// Gets the CSV configuration for this serializer.
  /// </summary>
  public CsvConfiguration Configuration => _configuration;

  /// <inheritdoc/>
  public async IAsyncEnumerable<TRow> DeserializeRows(Stream stream)
  {
    if (stream == null)
    {
      throw new ArgumentNullException(nameof(stream));
    }

    // Create reader with UTF-8 encoding (leaveOpen: true so caller can manage stream lifecycle)
    using var reader = new StreamReader(stream, leaveOpen: true);
    using var csv = new CsvReader(reader, _configuration);

    // Register SerializedLabel-aware class map
    csv.Context.RegisterClassMap<SerializedLabelClassMap<TRow>>();

    // Stream records one at a time
    await foreach (var record in csv.GetRecordsAsync<TRow>())
    {
      yield return record;
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

    // Create writer with UTF-8 encoding (leaveOpen: true so caller can manage stream lifecycle)
    await using var writer = new StreamWriter(stream, leaveOpen: true);
    await using var csv = new CsvWriter(writer, _configuration);

    // Register SerializedLabel-aware class map
    csv.Context.RegisterClassMap<SerializedLabelClassMap<TRow>>();

    // Write header (if configured)
    if (_configuration.HasHeaderRecord)
    {
      csv.WriteHeader<TRow>();
      await csv.NextRecordAsync();
    }

    // Write rows as they arrive
    await foreach (var row in rows)
    {
      csv.WriteRecord(row);
      await csv.NextRecordAsync();
    }

    await csv.FlushAsync();
  }

  /// <inheritdoc/>
  public PropertyMappingConfiguration GetPropertyMappingConfiguration()
  {
    return PropertyMappingConfiguration.FromSerializedLabel<TRow>();
  }
}

/// <summary>
/// CsvHelper class map that uses SerializedLabel attributes for field name mapping.
/// </summary>
/// <typeparam name="T">The row type to map</typeparam>
internal sealed class SerializedLabelClassMap<T> : ClassMap<T>
{
  public SerializedLabelClassMap()
  {
    // Map each property to its external field name (from SerializedLabel or property name)
    var properties = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance);

    foreach (var property in properties)
    {
      var fieldName = PropertyMappingHelper.GetFieldName(property);
      Map(typeof(T), property).Name(fieldName);
    }
  }
}
