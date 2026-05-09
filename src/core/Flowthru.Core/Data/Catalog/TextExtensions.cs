using Flowthru.Data.Storage;

namespace Flowthru.Data.Catalog;

/// <summary>
/// Plain-text-file item-builder extensions. The
/// <see cref="ItemAnchor{T}"/> receiver is constrained to the
/// <see cref="string"/>-typed anchor — text items are always
/// <c>string</c>-typed; if you need structure, use Json/Csv/Parquet.
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

/// <summary>Tier-1 builder for a text-file catalog item.</summary>
public sealed class TextBuilder
{
  private readonly string _label;
  private string? _path;

  internal TextBuilder(string label)
  {
    _label = label;
  }

  /// <summary>Set the filesystem path for this text file.</summary>
  public TextBuilder AtPath(string path)
  {
    if (string.IsNullOrWhiteSpace(path))
      throw new ArgumentException("Path cannot be null or whitespace.", nameof(path));
    _path = path;
    return this;
  }

  /// <summary>Materialise the <see cref="IItem{String}"/>.</summary>
  public IItem<string> Build()
  {
    if (_path is null)
      throw new InvalidOperationException(
        $"Text item '{_label}' requires AtPath(...) before Build()."
      );
    return new Item<string>(_label, new TextFileStorageAdapter(_path));
  }
}
