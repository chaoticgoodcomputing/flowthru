using Flowthru.Data.Schema;
using Flowthru.Data.Storage;
using Flowthru.Data.Storage.Xml;

namespace Flowthru.Data.Catalog;

/// <summary>
/// XML item-builder extensions on <see cref="ItemAnchor{T}"/>. XML
/// in this extension is document-mode only — one file holds one
/// <c>T</c>, and a directory (constructed via the
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
  private IStorageMediumResolver? _resolver;

  internal XmlBuilder(string label)
  {
    _label = label;
  }

  /// <inheritdoc/>
  public string Label => _label;

  /// <inheritdoc/>
  public string DefaultFilePattern => "*.xml";

  /// <summary>
  /// Set the filesystem path for this XML file. The XML extension is
  /// document-mode and currently only supports filesystem-backed
  /// resources — non-file schemes throw at <see cref="Build"/> time.
  /// </summary>
  public XmlBuilder<T> AtPath(string path)
  {
    if (string.IsNullOrWhiteSpace(path))
      throw new ArgumentException("Path cannot be null or whitespace.", nameof(path));
    _path = path;
    return this;
  }

  /// <summary>
  /// Provide an optional <see cref="IStorageMediumResolver"/>. The XML
  /// extension currently only supports filesystem-backed media; this
  /// hook exists for parity with the other format builders and may
  /// resolve through a resolver-registered file provider in the
  /// future. When omitted, the builder consults
  /// <see cref="StorageMediumResolver.Current"/> and finally falls
  /// back to <see cref="StorageMediumResolver.Filesystem"/>.
  /// </summary>
  public XmlBuilder<T> WithResolver(IStorageMediumResolver resolver)
  {
    _resolver = resolver ?? throw new ArgumentNullException(nameof(resolver));
    return this;
  }

  /// <inheritdoc/>
  public IStorageAdapter<T> CreateAdapterForFile(string filePath)
  {
    var resolver =
      _resolver ?? StorageMediumResolver.Current ?? StorageMediumResolver.Filesystem;
    // The XML adapter is document-only and only knows how to read/write
    // files today. Resolve the path so non-file schemes surface their
    // standard diagnostic; reject anything else explicitly here.
    var medium = resolver.Resolve(filePath);
    if (medium is not FileStorageMedium fileMedium)
    {
      throw new InvalidOperationException(
        $"Xml item '{_label}' resolves to a non-filesystem medium "
        + $"({medium.GetType().Name}). The XML extension is document-mode "
        + "and only supports filesystem-backed resources today."
      );
    }
    return new SingletonXmlAdapter<T>(fileMedium.FilePath);
  }

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
