using Flowthru.Data.Schema;
using Flowthru.Data.Storage;
using Flowthru.Data.Storage.Xml;

namespace Flowthru.Data.Catalog;

/// <summary>
/// Extension methods that contribute XML smart constructors into
/// <see cref="ItemFactory.Singleton"/> and
/// <see cref="ItemFactory.Directory"/>. End users see them as
/// <c>ItemFactory.Singleton.Xml&lt;T&gt;(...)</c> and
/// <c>ItemFactory.Directory.Xml&lt;T&gt;(...)</c> via a single
/// <c>using Flowthru.Data.Catalog;</c> import.
/// </summary>
/// <remarks>
/// XML in this extension is document-mode only — one file holds one
/// <typeparamref name="T"/>, and a directory holds N independent
/// documents. There is no row-streaming variant on
/// <see cref="EnumerableItemFactory"/>; that would require a
/// dedicated <c>XmlFormatSerializer&lt;TRow&gt;</c> implementing
/// <c>IFormatRowReader&lt;TRow&gt;</c> and is not in this extension's
/// scope.
/// </remarks>
public static class XmlItemFactoryExtensions
{
  /// <summary>
  /// Single XML file holding one <typeparamref name="T"/> document.
  /// Decorate <typeparamref name="T"/> with
  /// <see cref="System.Xml.Serialization.XmlRootAttribute"/>,
  /// <see cref="System.Xml.Serialization.XmlElementAttribute"/>, and
  /// <see cref="System.Xml.Serialization.XmlAttributeAttribute"/> as
  /// required by <see cref="System.Xml.Serialization.XmlSerializer"/>.
  /// </summary>
  /// <typeparam name="T">Document type. Must implement <see cref="IStructuredSerializable"/>.</typeparam>
  /// <param name="_">The factory anchor — discriminates extension target.</param>
  /// <param name="label">Catalog label for DAG resolution.</param>
  /// <param name="filePath">Path to the XML file.</param>
  public static IItem<T> Xml<T>(
    this SingletonItemFactory _,
    string label,
    string filePath
  )
    where T : notnull, IStructuredSerializable =>
    new Item<T>(label, new SingletonXmlAdapter<T>(filePath));

  /// <summary>
  /// Directory of XML files, each containing one
  /// <typeparamref name="T"/> document. Save hard-deletes existing
  /// files matching <paramref name="filePattern"/> first so re-runs
  /// are deterministic.
  /// </summary>
  /// <remarks>
  /// All files must share the same schema. This is intentionally not
  /// a partitioning primitive — each file represents an independent
  /// unit. To chunk one logical dataset across files, do that as a
  /// step before write and reassemble in a step after read.
  /// </remarks>
  public static IItem<Directory<T>> Xml<T>(
    this DirectoryItemFactory _,
    string label,
    string directoryPath,
    string filePattern = "*.xml"
  )
    where T : notnull, IStructuredSerializable =>
    new Item<Directory<T>>(
      label,
      new DirectoryStorageAdapter<T>(
        directoryPath,
        filePattern,
        perFilePath => new SingletonXmlAdapter<T>(perFilePath)
      )
    );
}
