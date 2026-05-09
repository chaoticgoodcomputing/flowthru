using Flowthru.Data.Schema;
using Flowthru.Data.Storage;
using Flowthru.Data.Storage.Xml;

namespace Flowthru.Data.Catalog;

/// <summary>
/// XML item-builder extensions on <see cref="ItemAnchor{T}"/>. XML
/// in this extension is document-mode only — one file holds one
/// <typeparamref name="T"/>, and a directory (constructed via the
/// universal <see cref="DirectoryOfExtensions.Directory{T, TBuilder}"/>
/// lift) holds N independent documents.
/// </summary>
public static class XmlExtensions
{
  /// <summary>Build a single XML-document catalog item.</summary>
  public static XmlBuilder<T> Xml<T>(this ItemAnchor<T> anchor)
    where T : notnull, IStructuredSerializable =>
    new(anchor.Label);
}

/// <summary>Tier-1 builder for an XML catalog item (single document or, via lift, directory).</summary>
public sealed class XmlBuilder<T>
  : IFileItemBuilder<T>
  where T : notnull, IStructuredSerializable
{
  private readonly string _label;
  private string? _path;

  internal XmlBuilder(string label)
  {
    _label = label;
  }

  /// <inheritdoc/>
  public string Label => _label;

  /// <inheritdoc/>
  public string DefaultFilePattern => "*.xml";

  /// <summary>Set the filesystem path for this XML file.</summary>
  public XmlBuilder<T> AtPath(string path)
  {
    if (string.IsNullOrWhiteSpace(path))
      throw new ArgumentException("Path cannot be null or whitespace.", nameof(path));
    _path = path;
    return this;
  }

  /// <inheritdoc/>
  public IStorageAdapter<T> CreateAdapterForFile(string filePath) =>
    new SingletonXmlAdapter<T>(filePath);

  /// <summary>Materialise the <see cref="IItem{T}"/>.</summary>
  public IItem<T> Build()
  {
    if (_path is null)
      throw new InvalidOperationException(
        $"Xml item '{_label}' requires AtPath(...) before Build()."
      );
    return new Item<T>(_label, CreateAdapterForFile(_path));
  }
}
