using System.Globalization;
using System.Reflection;
using System.Runtime.CompilerServices;
using CsvHelper;
using CsvHelper.Configuration;
using Flowthru.Core.Abstractions;
using Flowthru.Core.Data.Capabilities;
using Flowthru.Core.Data.Serialization;
using Flowthru.Core.Serialization;

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
/// <remarks>
/// <para>
/// Consumes <see cref="PropertyMappingPlanner"/> to drive its class map. The planner
/// classifies each property (primitive / enum / IScalar / nested) and reports
/// nullability, field-name override, and per-kind metadata. This class translates the
/// plan into CsvHelper's <c>ClassMap</c> abstractions:
/// </para>
/// <list type="bullet">
/// <item>For types with a parameterless constructor: <c>Map(...)</c> per property.</item>
/// <item>For positional records (primary-constructor-only types): <c>Parameter(...)</c>
/// per constructor parameter, matched to a planner binding by name. This closes the
/// CsvHelper positional-record gap surfaced during the Phase A foundation pass.</item>
/// </list>
/// </remarks>
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
    var plan = PropertyMappingPlanner.Build<T>(
      new PropertyMappingPlannerOptions { NullSentinels = nullValues }
    );

    // Register property-side maps unconditionally. These cover the writer path for both
    // parameterless-constructor types and positional records (record's compiler-
    // synthesized properties have getters), and cover the reader path for types with a
    // parameterless constructor.
    foreach (var binding in plan.Bindings)
    {
      var memberMap = Map(typeof(T), binding.Property).Name(binding.FieldName);
      ApplyConverter(memberMap, binding);
      ApplyNullSentinels(memberMap, binding);
    }

    // For positional records (no parameterless constructor), additionally register
    // ParameterMaps so CsvHelper's reader can bind cells to the primary constructor's
    // parameters and instantiate the type. This is the path closed by Phase B2 — the
    // planner's per-property metadata drives both Member (write) and Parameter (read)
    // registrations.
    var hasParameterlessCtor = typeof(T).GetConstructor(Type.EmptyTypes) is not null;
    if (!hasParameterlessCtor)
    {
      var primaryCtor = typeof(T)
        .GetConstructors(BindingFlags.Public | BindingFlags.Instance)
        .OrderByDescending(c => c.GetParameters().Length)
        .First();

      foreach (var param in primaryCtor.GetParameters())
      {
        // Records emit primary-constructor parameters with names matching the synthesized
        // properties verbatim. Match by property name; falling back to parameter name
        // only if the property lookup fails.
        var binding = plan.Bindings.FirstOrDefault(b =>
          string.Equals(b.Property.Name, param.Name, StringComparison.Ordinal)
        );
        if (binding is null)
        {
          continue; // unusual; let CsvHelper handle via defaults
        }

        var parameterMap = Parameter(() => primaryCtor, param.Name!).Name(binding.FieldName);
        ApplyConverter(parameterMap, binding);
        ApplyNullSentinels(parameterMap, binding);
      }
    }
  }

  // ── Per-binding wiring helpers ────────────────────────────────────────────────

  private static void ApplyConverter(MemberMap memberMap, PropertyBinding binding)
  {
    var converter = BuildConverter(binding);
    if (converter is not null)
    {
      memberMap.TypeConverter(converter);
    }
  }

  private static void ApplyConverter(ParameterMap parameterMap, PropertyBinding binding)
  {
    var converter = BuildConverter(binding);
    if (converter is not null)
    {
      parameterMap.TypeConverter(converter);
    }
  }

  private static void ApplyNullSentinels(MemberMap memberMap, PropertyBinding binding)
  {
    if (binding.IsNullable && binding.NullSentinels.Count > 0)
    {
      memberMap.TypeConverterOption.NullValues(binding.NullSentinels.ToArray());
    }
  }

  private static void ApplyNullSentinels(ParameterMap parameterMap, PropertyBinding binding)
  {
    if (binding.IsNullable && binding.NullSentinels.Count > 0)
    {
      parameterMap.TypeConverterOption.NullValues(binding.NullSentinels.ToArray());
    }
  }

  private static CsvHelper.TypeConversion.ITypeConverter? BuildConverter(PropertyBinding binding)
  {
    switch (binding.Kind)
    {
      case PropertyKind.Enum:
      {
        var converterType = typeof(SerializedEnumCsvConverter<>).MakeGenericType(
          binding.EffectiveType
        );
        return (CsvHelper.TypeConversion.ITypeConverter)Activator.CreateInstance(converterType)!;
      }
      case PropertyKind.IScalar:
      {
        var info = binding.IScalar!;
        var converterType = typeof(IScalarCsvConverter<,>).MakeGenericType(
          info.ScalarType,
          info.BackingType
        );
        return (CsvHelper.TypeConversion.ITypeConverter)
          Activator.CreateInstance(converterType, info.ValueProperty.Name)!;
      }
      case PropertyKind.Primitive:
        // CsvHelper handles primitives natively via its built-in TypeConverterCache.
        return null;
      case PropertyKind.Nested:
        // CsvFormatSerializer's generic constraint on IFlatSchema prevents nested-bearing
        // schemas from compiling here — receiving a Nested binding indicates a bug
        // upstream. Returning null falls through to CsvHelper's default converter, which
        // will produce a clear error if nested data unexpectedly reaches this point.
        return null;
      default:
        return null;
    }
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
  private readonly EnumMetadataCache<TEnum> _metadata;

  public SerializedEnumCsvConverter()
  {
    _metadata = EnumMetadataRegistry.Create<TEnum>();
  }

  public override object? ConvertFromString(
    string? text,
    CsvHelper.IReaderRow row,
    CsvHelper.Configuration.MemberMapData memberMapData
  )
  {
    // Honor the member's null-sentinel configuration. CsvHelper auto-applies NullValues
    // for built-in nullable converters; for custom converters like this one the converter
    // must consult MemberMapData itself. When the member is nullable and the cell matches
    // a configured sentinel, return null (CsvHelper unboxes to TEnum? on the property
    // setter); for non-nullable enum members, returning null causes CsvHelper to surface
    // a clear "cannot convert null" error rather than silently substituting default(TEnum).
    var nullValues = memberMapData.TypeConverterOptions?.NullValues;
    if (text is not null && nullValues is not null && nullValues.Contains(text))
    {
      return null;
    }

    if (string.IsNullOrEmpty(text))
    {
      return null;
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

/// <summary>
/// CsvHelper type converter for <see cref="IScalar"/> NewType wrappers (e.g. record struct
/// <c>CustomerId(string Value) : IScalar</c>). Reads/writes the cell as the backing type
/// and constructs the wrapper via its single-arg constructor.
/// </summary>
internal sealed class IScalarCsvConverter<TScalar, TBacking>
  : CsvHelper.TypeConversion.DefaultTypeConverter
  where TScalar : IScalar
{
  private readonly PropertyInfo _valueProperty;
  private readonly ConstructorInfo _wrappingConstructor;

  public IScalarCsvConverter(string valuePropertyName)
  {
    _valueProperty =
      typeof(TScalar).GetProperty(valuePropertyName, BindingFlags.Public | BindingFlags.Instance)
      ?? throw new InvalidOperationException(
        $"IScalar type '{typeof(TScalar).Name}' does not expose a public '{valuePropertyName}' property."
      );

    _wrappingConstructor =
      typeof(TScalar).GetConstructor(new[] { typeof(TBacking) })
      ?? throw new InvalidOperationException(
        $"IScalar type '{typeof(TScalar).Name}' does not expose a constructor taking a single '{typeof(TBacking).Name}' argument."
      );
  }

  public override object? ConvertFromString(
    string? text,
    CsvHelper.IReaderRow row,
    CsvHelper.Configuration.MemberMapData memberMapData
  )
  {
    var backingConverter = row.Context.TypeConverterCache.GetConverter(typeof(TBacking));
    var rawValue = backingConverter.ConvertFromString(text, row, memberMapData);
    return _wrappingConstructor.Invoke(new[] { rawValue });
  }

  public override string? ConvertToString(
    object? value,
    CsvHelper.IWriterRow row,
    CsvHelper.Configuration.MemberMapData memberMapData
  )
  {
    if (value is null)
    {
      return string.Empty;
    }

    var raw = _valueProperty.GetValue(value);
    var backingConverter = row.Context.TypeConverterCache.GetConverter(typeof(TBacking));
    return backingConverter.ConvertToString(raw, row, memberMapData);
  }
}
