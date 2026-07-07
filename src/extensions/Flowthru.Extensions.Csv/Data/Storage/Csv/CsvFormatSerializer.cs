using System.Globalization;
using System.Runtime.CompilerServices;
using CsvHelper;
using CsvHelper.Configuration;
using Flowthru.Data.Schema;
using Flowthru.Data.Storage.Csv.Internal;

namespace Flowthru.Data.Storage.Csv;

/// <summary>
/// Format serializer for CSV (Comma-Separated Values) over a flat row
/// schema. Streaming on both sides — rows are yielded as deserialized
/// and serialized as consumed, so files larger than memory round-trip
/// without materialisation.
/// </summary>
/// <typeparam name="TRow">
/// Row schema. Must implement <see cref="IFlatSchema"/> (no nested
/// objects or collections) and <see cref="ITextSerializable"/>; both
/// markers are emitted by the <c>[FlowthruSchema]</c> source generator
/// for flat schemas.
/// </typeparam>
/// <remarks>
/// <para>
/// <strong>Capability claims:</strong> implements <see cref="IFormatStreamReader{TRow}"/>
/// (genuinely streaming via <c>CsvHelper</c>'s forward-only cursor) and
/// <see cref="ISupportsIScalar"/> (NewType wrappers round-trip via the
/// shared <c>PropertyMappingPlanner</c>). Does not implement
/// <see cref="ISupportsNested"/> — the <see cref="IFlatSchema"/>
/// constraint structurally rules out nested data at the call site.
/// </para>
/// <para>
/// <strong>Null handling:</strong> empty cells in nullable properties
/// (<c>string?</c>, <c>int?</c>, <c>DateTime?</c>) deserialize to
/// <c>null</c> by default — the conventional CSV "missing value"
/// semantics that pandas, R, and most CSV consumers use. Catalog authors
/// can extend the set of null sentinels via the <c>nullValues</c>
/// constructor parameter (e.g. <c>["", "NA", "N/A", "NULL"]</c> for
/// pandas-style messy-data handling). Nullability is detected per-
/// property; the override only applies to properties declared nullable.
/// </para>
/// <para>
/// <strong>Schema-mismatch translation:</strong>
/// <c>HeaderValidationException</c> from <c>CsvHelper</c> is
/// re-thrown as <see cref="SchemaMismatchException"/> so
/// <see cref="ComposedStorageAdapter{TContainer, TRow}"/>'s pre-flight
/// classifier surfaces it as <see cref="ValidationErrorType.SchemaMismatch"/>
/// rather than the generic deserialization-error variant. The
/// translation lands at the provider boundary; Core stays agnostic of
/// CsvHelper's exception hierarchy.
/// </para>
/// </remarks>
public sealed class CsvFormatSerializer<TRow>
  : IFormatSerializer<TRow>, IFormatStreamReader<TRow>, ISupportsIScalar
  where TRow : notnull, IFlatSchema, ITextSerializable
{
  private readonly CsvConfiguration _configuration;
  private readonly IReadOnlyList<string> _nullValues;

  /// <summary>
  /// Default configuration: <c>HasHeaderRecord = true</c>, invariant
  /// culture, comma delimiter. Empty cells round-trip as <c>null</c>
  /// for nullable properties.
  /// </summary>
  public CsvFormatSerializer()
    : this(BuildDefaultConfiguration(), CsvFormatSerializerDefaults.NullValues) { }

  /// <summary>
  /// Default configuration with a custom null-sentinel list. Pass
  /// <c>["", "NA", "N/A", "NULL"]</c> for pandas-style messy-data
  /// handling. The first entry is the canonical write-side null
  /// representation.
  /// </summary>
  public CsvFormatSerializer(IReadOnlyList<string> nullValues)
    : this(BuildDefaultConfiguration(), nullValues) { }

  /// <summary>Custom CsvHelper configuration with default null-sentinels.</summary>
  public CsvFormatSerializer(CsvConfiguration configuration)
    : this(configuration, CsvFormatSerializerDefaults.NullValues) { }

  /// <summary>Custom CsvHelper configuration with custom null-sentinels.</summary>
  public CsvFormatSerializer(CsvConfiguration configuration, IReadOnlyList<string> nullValues)
  {
    _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
    _nullValues = nullValues ?? throw new ArgumentNullException(nameof(nullValues));
  }

  /// <summary>The CsvHelper configuration in use.</summary>
  public CsvConfiguration Configuration => _configuration;

  /// <summary>The null-sentinel list applied to nullable properties on read.</summary>
  public IReadOnlyList<string> NullValues => _nullValues;

  /// <inheritdoc/>
  public StorageTraits Traits => new() { CanStream = true };

  /// <inheritdoc/>
  public async IAsyncEnumerable<TRow> DeserializeRows(
    Stream stream,
    [EnumeratorCancellation] CancellationToken cancellationToken = default)
  {
    if (stream is null)
    {
      throw new ArgumentNullException(nameof(stream));
    }

    using var reader = new StreamReader(stream, leaveOpen: true);
    using var csv = new CsvReader(reader, _configuration);

    csv.Context.RegisterClassMap(new SerializedLabelClassMap<TRow>(_nullValues));

    var enumerator = csv.GetRecordsAsync<TRow>(cancellationToken).GetAsyncEnumerator(cancellationToken);
    try
    {
      while (true)
      {
        bool hasMore;
        try
        {
          hasMore = await enumerator.MoveNextAsync().ConfigureAwait(false);
        }
        catch (HeaderValidationException ex)
        {
          throw new SchemaMismatchException(
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
      await enumerator.DisposeAsync().ConfigureAwait(false);
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

    await using var writer = new StreamWriter(stream, leaveOpen: true);
    await using var csv = new CsvWriter(writer, _configuration);

    csv.Context.RegisterClassMap(new SerializedLabelClassMap<TRow>(_nullValues));

    if (_configuration.HasHeaderRecord)
    {
      csv.WriteHeader<TRow>();
      await csv.NextRecordAsync().ConfigureAwait(false);
    }

    await foreach (var row in rows.ConfigureAwait(false))
    {
      csv.WriteRecord(row);
      await csv.NextRecordAsync().ConfigureAwait(false);
    }

    await csv.FlushAsync().ConfigureAwait(false);
  }

  private static CsvConfiguration BuildDefaultConfiguration() =>
    new(CultureInfo.InvariantCulture) { HasHeaderRecord = true };
}

/// <summary>
/// Constants shared across <see cref="CsvFormatSerializer{TRow}"/> instantiations.
/// </summary>
public static class CsvFormatSerializerDefaults
{
  /// <summary>The default null-sentinel list — empty cells round-trip as null.</summary>
  public static readonly IReadOnlyList<string> NullValues = new[] { string.Empty };
}
