using System.Collections.Concurrent;
using System.Linq.Expressions;
using System.Reflection;

namespace Flowthru.Data;

/// <summary>
/// Base class for strongly-typed catalog implementations with automatic property caching.
/// </summary>
/// <remarks>
/// <para>
/// <strong>Problem Solved:</strong>
/// Expression-bodied properties (<c>Property => new Entry()</c>) create new instances on each access,
/// breaking DAG dependency resolution which relies on object identity.
/// </para>
/// <para>
/// <strong>Solution:</strong>
/// Uses reflection to:
/// 1. Discover all ICatalogEntry properties on derived classes
/// 2. Create backing fields to cache instances
/// 3. Intercept property getters to return cached instances
/// </para>
/// <para>
/// <strong>Usage Pattern:</strong>
/// <code>
/// public class MyCatalog : DataCatalogBase
/// {
///     public MyCatalog(string basePath = "Data") : base()
///     {
///         BasePath = basePath;
///         InitializeCatalogProperties();
///     }
///
///     protected string BasePath { get; }
///
///     // Declare once - automatically cached!
///     public ICatalogEntry&lt;IEnumerable&lt;MyData&gt;&gt; MyData =>
///         GetOrCreateEntry(() => new CsvCatalogEntry&lt;MyData&gt;("my_data", $"{BasePath}/data.csv"));
/// }
/// </code>
/// </para>
/// <para>
/// <strong>Key Benefits:</strong>
/// - Declare catalog entries ONCE (no redundant constructor code)
/// - Automatic instance caching (object identity preserved)
/// - Type-safe (compile-time checks)
/// - Zero runtime overhead after first access (cached delegates)
/// </para>
/// </remarks>
public abstract class DataCatalogBase
{
  /// <summary>
  /// Cache of property values to ensure object identity for DAG resolution.
  /// Key: Property name, Value: Cached ICatalogEntry instance
  /// </summary>
  private readonly ConcurrentDictionary<string, ICatalogEntry> _propertyCache = new();

  /// <summary>
  /// Optional service provider for dependency injection into catalog entries.
  /// </summary>
  /// <remarks>
  /// Set by FlowthruApplication before pipeline execution to enable catalog
  /// entries to resolve services (e.g., database connections, HTTP clients).
  /// </remarks>
  public IServiceProvider? Services { get; set; }

  /// <summary>
  /// Gets or creates a unified catalog entry, caching it for subsequent accesses.
  /// </summary>
  /// <typeparam name="T">
  /// The data type stored in this catalog entry.
  /// For singletons: Use T directly (e.g., LinearRegressionModel)
  /// For collections: Use Seq&lt;T&gt; or IEnumerable&lt;T&gt; (e.g., Seq&lt;FeatureRow&gt;)
  /// </typeparam>
  /// <param name="factory">Factory function to create the entry on first access</param>
  /// <param name="propertyName">Auto-populated by compiler with calling property name</param>
  /// <returns>Cached catalog entry instance</returns>
  /// <remarks>
  /// <para>
  /// <strong>Unified API (v0.5.0):</strong> This single method replaces GetOrCreateObject
  /// and GetOrCreateDataset. Cardinality is determined by the type parameter T.
  /// </para>
  /// <para>
  /// <strong>Usage Examples:</strong>
  /// <code>
  /// // Singleton object
  /// public ICatalogEntry&lt;LinearRegressionModel&gt; Model =>
  ///     GetOrCreateEntry(() => new MemoryCatalogEntry&lt;LinearRegressionModel&gt;("model"));
  ///
  /// // Collection
  /// public ICatalogEntry&lt;Seq&lt;FeatureRow&gt;&gt; Features =>
  ///     GetOrCreateEntry(() => new CsvCatalogEntry&lt;Seq&lt;FeatureRow&gt;&gt;("features", "data.csv"));
  /// </code>
  /// </para>
  /// </remarks>
  protected ICatalogEntry<T> GetOrCreateEntry<T>(
    Func<ICatalogEntry<T>> factory,
    [System.Runtime.CompilerServices.CallerMemberName] string propertyName = ""
  )
  {
    var entry = _propertyCache.GetOrAdd(propertyName, _ => factory());
    return (ICatalogEntry<T>)entry;
  }

  /// <summary>
  /// Gets or creates a unified catalog entry with service provider access.
  /// </summary>
  /// <typeparam name="T">The data type (singleton or collection)</typeparam>
  /// <param name="factory">Factory function that receives service provider</param>
  /// <param name="propertyName">Auto-populated by compiler with calling property name</param>
  /// <returns>Cached catalog entry instance</returns>
  protected ICatalogEntry<T> GetOrCreateEntry<T>(
    Func<IServiceProvider?, ICatalogEntry<T>> factory,
    [System.Runtime.CompilerServices.CallerMemberName] string propertyName = ""
  )
  {
    var entry = _propertyCache.GetOrAdd(propertyName, _ => factory(Services));
    return (ICatalogEntry<T>)entry;
  }

  /// <summary>
  /// Gets or creates a read-only catalog entry, caching it for subsequent accesses.
  /// </summary>
  /// <typeparam name="T">The data type (singleton or collection)</typeparam>
  /// <param name="factory">Factory function to create the read-only entry</param>
  /// <param name="propertyName">Auto-populated by compiler with calling property name</param>
  /// <returns>Cached read-only catalog entry instance</returns>
  protected IReadableCatalogEntry<T> GetOrCreateReadOnlyEntry<T>(
    Func<IReadableCatalogEntry<T>> factory,
    [System.Runtime.CompilerServices.CallerMemberName] string propertyName = ""
  )
  {
    var entry = _propertyCache.GetOrAdd(propertyName, _ => factory());
    return (IReadableCatalogEntry<T>)entry;
  }

  /// <summary>
  /// Gets or creates a read-only catalog entry with service provider access.
  /// </summary>
  /// <typeparam name="T">The data type (singleton or collection)</typeparam>
  /// <param name="factory">Factory function that receives service provider</param>
  /// <param name="propertyName">Auto-populated by compiler with calling property name</param>
  /// <returns>Cached read-only catalog entry instance</returns>
  protected IReadableCatalogEntry<T> GetOrCreateReadOnlyEntry<T>(
    Func<IServiceProvider?, IReadableCatalogEntry<T>> factory,
    [System.Runtime.CompilerServices.CallerMemberName] string propertyName = ""
  )
  {
    var entry = _propertyCache.GetOrAdd(propertyName, _ => factory(Services));
    return (IReadableCatalogEntry<T>)entry;
  }

  /// <summary>
  /// Initializes all catalog entry properties by invoking their getters once.
  /// </summary>
  /// <remarks>
  /// <para>
  /// <strong>Purpose:</strong> Eager initialization ensures all entries are cached
  /// before pipeline construction begins, preventing any potential race conditions
  /// or unexpected lazy initialization behavior.
  /// </para>
  /// <para>
  /// <strong>When to Call:</strong> At the end of the derived catalog's constructor,
  /// after all configuration properties (like BasePath) are set.
  /// </para>
  /// <para>
  /// <strong>How It Works:</strong>
  /// Uses reflection to find all public instance properties that return ICatalogEntry,
  /// then invokes each getter once to populate the cache.
  /// </para>
  /// </remarks>
  protected void InitializeCatalogProperties()
  {
    var catalogProperties = GetType()
      .GetProperties(BindingFlags.Public | BindingFlags.Instance)
      .Where(p => typeof(ICatalogEntry).IsAssignableFrom(p.PropertyType));

    foreach (var property in catalogProperties)
    {
      // Invoke getter to populate cache
      _ = property.GetValue(this);
    }
  }

  /// <summary>
  /// Gets all cached catalog entries.
  /// </summary>
  /// <returns>Enumerable of all initialized catalog entries</returns>
  /// <remarks>
  /// Useful for diagnostic purposes or when you need to iterate over all entries
  /// (e.g., for validation, cleanup, or reporting).
  /// </remarks>
  protected IEnumerable<ICatalogEntry> GetAllEntries()
  {
    return _propertyCache.Values;
  }

  /// <summary>
  /// Clears the property cache. Use with caution!
  /// </summary>
  /// <remarks>
  /// <para>
  /// <strong>Warning:</strong> Clearing the cache after pipeline construction will break
  /// DAG dependencies since new instances will be created on next access.
  /// </para>
  /// <para>
  /// <strong>Use Case:</strong> Primarily for testing scenarios where you need to reset
  /// catalog state between test runs.
  /// </para>
  /// </remarks>
  protected void ClearCache()
  {
    _propertyCache.Clear();
  }
}
