using Flowthru.Core.Abstractions;
using Flowthru.Core.Data.Storage;
using Flowthru.Core.Data.Storage.Container;
using Flowthru.Core.Data.Storage.Format;
using Flowthru.Core.Data.Storage.Medium;

namespace Flowthru.Core.Data;

/// <summary>
/// Extension point for <see cref="ItemFactory.Enumerable"/> factory methods.
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
/// <item>Standard data processing flows (90% of cases)</item>
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
public sealed class EnumerableItemFactory
{
  internal EnumerableItemFactory() { }

  /// <summary>
  /// Creates a JSON file catalog item with IEnumerable container for collections.
  /// </summary>
  /// <typeparam name="TRow">Row schema type (must be structured-serializable)</typeparam>
  /// <param name="label">Unique catalog label for DAG resolution</param>
  /// <param name="filePath">Path or URI to JSON file</param>
  /// <param name="resolver">
  /// Optional resolver for remote URIs (e.g., <c>https://</c>, <c>sftp://</c>).
  /// Falls back to <see cref="Flowthru.Core.Data.Storage.Medium.FileStorageMedium"/> when <c>null</c>.
  /// </param>
  /// <param name="medium">
  /// Explicit medium override. Takes precedence over <paramref name="resolver"/> when both
  /// are supplied. Use for per-entry customisation or direct injection in tests.
  /// </param>
  /// <returns>Catalog item with file + JSON + IEnumerable composition</returns>
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
  public Item<IEnumerable<TRow>> Json<TRow>(
    string label,
    string filePath,
    IStorageMediumResolver? resolver = null,
    IStorageMedium? medium = null
  )
    where TRow : notnull, IStructuredSerializable
  {
    var resolvedMedium = medium ?? resolver?.Resolve(filePath) ?? new FileStorageMedium(filePath);
    var format = new JsonFormatSerializer<TRow>();
    var container = new EnumerableContainerAdapter<TRow>();
    var storage = new ComposedStorageAdapter<IEnumerable<TRow>, TRow>(
      resolvedMedium,
      format,
      container
    );

    return new Item<IEnumerable<TRow>>(label, storage);
  }

  /// <summary>
  /// Creates an in-memory transient catalog item with IEnumerable container.
  /// </summary>
  /// <typeparam name="TRow">Row schema type</typeparam>
  /// <param name="label">Unique catalog label for DAG resolution</param>
  /// <returns>Catalog item with memory storage (no serialization)</returns>
  /// <remarks>
  /// <para>
  /// <strong>Use Case:</strong> Intermediate Flow data that doesn't need persistence
  /// </para>
  /// <para>
  /// <strong>Storage Traits:</strong>
  /// </para>
  /// <list type="bullet">
  /// <item>IsPersistent: false (data lost when process ends)</item>
  /// </list>
  /// </remarks>
  public Item<IEnumerable<TRow>> Memory<TRow>(string label)
  {
    var storage = new MemoryStorageAdapter<IEnumerable<TRow>>();
    return new Item<IEnumerable<TRow>>(label, storage);
  }

  /// <summary>
  /// Creates a catalog entry over a directory of JSON files where each file is a JSON
  /// array of <typeparamref name="TRow"/> (mirrors the <see cref="Json{TRow}"/> single-file
  /// shape). Read produces a <see cref="Directory{T}"/> keyed by full file path with
  /// <c>IEnumerable&lt;TRow&gt;</c> values; Save writes one JSON file per entry, deleting
  /// existing <c>*.json</c> in the directory first so re-runs are deterministic.
  /// </summary>
  /// <typeparam name="TRow">Row schema type (must be structured-serializable)</typeparam>
  /// <param name="label">Unique catalog label for DAG resolution</param>
  /// <param name="directoryPath">Path to the directory containing the JSON array files</param>
  /// <remarks>
  /// All files must share the same schema. This is intentionally not a partitioning
  /// primitive — each file represents an independent unit. Use <see cref="JsonDocuments{T}"/>
  /// for the singleton-document-per-file shape (one JSON object per file).
  /// </remarks>
  public Item<Directory<IEnumerable<TRow>>> JsonDirectory<TRow>(
    string label,
    string directoryPath
  )
    where TRow : notnull, IStructuredSerializable
  {
    var format = new JsonFormatSerializer<TRow>();
    var container = new EnumerableContainerAdapter<TRow>();

    IStorageAdapter<IEnumerable<TRow>> PerFileAdapter(string path) =>
      new ComposedStorageAdapter<IEnumerable<TRow>, TRow>(
        new FileStorageMedium(path),
        format,
        container
      );

    return new Item<Directory<IEnumerable<TRow>>>(
      label,
      new DirectoryStorageAdapter<IEnumerable<TRow>>(
        directoryPath: directoryPath,
        filePattern: "*.json",
        perFileAdapter: PerFileAdapter
      )
    );
  }

  /// <summary>
  /// Creates a catalog entry over a directory of singleton-JSON-document files (one JSON
  /// object per file). Read produces a <see cref="Directory{T}"/> keyed by full file path
  /// with deserialised <typeparamref name="T"/> values; Save writes one JSON file per
  /// entry, deleting existing <c>*.json</c> in the directory first so re-runs are
  /// deterministic.
  /// </summary>
  /// <typeparam name="T">Document type (must be structured-serializable)</typeparam>
  /// <param name="label">Unique catalog label for DAG resolution</param>
  /// <param name="directoryPath">Path to the directory containing the JSON document files</param>
  /// <remarks>
  /// Use <see cref="JsonDirectory{TRow}"/> for the row-collection-per-file shape (each file
  /// is a JSON array). This entry's per-file contract is one JSON object per file —
  /// parallel to <see cref="ItemFactory.Single.Json{T}"/>.
  /// </remarks>
  public Item<Directory<T>> JsonDocuments<T>(string label, string directoryPath)
    where T : IStructuredSerializable
  {
    IStorageAdapter<T> PerFileAdapter(string path) => new SingletonJsonStorageAdapter<T>(path);

    return new Item<Directory<T>>(
      label,
      new DirectoryStorageAdapter<T>(
        directoryPath: directoryPath,
        filePattern: "*.json",
        perFileAdapter: PerFileAdapter
      )
    );
  }

  /// <summary>
  /// Creates a catalog entry over a directory of binary files (one blob per file). Read
  /// produces a <see cref="Directory{T}"/> keyed by full file path with <c>byte[]</c>
  /// values; Save writes one file per entry, deleting any existing files matching the
  /// pattern first so re-runs are deterministic.
  /// </summary>
  /// <param name="label">Unique catalog label for DAG resolution</param>
  /// <param name="directoryPath">Path to the directory.</param>
  /// <param name="filePattern">
  /// Glob for matching files (default <c>"*"</c> — every file in the directory). Pass
  /// e.g. <c>"*.png"</c> when the directory hosts a single binary format alongside other
  /// content that should be ignored.
  /// </param>
  /// <remarks>
  /// This is intentionally not a partitioning primitive — each file represents an
  /// independent binary unit (a PNG, a PDF, a serialised model). If you need to chunk a
  /// single logical artifact across files, do that in a step before write and reassemble
  /// in a step after read.
  /// </remarks>
  public Item<Directory<byte[]>> BinaryDirectory(
    string label,
    string directoryPath,
    string filePattern = "*"
  )
  {
    var storage = new DirectoryStorageAdapter<byte[]>(
      directoryPath: directoryPath,
      filePattern: filePattern,
      perFileAdapter: path => new BinaryFileStorageAdapter(path)
    );
    return new Item<Directory<byte[]>>(label, storage);
  }
}
