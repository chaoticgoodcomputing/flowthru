using Flowthru.Core.Abstractions;
using Flowthru.Core.Data.Storage;

namespace Flowthru.Core.Data;

/// <summary>
/// Factory methods for creating <see cref="Item{T}"/> instances with XML storage adapters.
/// </summary>
/// <remarks>
/// <para>
/// Mirrors the <c>EFCoreItemFactory</c> / <c>GqlItemFactory</c> pattern: a parallel static
/// factory class for extension-specific storage types, since <c>ItemFactory.Single</c> is a
/// nested static class and cannot be extended from outside the core assembly.
/// </para>
/// <para>
/// The <see cref="Enumerable"/> factory also extends <see cref="ItemFactory.Enumerable"/>
/// via an extension method on <see cref="EnumerableItemFactory"/>.
/// </para>
/// </remarks>
public static partial class XmlItemFactory
{
  /// <summary>
  /// Factory methods for single XML document catalog entries.
  /// </summary>
  public static partial class Single
  {
    /// <summary>
    /// Creates an XML file catalog entry for a single document.
    /// </summary>
    /// <typeparam name="T">The document type. Must implement <see cref="IStructuredSerializable"/>.</typeparam>
    /// <param name="label">Unique catalog label for DAG resolution</param>
    /// <param name="filePath">Path to the XML file</param>
    /// <returns>Catalog entry backed by a single XML file</returns>
    /// <remarks>
    /// Decorate <typeparamref name="T"/> with <c>[XmlRoot]</c>, <c>[XmlElement]</c>, and
    /// <c>[XmlAttribute]</c> as required by <see cref="System.Xml.Serialization.XmlSerializer"/>.
    /// </remarks>
    public static Item<T> Xml<T>(string label, string filePath)
      where T : IStructuredSerializable
    {
      var storage = new SingletonXmlStorageAdapter<T>(filePath);
      return new Item<T>(label, storage);
    }
  }
}

/// <summary>
/// Extension methods that add XML directory support to <see cref="ItemFactory.Enumerable"/>.
/// </summary>
public static class XmlEnumerableItemExtensions
{
  /// <summary>
  /// Creates a catalog entry over a directory of XML files where each file deserialises to
  /// one <typeparamref name="T"/>. Read produces a <see cref="Directory{T}"/> keyed by full
  /// file path; Save writes one XML file per entry, deleting any existing <c>*.xml</c> in
  /// the directory first so re-runs are deterministic.
  /// </summary>
  /// <typeparam name="T">The document type for each XML file.</typeparam>
  /// <param name="_">The enumerable catalog entries factory (from <see cref="ItemFactory.Enumerable"/>)</param>
  /// <param name="label">Unique catalog label for DAG resolution</param>
  /// <param name="directoryPath">Path to the directory containing XML files</param>
  /// <remarks>
  /// All files must share the same schema. This is intentionally not a partitioning
  /// primitive — each file represents an independent unit. If you need to chunk a single
  /// logical dataset across files, do that in a step before write and reassemble in a step
  /// after read.
  /// </remarks>
  public static Item<Directory<T>> XmlDocuments<T>(
    this EnumerableItemFactory _,
    string label,
    string directoryPath
  )
    where T : IStructuredSerializable
  {
    IStorageAdapter<T> PerFileAdapter(string path) => new SingletonXmlStorageAdapter<T>(path);

    return new Item<Directory<T>>(
      label,
      new DirectoryStorageAdapter<T>(
        directoryPath: directoryPath,
        filePattern: "*.xml",
        perFileAdapter: PerFileAdapter
      )
    );
  }
}
