using System.Runtime.CompilerServices;
using Flowthru.Core.Abstractions;
using Flowthru.Core.Data.Capabilities;
using Flowthru.Core.Data.Storage.Format;
using Flowthru.Core.Data.Validation;
using Flowthru.Core.Effects;

namespace Flowthru.Core.Data.Storage;

/// <summary>
/// Storage adapter that reads all CSV files in a directory and concatenates
/// them into a single <see cref="IEnumerable{TRow}"/>.
/// </summary>
/// <typeparam name="TRow">Row schema type (must be flat and text-serializable)</typeparam>
/// <remarks>
/// <para>
/// This adapter is <strong>read-only</strong>. It enumerates every <c>*.csv</c> file in the
/// given directory in lexicographic order, deserialises each with a shared
/// <see cref="CsvFormatSerializer{TRow}"/>, and returns all rows concatenated.
/// </para>
/// <para>
/// All files must share the same schema (identical column headers). Files from
/// mixed schemas will cause deserialization errors at load time.
/// </para>
/// <para>
/// Typical use case: a raw ingest layer where data is delivered as one file per
/// day, one file per region, etc.
/// </para>
/// </remarks>
public sealed class DirectoryCsvStorageAdapter<TRow> : ReadOnlyDirectoryStorageAdapter<TRow>
  where TRow : notnull, IFlatSchema, ITextSerializable
{
  private readonly CsvFormatSerializer<TRow> _format;

  /// <summary>Creates a new directory CSV adapter.</summary>
  /// <param name="directoryPath">Path to the directory containing CSV files.</param>
  /// <param name="nullValues">
  /// Optional set of strings that should deserialize to null for nullable properties.
  /// Defaults to <c>[""]</c> — empty cells are treated as null per CSV convention. Pass a
  /// custom list (e.g. <c>["", "NA", "N/A"]</c>) for pandas-style handling of messy data.
  /// </param>
  /// <exception cref="ArgumentException">Thrown if <paramref name="directoryPath"/> is null or whitespace.</exception>
  public DirectoryCsvStorageAdapter(
    string directoryPath,
    IReadOnlyList<string>? nullValues = null
  )
    : base(directoryPath, "*.csv", typeof(TRow).Name)
  {
    _format = nullValues is null
      ? new CsvFormatSerializer<TRow>()
      : new CsvFormatSerializer<TRow>(nullValues);
  }

  /// <inheritdoc/>
  /// <remarks>
  /// <c>CanWrite</c> is <c>false</c> — directory ingest entries represent immutable source data
  /// and cannot be written back to the directory.
  /// <c>CanStream</c> is <c>true</c> — each file is streamed row-by-row.
  /// </remarks>
  public override StorageTraits Traits => new StorageTraits { CanWrite = false, CanStream = true };

  /// <inheritdoc/>
  protected override async IAsyncEnumerable<TRow> LoadFile(
    string filePath,
    [EnumeratorCancellation] CancellationToken ct
  )
  {
    await using var stream = File.OpenRead(filePath);
    await foreach (var row in _format.DeserializeRows(stream).WithCancellation(ct))
    {
      yield return row;
    }
  }

  /// <inheritdoc/>
  protected override async Task<ValidationResult> ValidateFileAsync(
    string filePath,
    int sampleSize,
    CancellationToken ct
  )
  {
    try
    {
      var sampled = 0;
      await using var stream = File.OpenRead(filePath);
      await foreach (var _ in _format.DeserializeRows(stream).WithCancellation(ct))
      {
        if (sampleSize > 0 && ++sampled >= sampleSize)
        {
          break;
        }
      }

      return ValidationResult.Success();
    }
    catch (Exception ex)
    {
      return ValidationResult.FromException(typeof(TRow).Name, ex);
    }
  }
}
