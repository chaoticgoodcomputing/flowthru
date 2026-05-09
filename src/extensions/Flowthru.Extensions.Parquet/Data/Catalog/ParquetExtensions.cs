using Flowthru.Data.Schema;
using Flowthru.Data.Storage;
using Flowthru.Data.Storage.Parquet;

namespace Flowthru.Data.Catalog;

/// <summary>
/// Parquet item-builder extensions on <see cref="ItemAnchor{T}"/>.
/// Parquet is always a row stream; the row type must satisfy
/// <see cref="IFlatSchema"/> + <see cref="IBinarySerializable"/>.
/// </summary>
public static class ParquetExtensions
{
  /// <summary>Build a Parquet file catalog item.</summary>
  public static ParquetBuilder<TRow> Parquet<TRow>(this ItemAnchor<IEnumerable<TRow>> anchor)
    where TRow : notnull, IFlatSchema, IBinarySerializable =>
    new(anchor.Label);

  /// <summary>Build a directory-of-Parquet-files catalog item.</summary>
  public static ParquetDirectoryBuilder<TRow> Parquet<TRow>(
    this ItemAnchor<Directory<IEnumerable<TRow>>> anchor
  )
    where TRow : notnull, IFlatSchema, IBinarySerializable =>
    new(anchor.Label);
}

/// <summary>Tier-1 builder for a single-file Parquet catalog item.</summary>
public sealed class ParquetBuilder<TRow> where TRow : notnull, IFlatSchema, IBinarySerializable
{
  private readonly string _label;
  private string? _path;
  private IStorageMediumResolver? _resolver;
  private ParquetItemOptions<TRow>? _options;

  internal ParquetBuilder(string label)
  {
    _label = label;
  }

  /// <summary>Set the path or URI for this Parquet file.</summary>
  public ParquetBuilder<TRow> AtPath(string path)
  {
    if (string.IsNullOrWhiteSpace(path))
      throw new ArgumentException("Path cannot be null or whitespace.", nameof(path));
    _path = path;
    return this;
  }

  /// <summary>Optional <see cref="IStorageMediumResolver"/> for non-filesystem schemes.</summary>
  public ParquetBuilder<TRow> WithResolver(IStorageMediumResolver resolver)
  {
    _resolver = resolver;
    return this;
  }

  /// <summary>
  /// Optional performance/behaviour tuning. Defaults: Snappy
  /// compression, 1 000 000-row groups, dictionary encoding enabled.
  /// </summary>
  public ParquetBuilder<TRow> WithOptions(ParquetItemOptions<TRow> options)
  {
    _options = options ?? throw new ArgumentNullException(nameof(options));
    return this;
  }

  /// <summary>Materialise the <see cref="IItem{T}"/>.</summary>
  public IItem<IEnumerable<TRow>> Build()
  {
    if (_path is null)
      throw new InvalidOperationException(
        $"Parquet item '{_label}' requires AtPath(...) before Build()."
      );
    var medium = (_resolver ?? StorageMediumResolver.Filesystem).Resolve(_path);
    return new Item<IEnumerable<TRow>>(
      _label,
      new ComposedStorageAdapter<IEnumerable<TRow>, TRow>(
        medium,
        new ParquetFormatSerializer<TRow>(_options),
        new EnumerableContainerAdapter<TRow>()
      )
    );
  }
}

/// <summary>Tier-1 builder for a directory-of-Parquet-files catalog item.</summary>
public sealed class ParquetDirectoryBuilder<TRow> where TRow : notnull, IFlatSchema, IBinarySerializable
{
  private readonly string _label;
  private string? _directoryPath;
  private string _filePattern = "*.parquet";
  private ParquetItemOptions<TRow>? _options;

  internal ParquetDirectoryBuilder(string label)
  {
    _label = label;
  }

  /// <summary>Set the directory path holding the Parquet files.</summary>
  public ParquetDirectoryBuilder<TRow> AtPath(string directoryPath)
  {
    if (string.IsNullOrWhiteSpace(directoryPath))
      throw new ArgumentException("Path cannot be null or whitespace.", nameof(directoryPath));
    _directoryPath = directoryPath;
    return this;
  }

  /// <summary>Override the default <c>*.parquet</c> filename pattern.</summary>
  public ParquetDirectoryBuilder<TRow> WithFilePattern(string filePattern)
  {
    if (string.IsNullOrWhiteSpace(filePattern))
      throw new ArgumentException("File pattern cannot be null or whitespace.", nameof(filePattern));
    _filePattern = filePattern;
    return this;
  }

  /// <summary>See <see cref="ParquetBuilder{TRow}.WithOptions"/>.</summary>
  public ParquetDirectoryBuilder<TRow> WithOptions(ParquetItemOptions<TRow> options)
  {
    _options = options ?? throw new ArgumentNullException(nameof(options));
    return this;
  }

  /// <summary>Materialise the <see cref="IItem{T}"/>.</summary>
  public IItem<Directory<IEnumerable<TRow>>> Build()
  {
    if (_directoryPath is null)
      throw new InvalidOperationException(
        $"Parquet directory item '{_label}' requires AtPath(...) before Build()."
      );
    return new Item<Directory<IEnumerable<TRow>>>(
      _label,
      new DirectoryStorageAdapter<IEnumerable<TRow>>(
        _directoryPath,
        _filePattern,
        perFilePath => new ComposedStorageAdapter<IEnumerable<TRow>, TRow>(
          new FileStorageMedium(perFilePath),
          new ParquetFormatSerializer<TRow>(_options),
          new EnumerableContainerAdapter<TRow>()
        )
      )
    );
  }
}
