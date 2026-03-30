using Flowthru.Abstractions;
using Flowthru.Data.Storage;
using Flowthru.Data.Storage.Container;
using Flowthru.Data.Storage.Format;
using Flowthru.Data.Storage.Medium;

namespace Flowthru.Data;

/// <summary>
/// Extension point for <see cref="CatalogEntries.Enumerable"/> factory methods.
/// </summary>
/// <remarks>
/// <para>
/// IEnumerable&lt;T&gt; is the standard .NET collection interface.
/// </para>
/// <para>
/// <strong>Characteristics:</strong>
/// </para>
/// <list type="bullet">
/// <item><strong>Lazy evaluation:</strong> LINQ queries deferred until enumeration</item>
/// <item><strong>Re-enumerable:</strong> Can cause side effects (multiple DB hits, file reads)</item>
/// <item><strong>Mutable:</strong> Underlying collection can be modified</item>
/// <item><strong>Standard .NET:</strong> Works with all .NET libraries</item>
/// </list>
/// <para>
/// <strong>Use Cases:</strong>
/// </para>
/// <list type="bullet">
/// <item>Standard data processing pipelines (90% of cases)</item>
/// <item>Interop with .NET libraries expecting IEnumerable</item>
/// <item>LINQ query composition</item>
/// <item>Large datasets where you'll enumerate only once</item>
/// </list>
/// <para>
/// Format-specific factory methods (CSV, Parquet, Excel) are provided as extension
/// methods by their respective packages. Add extension methods to this type to
/// register new formats.
/// </para>
/// </remarks>
public sealed class EnumerableCatalogEntries
{
  internal EnumerableCatalogEntries() { }

  /// <summary>
  /// Creates a JSON file catalog entry with IEnumerable container for collections.
  /// </summary>
  /// <typeparam name="TRow">Row schema type (must be structured-serializable)</typeparam>
  /// <param name="label">Unique catalog label for DAG resolution</param>
  /// <param name="filePath">Path to JSON file</param>
  /// <returns>Catalog entry with file + JSON + IEnumerable composition</returns>
  /// <remarks>
  /// <para>
  /// <strong>Requirements:</strong>
  /// </para>
  /// <list type="bullet">
  /// <item>TRow must implement IStructuredSerializable</item>
  /// <item>TRow supports both flat and nested schemas</item>
  /// </list>
  /// <para>
  /// <strong>Supports:</strong>
  /// </para>
  /// <list type="bullet">
  /// <item>Traditional schemas with parameterless constructors</item>
  /// <item>Modern schemas with required properties (C# 11+)</item>
  /// <item>Positional records with primary constructors</item>
  /// </list>
  /// <para>
  /// <strong>Serialization:</strong> JSON array format for collections
  /// </para>
  /// </remarks>
  public CatalogEntry<IEnumerable<TRow>> Json<TRow>(string label, string filePath)
    where TRow : notnull, IStructuredSerializable
  {
    var medium = new FileStorageMedium(filePath);
    var format = new JsonFormatSerializer<TRow>();
    var container = new EnumerableContainerAdapter<TRow>();
    var storage = new ComposedStorageAdapter<IEnumerable<TRow>, TRow>(medium, format, container);

    return new CatalogEntry<IEnumerable<TRow>>(label, storage);
  }

  /// <summary>
  /// Creates an in-memory transient catalog entry with IEnumerable container.
  /// </summary>
  /// <typeparam name="TRow">Row schema type</typeparam>
  /// <param name="label">Unique catalog label for DAG resolution</param>
  /// <returns>Catalog entry with memory storage (no serialization)</returns>
  /// <remarks>
  /// <para>
  /// <strong>Use Case:</strong> Intermediate pipeline data that doesn't need persistence
  /// </para>
  /// <para>
  /// <strong>Storage Traits:</strong>
  /// </para>
  /// <list type="bullet">
  /// <item>IsPersistent: false (data lost when process ends)</item>
  /// </list>
  /// </remarks>
  public CatalogEntry<IEnumerable<TRow>> Memory<TRow>(string label)
  {
    var storage = new MemoryStorageAdapter<IEnumerable<TRow>>();
    return new CatalogEntry<IEnumerable<TRow>>(label, storage);
  }
}
