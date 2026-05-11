using Flowthru.Data.Schema;
using Flowthru.Data.Storage;
using Flowthru.Data.Storage.Excel;

namespace Flowthru.Data.Catalog;

/// <summary>
/// Excel item-builder extensions on <see cref="ItemAnchor{T}"/>.
/// Excel is read-only — the resulting items report
/// <see cref="StorageTraits.CanWrite"/> = <c>false</c>; calling
/// <c>Save</c> fails fast before touching the workbook.
/// Directory-of-Excel items are constructed via the universal
/// <see cref="DirectoryOfExtensions.Directory{T, TBuilder}"/> lift.
/// </summary>
public static class ExcelExtensions
{
  /// <summary>Build a read-only Excel (.xlsx) file catalog item.</summary>
  public static ExcelBuilder<TRow> Excel<TRow>(this ItemAnchor<IEnumerable<TRow>> anchor)
    where TRow : notnull, IFlatSchema, ITextSerializable =>
    new(anchor.Label);
}

/// <summary>Tier-1 builder for an Excel catalog item (single file or, via lift, directory).</summary>
public sealed class ExcelBuilder<TRow>
  : IFileItemBuilder<IEnumerable<TRow>>
  where TRow : notnull, IFlatSchema, ITextSerializable
{
  private readonly string _label;
  private string? _path;
  private string? _sheetName;
  private IStorageMediumResolver? _resolver;
  private IReadOnlyList<string>? _nullValues;

  internal ExcelBuilder(string label)
  {
    _label = label;
  }

  /// <inheritdoc/>
  public string Label => _label;

  /// <inheritdoc/>
  public string DefaultFilePattern => "*.xlsx";

  /// <summary>Set the path or URI for this .xlsx file.</summary>
  public ExcelBuilder<TRow> AtPath(string path)
  {
    if (string.IsNullOrWhiteSpace(path))
      throw new ArgumentException("Path cannot be null or whitespace.", nameof(path));
    _path = path;
    return this;
  }

  /// <summary>Set the worksheet name within the workbook.</summary>
  public ExcelBuilder<TRow> WithSheet(string sheetName)
  {
    if (string.IsNullOrWhiteSpace(sheetName))
      throw new ArgumentException("Sheet name cannot be null or whitespace.", nameof(sheetName));
    _sheetName = sheetName;
    return this;
  }

  /// <summary>Optional <see cref="IStorageMediumResolver"/> for non-filesystem schemes.</summary>
  public ExcelBuilder<TRow> WithResolver(IStorageMediumResolver resolver)
  {
    _resolver = resolver;
    return this;
  }

  /// <summary>Optional null-sentinel list for nullable properties.</summary>
  public ExcelBuilder<TRow> WithNullValues(IReadOnlyList<string> nullValues)
  {
    _nullValues = nullValues ?? throw new ArgumentNullException(nameof(nullValues));
    return this;
  }

  /// <inheritdoc/>
  public IStorageAdapter<IEnumerable<TRow>> CreateAdapterForFile(string filePath)
  {
    if (_sheetName is null)
      throw new InvalidOperationException(
        $"Excel item '{_label}' requires WithSheet(...) before Build()."
      );
    var format = _nullValues is null
      ? new ExcelFormatSerializer<TRow>(_sheetName)
      : new ExcelFormatSerializer<TRow>(_sheetName, _nullValues);
    var medium = (_resolver ?? StorageMediumResolver.Filesystem).Resolve(filePath);
    return new ComposedStorageAdapter<IEnumerable<TRow>, TRow>(
      medium,
      reader: format,
      writer: null,
      new EnumerableContainerAdapter<TRow>()
    );
  }

  /// <summary>Materialise the <see cref="IItem{T}"/>.</summary>
  public IItem<IEnumerable<TRow>> Build()
  {
    if (_path is null)
      throw new InvalidOperationException(
        $"Excel item '{_label}' requires AtPath(...) before Build()."
      );
    return new Item<IEnumerable<TRow>>(_label, CreateAdapterForFile(_path));
  }
}
