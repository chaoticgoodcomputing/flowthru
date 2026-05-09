using Flowthru.Data.Storage;

namespace Flowthru.Data.Catalog;

/// <summary>
/// Binary-file item-builder extensions. Like <see cref="TextExtensions"/>
/// but for <see cref="byte"/>-array content — PNG/JPG/PDF/etc.
/// </summary>
public static class BinaryExtensions
{
  /// <summary>
  /// Build a binary-file catalog item.
  /// <see cref="BinaryBuilder.AtPath(string)"/> must be called before
  /// <see cref="BinaryBuilder.Build"/>.
  /// </summary>
  public static BinaryBuilder Binary(this ItemAnchor<byte[]> anchor) => new(anchor.Label);
}

/// <summary>Tier-1 builder for a binary-file catalog item.</summary>
public sealed class BinaryBuilder
{
  private readonly string _label;
  private string? _path;

  internal BinaryBuilder(string label)
  {
    _label = label;
  }

  /// <summary>Set the filesystem path for this binary file.</summary>
  public BinaryBuilder AtPath(string path)
  {
    if (string.IsNullOrWhiteSpace(path))
      throw new ArgumentException("Path cannot be null or whitespace.", nameof(path));
    _path = path;
    return this;
  }

  /// <summary>Materialise the <see cref="IItem{ByteArray}"/>.</summary>
  public IItem<byte[]> Build()
  {
    if (_path is null)
      throw new InvalidOperationException(
        $"Binary item '{_label}' requires AtPath(...) before Build()."
      );
    return new Item<byte[]>(_label, new BinaryFileStorageAdapter(_path));
  }
}
