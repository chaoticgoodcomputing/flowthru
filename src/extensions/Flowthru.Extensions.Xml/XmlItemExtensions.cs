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
  /// Creates a read-only catalog entry that deserializes all <c>*.xml</c> files in a directory,
  /// yielding each as an <see cref="XmlDocument{T}"/> that carries the source file name.
  /// </summary>
  /// <typeparam name="T">The document type for each XML file.</typeparam>
  /// <param name="_">The enumerable catalog entries factory (from <see cref="ItemFactory.Enumerable"/>)</param>
  /// <param name="label">Unique catalog label for DAG resolution</param>
  /// <param name="directoryPath">Path to the directory containing XML files</param>
  /// <returns>Read-only catalog entry yielding one <see cref="XmlDocument{T}"/> per file</returns>
  /// <remarks>
  /// <para>
  /// Files are processed in lexicographic order for deterministic output across runs.
  /// The <see cref="XmlDocument{T}.FileName"/> carries the file name without directory path,
  /// allowing downstream steps to derive semantic meaning from the naming convention.
  /// </para>
  /// <para>
  /// This entry is <strong>read-only</strong> — attempting to save will fail with
  /// <see cref="NotSupportedException"/>.
  /// </para>
  /// </remarks>
  public static Item<IEnumerable<XmlDocument<T>>> XmlDocuments<T>(
    this EnumerableItemFactory _,
    string label,
    string directoryPath
  )
    where T : IStructuredSerializable
  {
    var storage = new XmlDirectoryStorageAdapter<T>(directoryPath);
    return new Item<IEnumerable<XmlDocument<T>>>(label, storage);
  }
}
