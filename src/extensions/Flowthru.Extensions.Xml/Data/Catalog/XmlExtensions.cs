using Flowthru.Data.Schema;
using Flowthru.Data.Storage;
using Flowthru.Data.Storage.Xml;

namespace Flowthru.Data.Catalog;

/// <summary>
/// XML item-builder extensions on <see cref="ItemAnchor{T}"/>. XML
/// in this extension is document-mode only — one file holds one
/// <typeparamref name="T"/>, and a directory holds N independent
/// documents.
/// </summary>
public static class XmlExtensions
{
  /// <summary>Build a single XML-document catalog item.</summary>
  public static XmlBuilder<T> Xml<T>(this ItemAnchor<T> anchor)
    where T : notnull, IStructuredSerializable =>
    new(anchor.Label);

  /// <summary>Build a directory-of-XML-documents catalog item.</summary>
  public static XmlDirectoryBuilder<T> Xml<T>(this ItemAnchor<Directory<T>> anchor)
    where T : notnull, IStructuredSerializable =>
    new(anchor.Label);
}

/// <summary>Tier-1 builder for a single-XML-file catalog item.</summary>
public sealed class XmlBuilder<T> where T : notnull, IStructuredSerializable
{
  private readonly string _label;
  private string? _path;

  internal XmlBuilder(string label)
  {
    _label = label;
  }

  /// <summary>Set the filesystem path for this XML file.</summary>
  public XmlBuilder<T> AtPath(string path)
  {
    if (string.IsNullOrWhiteSpace(path))
      throw new ArgumentException("Path cannot be null or whitespace.", nameof(path));
    _path = path;
    return this;
  }

  /// <summary>Materialise the <see cref="IItem{T}"/>.</summary>
  public IItem<T> Build()
  {
    if (_path is null)
      throw new InvalidOperationException(
        $"Xml item '{_label}' requires AtPath(...) before Build()."
      );
    return new Item<T>(_label, new SingletonXmlAdapter<T>(_path));
  }
}

/// <summary>Tier-1 builder for a directory-of-XML-files catalog item.</summary>
public sealed class XmlDirectoryBuilder<T> where T : notnull, IStructuredSerializable
{
  private readonly string _label;
  private string? _directoryPath;
  private string _filePattern = "*.xml";

  internal XmlDirectoryBuilder(string label)
  {
    _label = label;
  }

  /// <summary>Set the directory path holding the XML files.</summary>
  public XmlDirectoryBuilder<T> AtPath(string directoryPath)
  {
    if (string.IsNullOrWhiteSpace(directoryPath))
      throw new ArgumentException("Path cannot be null or whitespace.", nameof(directoryPath));
    _directoryPath = directoryPath;
    return this;
  }

  /// <summary>Override the default <c>*.xml</c> filename pattern.</summary>
  public XmlDirectoryBuilder<T> WithFilePattern(string filePattern)
  {
    if (string.IsNullOrWhiteSpace(filePattern))
      throw new ArgumentException("File pattern cannot be null or whitespace.", nameof(filePattern));
    _filePattern = filePattern;
    return this;
  }

  /// <summary>Materialise the <see cref="IItem{T}"/>.</summary>
  public IItem<Directory<T>> Build()
  {
    if (_directoryPath is null)
      throw new InvalidOperationException(
        $"Xml directory item '{_label}' requires AtPath(...) before Build()."
      );
    return new Item<Directory<T>>(
      _label,
      new DirectoryStorageAdapter<T>(
        _directoryPath,
        _filePattern,
        perFilePath => new SingletonXmlAdapter<T>(perFilePath)
      )
    );
  }
}
