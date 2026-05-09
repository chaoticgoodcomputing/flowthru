using Flowthru.Data.Storage;

namespace Flowthru.Data.Catalog;

/// <summary>
/// Universal builder for a <see cref="DirectoryOf{T}"/> catalog item.
/// Wraps any <see cref="IFileItemBuilder{T}"/> into a directory
/// scan; format extensions don't ship their own per-directory
/// builder.
/// </summary>
/// <typeparam name="T">Per-file payload type, surfaced from the wrapped file builder.</typeparam>
public sealed class DirectoryOfBuilder<T> where T : notnull
{
  private readonly string _label;
  private readonly IFileItemBuilder<T> _fileBuilder;
  private string? _path;
  private string _filePattern;

  internal DirectoryOfBuilder(string label, IFileItemBuilder<T> fileBuilder)
  {
    _label = label;
    _fileBuilder = fileBuilder;
    _filePattern = fileBuilder.DefaultFilePattern;
  }

  /// <summary>Set the directory path holding the per-file payloads.</summary>
  public DirectoryOfBuilder<T> AtPath(string path)
  {
    if (string.IsNullOrWhiteSpace(path))
      throw new ArgumentException("Path cannot be null or whitespace.", nameof(path));
    _path = path;
    return this;
  }

  /// <summary>
  /// Override the filename pattern; defaults to the format builder's
  /// <see cref="IFileItemBuilder{T}.DefaultFilePattern"/>.
  /// </summary>
  public DirectoryOfBuilder<T> WithFilePattern(string filePattern)
  {
    if (string.IsNullOrWhiteSpace(filePattern))
      throw new ArgumentException("File pattern cannot be null or whitespace.", nameof(filePattern));
    _filePattern = filePattern;
    return this;
  }

  /// <summary>Materialise the <see cref="IItem{T}"/>.</summary>
  public IItem<DirectoryOf<T>> Build()
  {
    if (_path is null)
      throw new InvalidOperationException(
        $"DirectoryOf item '{_label}' requires AtPath(...) before Build()."
      );
    return new Item<DirectoryOf<T>>(
      _label,
      new DirectoryStorageAdapter<T>(
        _path,
        _filePattern,
        perFilePath => _fileBuilder.CreateAdapterForFile(perFilePath)
      )
    );
  }
}

/// <summary>
/// Universal <c>Directory(...)</c> lift on <see cref="ItemAnchor{T}"/>:
/// any <see cref="IFileItemBuilder{T}"/> becomes a
/// <see cref="DirectoryOf{T}"/> item via a single Core extension method.
/// Format extensions don't need to ship their own directory variant.
/// </summary>
public static class DirectoryOfExtensions
{
  /// <summary>
  /// Lift a per-file format builder into a directory-of-files builder.
  /// </summary>
  /// <typeparam name="T">Per-file payload type.</typeparam>
  /// <typeparam name="TBuilder">
  /// Concrete per-file builder type — typically inferred. Must implement
  /// <see cref="IFileItemBuilder{T}"/>.
  /// </typeparam>
  /// <param name="anchor">The catalog anchor.</param>
  /// <param name="chooseFormat">
  /// Lambda that picks the format on a synthetic per-file anchor:
  /// <c>file =&gt; file.Csv().WithNullValues(["NA"])</c>.
  /// </param>
  public static DirectoryOfBuilder<T> Directory<T, TBuilder>(
    this ItemAnchor<DirectoryOf<T>> anchor,
    Func<ItemAnchor<T>, TBuilder> chooseFormat
  )
    where T : notnull
    where TBuilder : IFileItemBuilder<T>
  {
    if (anchor is null) throw new ArgumentNullException(nameof(anchor));
    if (chooseFormat is null) throw new ArgumentNullException(nameof(chooseFormat));

    var fileAnchor = new ItemAnchor<T>(anchor.Label);
    var fileBuilder = chooseFormat(fileAnchor);
    if (fileBuilder is null)
      throw new InvalidOperationException(
        $"DirectoryOf item '{anchor.Label}': format-picker lambda returned null."
      );
    return new DirectoryOfBuilder<T>(anchor.Label, fileBuilder);
  }
}
