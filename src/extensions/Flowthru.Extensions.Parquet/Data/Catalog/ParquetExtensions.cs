using Flowthru.Data.Schema;
using Flowthru.Data.Storage;
using Flowthru.Data.Storage.Parquet;

namespace Flowthru.Data.Catalog;

/// <summary>
/// Parquet item-builder extensions on <see cref="ItemAnchor{T}"/>.
/// Parquet is always a row stream; the row type must satisfy
/// <see cref="IFlatSchema"/> + <see cref="IBinarySerializable"/>.
/// Directory-of-Parquet items are constructed via the universal
/// <see cref="DirectoryOfExtensions.Directory{T, TBuilder}"/> lift.
/// </summary>
public static class ParquetExtensions
{
  /// <summary>Build a Parquet file catalog item.</summary>
  public static ParquetBuilder<TRow> Parquet<TRow>(this ItemAnchor<IEnumerable<TRow>> anchor)
    where TRow : notnull, IFlatSchema, IBinarySerializable =>
    new(anchor.Label);
}

/// <summary>Tier-1 builder for a Parquet catalog item (single file or, via lift, directory).</summary>
public sealed class ParquetBuilder<TRow>
  : IFileItemBuilder<IEnumerable<TRow>>
  where TRow : notnull, IFlatSchema, IBinarySerializable
{
  private readonly string _label;
  private string? _path;
  private IStorageMediumResolver? _resolver;
  private ParquetItemOptions<TRow>? _options;

  internal ParquetBuilder(string label)
  {
    _label = label;
  }

  /// <inheritdoc/>
  public string Label => _label;

  /// <inheritdoc/>
  public string DefaultFilePattern => "*.parquet";

  /// <summary>Set the path or URI for this Parquet file.</summary>
  public ParquetBuilder<TRow> AtPath(string path)
  {
    if (string.IsNullOrWhiteSpace(path))
      throw new ArgumentException("Path cannot be null or whitespace.", nameof(path));
    _path = path;
    return this;
  }

  /// <summary>
  /// Optional <see cref="IStorageMediumResolver"/> for non-filesystem
  /// schemes. When omitted, the builder consults
  /// <see cref="StorageMediumResolver.Current"/> (pushed by
  /// <see cref="CatalogAbstract.CreateItem{T}"/> during materialization)
  /// and finally falls back to <see cref="StorageMediumResolver.Filesystem"/>.
  /// </summary>
  public ParquetBuilder<TRow> WithResolver(IStorageMediumResolver resolver)
  {
    _resolver = resolver ?? throw new ArgumentNullException(nameof(resolver));
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

  /// <inheritdoc/>
  public IStorageAdapter<IEnumerable<TRow>> CreateAdapterForFile(string filePath)
  {
    var resolver =
      _resolver ?? StorageMediumResolver.Current ?? StorageMediumResolver.Filesystem;
    var medium = resolver.Resolve(filePath);
    return new ComposedStorageAdapter<IEnumerable<TRow>, TRow>(
      medium,
      new ParquetFormatSerializer<TRow>(_options),
      new EnumerableContainerAdapter<TRow>()
    );
  }

  /// <summary>Materialise the <see cref="IItem{T}"/>.</summary>
  public IItem<IEnumerable<TRow>> Build()
  {
    if (_path is null)
      throw new InvalidOperationException(
        $"Parquet item '{_label}' requires AtPath(...) before Build()."
      );
    return new Item<IEnumerable<TRow>>(_label, CreateAdapterForFile(_path));
  }
}
