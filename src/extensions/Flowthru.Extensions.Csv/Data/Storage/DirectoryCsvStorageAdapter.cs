using Flowthru.Abstractions;
using Flowthru.Data.Capabilities;
using Flowthru.Data.Storage.Format;
using Flowthru.Data.Validation;
using Flowthru.Effects;

namespace Flowthru.Data.Storage;

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
public sealed class DirectoryCsvStorageAdapter<TRow> : IStorageAdapter<IEnumerable<TRow>>
  where TRow : notnull, IFlatSchema, ITextSerializable
{
  private readonly string _directoryPath;
  private readonly CsvFormatSerializer<TRow> _format;

  /// <summary>Creates a new directory CSV adapter.</summary>
  /// <param name="directoryPath">Path to the directory containing CSV files.</param>
  /// <exception cref="ArgumentException">Thrown if <paramref name="directoryPath"/> is null or whitespace.</exception>
  public DirectoryCsvStorageAdapter(string directoryPath)
  {
    if (string.IsNullOrWhiteSpace(directoryPath))
      throw new ArgumentException(
        "Directory path cannot be null or whitespace",
        nameof(directoryPath)
      );

    _directoryPath = directoryPath;
    _format = new CsvFormatSerializer<TRow>();
  }

  /// <inheritdoc/>
  /// <remarks>
  /// <c>CanWrite</c> is <c>false</c> — directory ingest entries represent immutable source data
  /// and cannot be written back to the directory.
  /// <c>CanStream</c> is <c>true</c> — each file is streamed row-by-row.
  /// </remarks>
  public StorageTraits Traits => new StorageTraits { CanWrite = false, CanStream = true };

  /// <inheritdoc/>
  /// <remarks>
  /// Files are read in lexicographic (alphabetical) order so the resulting row sequence is
  /// deterministic across runs.
  /// </remarks>
  public FlowIO<IEnumerable<TRow>> Load()
  {
    return FlowIO.LiftAsync(
      async (CancellationToken ct) =>
      {
        var allRows = new List<TRow>();

        foreach (var filePath in GetCsvFiles())
        {
          ct.ThrowIfCancellationRequested();

          await using var stream = File.OpenRead(filePath);
          var rows = _format.DeserializeRows(stream);

          await foreach (var row in rows.WithCancellation(ct))
            allRows.Add(row);
        }

        return (IEnumerable<TRow>)allRows;
      }
    );
  }

  /// <inheritdoc/>
  /// <remarks>Always returns a failed effect — this adapter is read-only.</remarks>
  public FlowIO<FlowUnit> Save(IEnumerable<TRow> data) =>
    FlowIO.Fail<FlowUnit>(
      new NotSupportedException(
        $"DirectoryCsvStorageAdapter is read-only. "
          + $"Directory '{_directoryPath}' cannot be written to via this catalog entry."
      )
    );

  /// <inheritdoc/>
  public FlowIO<bool> Exists() =>
    FlowIO.Lift(() => Directory.Exists(_directoryPath) && GetCsvFiles().Any());

  /// <inheritdoc/>
  public FlowIO<ValidationResult> InspectShallow(int sampleSize)
  {
    return FlowIO.LiftAsync(
      async (CancellationToken ct) =>
      {
        if (!Directory.Exists(_directoryPath))
        {
          return ValidationResult.Failure(
            catalogKey: typeof(TRow).Name,
            errorType: ValidationErrorType.NotFound,
            message: $"Directory '{_directoryPath}' does not exist.",
            details: "Ensure the raw data directory is present before running the pipeline."
          );
        }

        var files = GetCsvFiles().ToList();
        if (files.Count == 0)
        {
          return ValidationResult.Failure(
            catalogKey: typeof(TRow).Name,
            errorType: ValidationErrorType.NotFound,
            message: $"No CSV files found in '{_directoryPath}'.",
            details: "Directory exists but contains no *.csv files."
          );
        }

        // Sample the first file to validate schema compatibility.
        try
        {
          var sampled = 0;
          await using var stream = File.OpenRead(files[0]);
          await foreach (var _ in _format.DeserializeRows(stream).WithCancellation(ct))
          {
            if (++sampled >= sampleSize)
              break;
          }

          return ValidationResult.Success();
        }
        catch (Exception ex)
        {
          return ValidationResult.FromException(typeof(TRow).Name, ex);
        }
      }
    );
  }

  /// <inheritdoc/>
  public FlowIO<ValidationResult> InspectDeep()
  {
    return FlowIO.LiftAsync(
      async (CancellationToken ct) =>
      {
        try
        {
          foreach (var filePath in GetCsvFiles())
          {
            ct.ThrowIfCancellationRequested();
            await using var stream = File.OpenRead(filePath);
            await foreach (var _ in _format.DeserializeRows(stream).WithCancellation(ct)) { }
          }

          return ValidationResult.Success();
        }
        catch (Exception ex)
        {
          return ValidationResult.FromException(typeof(TRow).Name, ex);
        }
      }
    );
  }

  private IEnumerable<string> GetCsvFiles() =>
    Directory.Exists(_directoryPath)
      ? Directory
        .EnumerateFiles(_directoryPath, "*.csv", SearchOption.TopDirectoryOnly)
        .OrderBy(f => f, StringComparer.OrdinalIgnoreCase)
      : Enumerable.Empty<string>();
}
