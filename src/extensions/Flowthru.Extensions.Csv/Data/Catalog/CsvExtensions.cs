using Flowthru.Data.Schema;
using Flowthru.Data.Storage;
using Flowthru.Data.Storage.Csv;

namespace Flowthru.Data.Catalog;

/// <summary>
/// CSV item-builder extensions on <see cref="ItemAnchor{T}"/>. Single
/// per-file overload over <c>ItemAnchor&lt;IEnumerable&lt;TRow&gt;&gt;</c>;
/// the row type must satisfy <see cref="IFlatSchema"/> +
/// <see cref="ITextSerializable"/>, both of which the
/// <c>[FlowthruSchema]</c> source generator emits when the schema's
/// shape is CSV-compatible. Directory-of-CSV items are constructed
/// via the universal <see cref="DirectoryOfExtensions.Directory{T, TBuilder}"/>
/// lift on <c>ItemAnchor&lt;DirectoryOf&lt;IEnumerable&lt;TRow&gt;&gt;&gt;</c>.
/// </summary>
public static class CsvExtensions
{
  /// <summary>Build a CSV file catalog item (single file, row stream).</summary>
  public static CsvBuilder<TRow> Csv<TRow>(this ItemAnchor<IEnumerable<TRow>> anchor)
    where TRow : notnull, IFlatSchema, ITextSerializable =>
    new(anchor.Label);
}

/// <summary>Tier-1 builder for a CSV catalog item (single file or, via lift, directory).</summary>
public sealed class CsvBuilder<TRow>
  : IFileItemBuilder<IEnumerable<TRow>>
  where TRow : notnull, IFlatSchema, ITextSerializable
{
  private readonly string _label;
  private string? _path;
  private IStorageMediumResolver? _resolver;
  private IReadOnlyList<string>? _nullValues;

  internal CsvBuilder(string label)
  {
    _label = label;
  }

  /// <inheritdoc/>
  public string Label => _label;

  /// <inheritdoc/>
  public string DefaultFilePattern => "*.csv";

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

  /// <inheritdoc/>
  public IStorageAdapter<IEnumerable<TRow>> CreateAdapterForFile(string filePath)
  {
    var format = _nullValues is null
      ? new CsvFormatSerializer<TRow>()
      : new CsvFormatSerializer<TRow>(_nullValues);
    var medium = (_resolver ?? StorageMediumResolver.Filesystem).Resolve(filePath);
    return new ComposedStorageAdapter<IEnumerable<TRow>, TRow>(
      medium,
      format,
      new EnumerableContainerAdapter<TRow>()
    );
  }

  /// <summary>Materialise the <see cref="IItem{T}"/>.</summary>
  public IItem<IEnumerable<TRow>> Build()
  {
    if (_path is null)
      throw new InvalidOperationException(
        $"Csv item '{_label}' requires AtPath(...) before Build()."
      );
    return new Item<IEnumerable<TRow>>(_label, CreateAdapterForFile(_path));
  }
}
