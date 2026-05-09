using Flowthru.Data.Schema;
using Flowthru.Data.Storage;
using Flowthru.Data.Storage.Csv;

namespace Flowthru.Data.Catalog;

/// <summary>
/// CSV item-builder extensions on <see cref="ItemAnchor{T}"/>.
/// CSV is always a row stream — single overload over
/// <c>ItemAnchor&lt;IEnumerable&lt;TRow&gt;&gt;</c>; the row type
/// must satisfy <see cref="IFlatSchema"/> +
/// <see cref="ITextSerializable"/>, both of which the
/// <c>[FlowthruSchema]</c> source generator emits when the schema's
/// shape is CSV-compatible.
/// </summary>
public static class CsvExtensions
{
  /// <summary>
  /// Build a CSV file catalog item (single file, row stream).
  /// </summary>
  public static CsvBuilder<TRow> Csv<TRow>(this ItemAnchor<IEnumerable<TRow>> anchor)
    where TRow : notnull, IFlatSchema, ITextSerializable =>
    new(anchor.Label);

  /// <summary>
  /// Build a directory-of-CSV-files catalog item — each file is an
  /// independent <see cref="IEnumerable{TRow}"/> sharing the same
  /// schema.
  /// </summary>
  public static CsvDirectoryBuilder<TRow> Csv<TRow>(
    this ItemAnchor<Directory<IEnumerable<TRow>>> anchor
  )
    where TRow : notnull, IFlatSchema, ITextSerializable =>
    new(anchor.Label);
}

/// <summary>Tier-1 builder for a single-file CSV catalog item.</summary>
public sealed class CsvBuilder<TRow> where TRow : notnull, IFlatSchema, ITextSerializable
{
  private readonly string _label;
  private string? _path;
  private IStorageMediumResolver? _resolver;
  private IReadOnlyList<string>? _nullValues;

  internal CsvBuilder(string label)
  {
    _label = label;
  }

  /// <summary>
  /// Set the path or URI for this CSV file. Bare paths and
  /// <c>file://</c> URIs resolve to local filesystem; other schemes
  /// (<c>https://</c>) require <see cref="WithResolver(IStorageMediumResolver)"/>.
  /// </summary>
  public CsvBuilder<TRow> AtPath(string path)
  {
    if (string.IsNullOrWhiteSpace(path))
      throw new ArgumentException("Path cannot be null or whitespace.", nameof(path));
    _path = path;
    return this;
  }

  /// <summary>
  /// Optional <see cref="IStorageMediumResolver"/> for non-filesystem
  /// schemes. When omitted, falls back to
  /// <see cref="StorageMediumResolver.Filesystem"/>.
  /// </summary>
  public CsvBuilder<TRow> WithResolver(IStorageMediumResolver resolver)
  {
    _resolver = resolver;
    return this;
  }

  /// <summary>
  /// Optional null-sentinel list for nullable properties. Defaults
  /// to <c>[""]</c> (empty cells round-trip as null). Pass
  /// <c>["", "NA", "N/A", "NULL"]</c> for pandas-style messy-data
  /// handling. The first entry is also the canonical write-side
  /// representation when a nullable property is null.
  /// </summary>
  public CsvBuilder<TRow> WithNullValues(IReadOnlyList<string> nullValues)
  {
    _nullValues = nullValues ?? throw new ArgumentNullException(nameof(nullValues));
    return this;
  }

  /// <summary>Materialise the <see cref="IItem{T}"/>.</summary>
  public IItem<IEnumerable<TRow>> Build()
  {
    if (_path is null)
      throw new InvalidOperationException(
        $"Csv item '{_label}' requires AtPath(...) before Build()."
      );
    var format = _nullValues is null
      ? new CsvFormatSerializer<TRow>()
      : new CsvFormatSerializer<TRow>(_nullValues);
    var medium = (_resolver ?? StorageMediumResolver.Filesystem).Resolve(_path);
    return new Item<IEnumerable<TRow>>(
      _label,
      new ComposedStorageAdapter<IEnumerable<TRow>, TRow>(
        medium,
        format,
        new EnumerableContainerAdapter<TRow>()
      )
    );
  }
}

/// <summary>Tier-1 builder for a directory-of-CSV-files catalog item.</summary>
public sealed class CsvDirectoryBuilder<TRow> where TRow : notnull, IFlatSchema, ITextSerializable
{
  private readonly string _label;
  private string? _directoryPath;
  private string _filePattern = "*.csv";
  private IReadOnlyList<string>? _nullValues;

  internal CsvDirectoryBuilder(string label)
  {
    _label = label;
  }

  /// <summary>Set the directory path holding the CSV files.</summary>
  public CsvDirectoryBuilder<TRow> AtPath(string directoryPath)
  {
    if (string.IsNullOrWhiteSpace(directoryPath))
      throw new ArgumentException("Path cannot be null or whitespace.", nameof(directoryPath));
    _directoryPath = directoryPath;
    return this;
  }

  /// <summary>
  /// Override the default <c>*.csv</c> filename pattern (e.g.
  /// <c>partition-*.csv</c> for partitioned datasets).
  /// </summary>
  public CsvDirectoryBuilder<TRow> WithFilePattern(string filePattern)
  {
    if (string.IsNullOrWhiteSpace(filePattern))
      throw new ArgumentException("File pattern cannot be null or whitespace.", nameof(filePattern));
    _filePattern = filePattern;
    return this;
  }

  /// <summary>See <see cref="CsvBuilder{TRow}.WithNullValues"/>.</summary>
  public CsvDirectoryBuilder<TRow> WithNullValues(IReadOnlyList<string> nullValues)
  {
    _nullValues = nullValues ?? throw new ArgumentNullException(nameof(nullValues));
    return this;
  }

  /// <summary>Materialise the <see cref="IItem{T}"/>.</summary>
  public IItem<Directory<IEnumerable<TRow>>> Build()
  {
    if (_directoryPath is null)
      throw new InvalidOperationException(
        $"Csv directory item '{_label}' requires AtPath(...) before Build()."
      );
    var format = _nullValues is null
      ? new CsvFormatSerializer<TRow>()
      : new CsvFormatSerializer<TRow>(_nullValues);
    return new Item<Directory<IEnumerable<TRow>>>(
      _label,
      new DirectoryStorageAdapter<IEnumerable<TRow>>(
        _directoryPath,
        _filePattern,
        perFilePath => new ComposedStorageAdapter<IEnumerable<TRow>, TRow>(
          new FileStorageMedium(perFilePath),
          format,
          new EnumerableContainerAdapter<TRow>()
        )
      )
    );
  }
}
