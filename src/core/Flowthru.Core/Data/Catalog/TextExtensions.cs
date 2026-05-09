using Flowthru.Data.Storage;

namespace Flowthru.Data.Catalog;

/// <summary>
/// Plain-text-file item-builder extensions. The
/// <see cref="ItemAnchor{T}"/> receiver is constrained to the
/// <see cref="string"/>-typed anchor — text items are always
/// <c>string</c>-typed; if you need structure, use Json/Csv/Parquet.
/// Directory-of-text items are constructed via the universal
/// <see cref="DirectoryOfExtensions.Directory{T, TBuilder}"/> lift.
/// </summary>
public static class TextExtensions
{
  /// <summary>
  /// Build a plain-text-file catalog item.
  /// <see cref="TextBuilder.AtPath(string)"/> must be called before
  /// <see cref="TextBuilder.Build"/>.
  /// </summary>
  public static TextBuilder Text(this ItemAnchor<string> anchor) => new(anchor.Label);
}

/// <summary>Tier-1 builder for a text-file catalog item (single file or, via lift, directory).</summary>
public sealed class TextBuilder : IFileItemBuilder<string>
{
  private readonly string _label;
  private string? _path;

  internal TextBuilder(string label)
  {
    _label = label;
  }

  /// <inheritdoc/>
  public string Label => _label;

  /// <inheritdoc/>
  public string DefaultFilePattern => "*.txt";

  /// <summary>Set the filesystem path for this text file.</summary>
  public TextBuilder AtPath(string path)
  {
    if (string.IsNullOrWhiteSpace(path))
      throw new ArgumentException("Path cannot be null or whitespace.", nameof(path));
    _path = path;
    return this;
  }

  /// <inheritdoc/>
  public IStorageAdapter<string> CreateAdapterForFile(string filePath) =>
    new TextFileStorageAdapter(filePath);

  /// <summary>Materialise the <see cref="IItem{String}"/>.</summary>
  public IItem<string> Build()
  {
    if (_path is null)
      throw new InvalidOperationException(
        $"Text item '{_label}' requires AtPath(...) before Build()."
      );
    return new Item<string>(_label, CreateAdapterForFile(_path));
  }
}
