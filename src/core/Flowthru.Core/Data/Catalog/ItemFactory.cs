using Flowthru.Data.Schema;
using Flowthru.Data.Storage;

namespace Flowthru.Data.Catalog;

/// <summary>
/// Smart constructors for <see cref="IItem{T}"/>. Core ships JSON over
/// filesystem and in-memory variants; format extensions add their own
/// constructors as extension methods on <see cref="EnumerableItemFactory"/>,
/// <see cref="SingletonItemFactory"/>, and
/// <see cref="DirectoryItemFactory"/> (e.g.,
/// <c>ItemFactory.Enumerable.Csv&lt;T&gt;</c> from <c>Flowthru.Csv</c>).
/// </summary>
/// <remarks>
/// The nested factory types are stateless singletons exposed via
/// properties on <see cref="ItemFactory"/>. Modelling them as instances
/// (rather than nested static classes) lets extension authors hook
/// their own smart constructors as extension methods on the factory
/// type, dispatched through the same <c>ItemFactory.Enumerable.Csv(...)</c>
/// surface end users learned from Core's built-ins.
/// </remarks>
public static class ItemFactory
{
  /// <summary>Smart-constructor surface for items that hold collections of rows.</summary>
  public static EnumerableItemFactory Enumerable { get; } = new();

  /// <summary>Smart-constructor surface for items that hold a single value.</summary>
  public static SingletonItemFactory Singleton { get; } = new();

  /// <summary>Smart-constructor surface for items backed by a directory of same-schema files.</summary>
  public static DirectoryItemFactory Directory { get; } = new();
}

/// <summary>
/// Smart-constructor surface for <see cref="IItem{T}"/> values whose
/// container is <see cref="IEnumerable{TRow}"/>. Core ships JSON and
/// in-memory variants; extensions add their own as extension methods.
/// </summary>
public sealed class EnumerableItemFactory
{
  internal EnumerableItemFactory() { }

  /// <summary>
  /// JSON file holding an array of <typeparamref name="TRow"/> rows.
  /// Composes a resolver-dispatched <see cref="IStorageMedium"/> +
  /// <see cref="JsonFormatSerializer{TRow}"/> +
  /// <see cref="EnumerableContainerAdapter{T}"/>.
  /// </summary>
  /// <param name="label">Catalog label for DAG resolution.</param>
  /// <param name="filePath">
  /// Path or URI to the JSON source. Bare paths and <c>file://</c>
  /// URIs always resolve to a <see cref="FileStorageMedium"/>; other
  /// schemes (e.g. <c>https://</c>) require a corresponding provider
  /// registered with <paramref name="resolver"/>.
  /// </param>
  /// <param name="resolver">
  /// Optional storage-medium resolver. When null, falls back to
  /// <see cref="StorageMediumResolver.Filesystem"/>.
  /// </param>
  public IItem<IEnumerable<TRow>> Json<TRow>(
    string label,
    string filePath,
    IStorageMediumResolver? resolver = null
  )
    where TRow : notnull, IStructuredSerializable
  {
    var effective =
      resolver ?? StorageMediumResolver.Current ?? StorageMediumResolver.Filesystem;
    var medium = effective.Resolve(filePath);
    return new Item<IEnumerable<TRow>>(
      label,
      new ComposedStorageAdapter<IEnumerable<TRow>, TRow>(
        medium,
        new JsonFormatSerializer<TRow>(),
        new EnumerableContainerAdapter<TRow>()
      )
    );
  }

  /// <summary>In-memory collection of <typeparamref name="TRow"/> rows.</summary>
  public IItem<IEnumerable<TRow>> Memory<TRow>(string label)
    where TRow : notnull =>
    new Item<IEnumerable<TRow>>(label, new MemoryStorageAdapter<IEnumerable<TRow>>());
}

/// <summary>
/// Smart-constructor surface for <see cref="IItem{T}"/> values whose
/// container holds a single value (no collection wrapper). Core ships
/// JSON and in-memory variants.
/// </summary>
public sealed class SingletonItemFactory
{
  internal SingletonItemFactory() { }

  /// <summary>
  /// JSON file holding a single <typeparamref name="T"/> value (object,
  /// not an array). When <paramref name="resolver"/> is null, the
  /// ambient <see cref="StorageMediumResolver.Current"/> is consulted
  /// (typically pushed by <see cref="CatalogAbstract.CreateItem{T}"/>);
  /// failing that, the call resolves directly against the local
  /// filesystem — preserving the historical bare-path fast path.
  /// </summary>
  public IItem<T> Json<T>(
    string label,
    string filePath,
    IStorageMediumResolver? resolver = null
  )
    where T : notnull, IStructuredSerializable
  {
    var effective = resolver ?? StorageMediumResolver.Current;
    if (effective is null)
    {
      // Preserve the original bare-path fast path when no resolver is
      // in scope — go straight to the file-backed adapter.
      return new Item<T>(label, new SingletonJsonAdapter<T>(filePath));
    }
    var medium = effective.Resolve(filePath);
    return new Item<T>(
      label,
      medium is FileStorageMedium fileMedium
        ? new SingletonJsonAdapter<T>(fileMedium.FilePath)
        : new SingletonJsonAdapter<T>(medium)
    );
  }

  /// <summary>In-memory singleton holding a single <typeparamref name="T"/> value.</summary>
  public IItem<T> Memory<T>(string label)
    where T : notnull =>
    new Item<T>(label, new MemoryStorageAdapter<T>());
}

/// <summary>
/// Smart-constructor surface for <see cref="IItem{T}"/> values backed
/// by a directory of same-schema files. Each file is one independent
/// unit; <see cref="DirectoryOf{T}"/> is the resulting key→payload view.
/// </summary>
public sealed class DirectoryItemFactory
{
  internal DirectoryItemFactory() { }

  /// <summary>
  /// Directory of JSON files, each containing an array of
  /// <typeparamref name="TRow"/> rows. Per-file adapter is the same
  /// composed JSON adapter as <see cref="EnumerableItemFactory.Json{TRow}"/>.
  /// </summary>
  public IItem<DirectoryOf<IEnumerable<TRow>>> JsonArrays<TRow>(
    string label,
    string directoryPath,
    string filePattern = "*.json"
  )
    where TRow : notnull, IStructuredSerializable =>
    new Item<DirectoryOf<IEnumerable<TRow>>>(
      label,
      new DirectoryStorageAdapter<IEnumerable<TRow>>(
        directoryPath,
        filePattern,
        perFilePath => new ComposedStorageAdapter<IEnumerable<TRow>, TRow>(
          new FileStorageMedium(perFilePath),
          new JsonFormatSerializer<TRow>(),
          new EnumerableContainerAdapter<TRow>()
        )
      )
    );

  /// <summary>
  /// Directory of JSON files, each containing one
  /// <typeparamref name="T"/> document (object, not array).
  /// Per-file adapter is the same singleton-JSON adapter as
  /// <see cref="SingletonItemFactory.Json{T}"/>.
  /// </summary>
  public IItem<DirectoryOf<T>> JsonDocuments<T>(
    string label,
    string directoryPath,
    string filePattern = "*.json"
  )
    where T : notnull, IStructuredSerializable =>
    new Item<DirectoryOf<T>>(
      label,
      new DirectoryStorageAdapter<T>(
        directoryPath,
        filePattern,
        perFilePath => new SingletonJsonAdapter<T>(perFilePath)
      )
    );
}
