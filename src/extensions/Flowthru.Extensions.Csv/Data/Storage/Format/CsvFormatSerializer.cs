using System.Globalization;
using System.Reflection;
using System.Runtime.CompilerServices;
using CsvHelper;
using CsvHelper.Configuration;
using Flowthru.Core.Abstractions;
using Flowthru.Core.Data.Capabilities;

namespace Flowthru.Core.Data.Storage.Format;

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
/// <strong>Null Handling:</strong>
/// </para>
/// <para>
/// By default, empty cells in nullable properties (<c>string?</c>, <c>int?</c>,
/// <c>DateTime?</c>, etc.) deserialize to <c>null</c> — matching the conventional CSV
/// representation where <c>,,</c> indicates a missing value (the same convention pandas,
/// R, and most CSV consumers use). Non-nullable properties retain their type's default
/// behavior: <c>string</c> reads empty cells as empty strings, value types use CsvHelper
/// defaults.
/// </para>
/// <para>
/// Catalog authors can extend the set of null sentinels via the <c>nullValues</c>
/// constructor parameter — for example <c>["", "NA", "N/A", "NULL"]</c> to handle messy
/// real-world data. Nullability is detected per-property via <see cref="NullabilityInfoContext"/>;
/// the override applies only to properties declared nullable in the schema.
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
  where TRow : notnull, IFlatSchema, ITextSerializable
{

  private readonly CsvConfiguration _configuration;
  private readonly IReadOnlyList<string> _nullValues;

  /// <summary>
  /// Creates a new CSV format serializer with default configuration. Empty cells in
  /// nullable properties deserialize to null.
  /// </summary>
  public CsvFormatSerializer()
    : this(
      new CsvConfiguration(CultureInfo.InvariantCulture, typeof(TRow)) { HasHeaderRecord = true },
      CsvFormatSerializerDefaults.NullValues
    ) { }

  /// <summary>
  /// Creates a new CSV format serializer with a custom set of null-representation strings.
  /// </summary>
  /// <param name="nullValues">
  /// Strings that should deserialize to null for nullable properties. Pass
  /// <c>["", "NA", "N/A", "NULL"]</c> for pandas-style handling of messy data. The first
  /// entry — typically <see cref="string.Empty"/> — is also used as the canonical write
  /// representation when a nullable property's value is null.
  /// </param>
  public CsvFormatSerializer(IReadOnlyList<string> nullValues)
    : this(
      new CsvConfiguration(CultureInfo.InvariantCulture, typeof(TRow)) { HasHeaderRecord = true },
      nullValues
    ) { }

  /// <summary>
  /// Creates a new CSV format serializer with custom configuration.
  /// </summary>
  /// <param name="configuration">CsvHelper configuration</param>
  /// <exception cref="ArgumentNullException">Thrown if configuration is null</exception>
  public CsvFormatSerializer(CsvConfiguration configuration)
    : this(configuration, CsvFormatSerializerDefaults.NullValues) { }

  /// <summary>
  /// Creates a new CSV format serializer with custom configuration and null-representation
  /// strings.
  /// </summary>
  /// <param name="configuration">CsvHelper configuration</param>
  /// <param name="nullValues">Strings that should deserialize to null for nullable properties.</param>
  /// <exception cref="ArgumentNullException">Thrown if either argument is null.</exception>
  public CsvFormatSerializer(CsvConfiguration configuration, IReadOnlyList<string> nullValues)
  {
    _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
    _nullValues = nullValues ?? throw new ArgumentNullException(nameof(nullValues));
  }

  /// <summary>
  /// Gets the CSV configuration for this serializer.
  /// </summary>
  public CsvConfiguration Configuration => _configuration;

  /// <summary>
  /// Gets the null-representation strings for this serializer.
  /// </summary>
  public IReadOnlyList<string> NullValues => _nullValues;

  /// <inheritdoc/>
  public StorageTraits Traits => new StorageTraits { CanStream = true };

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

    // Register SerializedLabel-aware class map with nullability handling.
    csv.Context.RegisterClassMap(new SerializedLabelClassMap<TRow>(_nullValues));

    // Stream records one at a time. Translate CsvHelper's HeaderValidationException
    // into Core's SchemaMismatchException so pre-flight surfaces a structural
    // mismatch (missing/renamed schema column) as ValidationErrorType.SchemaMismatch
    // rather than the generic InspectionFailure / DeserializationError. The
    // wrapping must happen here at the provider boundary; Core can't reference
    // CsvHelper directly. See Phase F in docs/scratch/extension-conformance-kits.md.
    var enumerator = csv.GetRecordsAsync<TRow>().GetAsyncEnumerator();
    try
    {
      while (true)
      {
        bool hasMore;
        try
        {
          hasMore = await enumerator.MoveNextAsync();
        }
        catch (CsvHelper.HeaderValidationException ex)
        {
          throw new Flowthru.Core.Data.Validation.SchemaMismatchException(
            $"CSV header does not match schema '{typeof(TRow).Name}': {ex.Message.Split('\n')[0]}",
            ex
          );
        }

        if (!hasMore)
        {
          yield break;
        }

        yield return enumerator.Current;
      }
    }
    finally
    {
      await enumerator.DisposeAsync();
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

    // Register SerializedLabel-aware class map with nullability handling.
    csv.Context.RegisterClassMap(new SerializedLabelClassMap<TRow>(_nullValues));

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
/// Constants shared across <see cref="CsvFormatSerializer{TRow}"/> instantiations.
/// </summary>
public static class CsvFormatSerializerDefaults
{
  /// <summary>The default set of strings treated as null on read for nullable properties.</summary>
  public static readonly IReadOnlyList<string> NullValues = new[] { string.Empty };
}

/// <summary>
/// CsvHelper class map that uses SerializedLabel attributes for field name mapping,
/// SerializedEnum attributes for enum value conversion, and a configurable null-value list
/// for nullable properties.
/// </summary>
/// <typeparam name="T">The row type to map</typeparam>
internal sealed class SerializedLabelClassMap<T> : ClassMap<T>
{
  /// <summary>Default constructor — empty cells are the only null sentinel.</summary>
  public SerializedLabelClassMap()
    : this(CsvFormatSerializerDefaults.NullValues) { }

  /// <summary>
  /// Creates a class map with a custom null-value list. Properties declared nullable in the
  /// schema (via <c>string?</c> or <c>T?</c>) treat each entry of <paramref name="nullValues"/>
  /// as null on read.
  /// </summary>
  public SerializedLabelClassMap(IReadOnlyList<string> nullValues)
  {
    var nullabilityContext = new NullabilityInfoContext();
    var properties = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance);

    foreach (var property in properties)
    {
      var fieldName = PropertyMappingHelper.GetFieldName(property);
      var memberMap = Map(typeof(T), property).Name(fieldName);

      // Add enum converter if property type is an enum
      if (property.PropertyType.IsEnum)
      {
        var converterType = typeof(SerializedEnumCsvConverter<>).MakeGenericType(
          property.PropertyType
        );
        var converter = Activator.CreateInstance(converterType);
        memberMap.TypeConverter((CsvHelper.TypeConversion.ITypeConverter)converter!);
      }

      if (IsNullable(property, nullabilityContext))
      {
        // Apply null-value sentinels for nullable properties only. Non-nullable string
        // fields keep "" as empty-string semantics; nullable fields treat "" (and any
        // additional sentinels) as null.
        memberMap.TypeConverterOption.NullValues(nullValues.ToArray());
      }
    }
  }

  /// <summary>
  /// Determines whether a property is declared nullable. For value types this is
  /// <c>Nullable&lt;T&gt;</c>; for reference types this reads the C# 8 nullability
  /// annotation via <see cref="NullabilityInfoContext"/>.
  /// </summary>
  private static bool IsNullable(PropertyInfo property, NullabilityInfoContext context)
  {
    if (Nullable.GetUnderlyingType(property.PropertyType) is not null)
    {
      return true;
    }

    if (property.PropertyType.IsValueType)
    {
      return false;
    }

    var info = context.Create(property);
    return info.ReadState == NullabilityState.Nullable;
  }
}

/// <summary>
/// CsvHelper type converter that respects SerializedEnum attributes for enum value conversion.
/// </summary>
/// <typeparam name="TEnum">The enum type to convert</typeparam>
internal sealed class SerializedEnumCsvConverter<TEnum>
  : CsvHelper.TypeConversion.DefaultTypeConverter
  where TEnum : struct, Enum
{
  private readonly Serialization.EnumMetadataCache<TEnum> _metadata;

  public SerializedEnumCsvConverter()
  {
    _metadata = Serialization.EnumMetadataRegistry.Create<TEnum>();
  }

  public override object? ConvertFromString(
    string? text,
    CsvHelper.IReaderRow row,
    CsvHelper.Configuration.MemberMapData memberMapData
  )
  {
    if (string.IsNullOrWhiteSpace(text))
    {
      // Return default value for empty/null strings
      return default(TEnum);
    }

    try
    {
      return _metadata.Parse(text);
    }
    catch (InvalidOperationException ex)
    {
      throw new CsvHelper.TypeConversion.TypeConverterException(
        this,
        memberMapData,
        text,
        row.Context,
        $"Failed to convert '{text}' to enum type '{typeof(TEnum).Name}'. {ex.Message}"
      );
    }
  }

  public override string? ConvertToString(
    object? value,
    CsvHelper.IWriterRow row,
    CsvHelper.Configuration.MemberMapData memberMapData
  )
  {
    if (value == null)
    {
      return string.Empty;
    }

    try
    {
      return _metadata.ToString((TEnum)value);
    }
    catch (InvalidOperationException ex)
    {
      throw new CsvHelper.TypeConversion.TypeConverterException(
        this,
        memberMapData,
        value,
        row.Context,
        $"Failed to convert enum value '{value}' of type '{typeof(TEnum).Name}' to string. {ex.Message}"
      );
    }
  }
}
