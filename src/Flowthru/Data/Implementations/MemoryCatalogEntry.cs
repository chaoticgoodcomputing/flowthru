using LanguageExt;
using static LanguageExt.Prelude;

namespace Flowthru.Data.Implementations;

/// <summary>
/// In-memory catalog entry for transient storage.
/// Supports both singleton objects and collections.
/// </summary>
/// <typeparam name="T">
/// The data type to store.
/// Can be a singleton type (e.g., LinearRegressionModel) or
/// a collection type (e.g., Seq&lt;FeatureRow&gt;).
/// </typeparam>
/// <remarks>
/// <para>
/// <strong>Unified Design:</strong> This single implementation replaces both
/// MemoryCatalogObject and MemoryCatalogDataset. The type parameter T determines
/// whether this stores a singleton or collection.
/// </para>
/// <para>
/// <strong>Use Cases:</strong>
/// - Intermediate pipeline data that doesn't need persistence
/// - Test data that doesn't require file I/O
/// - Temporary results between pipeline stages
/// - ML models, metrics, or any ephemeral data
/// </para>
/// <para>
/// <strong>Thread Safety:</strong> This implementation is thread-safe for concurrent
/// Save() and Load() operations.
/// </para>
/// <para>
/// <strong>Lifetime:</strong> Data persists only for the lifetime of this instance.
/// Data is lost when the application terminates.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Singleton usage
/// var modelEntry = new MemoryCatalogEntry&lt;LinearRegressionModel&gt;("model");
/// await modelEntry.Save(model).RunAsync();
/// var loadedModel = await modelEntry.Load().RunAsync();
///
/// // Collection usage
/// var dataEntry = new MemoryCatalogEntry&lt;Seq&lt;FeatureRow&gt;&gt;("features");
/// await dataEntry.Save(features.ToSeq()).RunAsync();
/// var loadedFeatures = await dataEntry.Load().RunAsync();
/// </code>
/// </example>
public class MemoryCatalogEntry<T> : CatalogEntryBase<T>
{
  private T? _data;
  private bool _hasData;
  private readonly object _lock = new();

  /// <summary>
  /// Creates a new in-memory catalog entry.
  /// </summary>
  /// <param name="key">Unique identifier for this catalog entry</param>
  public MemoryCatalogEntry(string key)
    : base(key) { }

  /// <inheritdoc/>
  public override IO<T> Load()
  {
    return IO.liftAsync(async () =>
    {
      lock (_lock)
      {
        if (!_hasData)
        {
          throw new InvalidOperationException(
            $"Cannot load from memory catalog entry '{Key}' - no data has been saved yet"
          );
        }
        return _data!;
      }
    });
  }

  /// <inheritdoc/>
  public override IO<Unit> Save(T data)
  {
    return IO.liftAsync(async () =>
    {
      lock (_lock)
      {
        _data = data;
        _hasData = true;
        return unit;
      }
    });
  }

  /// <inheritdoc/>
  public override IO<bool> Exists()
  {
    return IO.liftAsync(async () =>
    {
      lock (_lock)
      {
        return _hasData;
      }
    });
  }
}
