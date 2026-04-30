using System.Collections.Concurrent;
using System.Linq.Expressions;
using System.Reflection;

namespace Flowthru.Core.Data;

/// <summary>
/// Base class for strongly-typed catalog implementations with automatic property caching.
/// </summary>
/// <remarks>
/// <para>
/// <strong>Problem Solved:</strong>
/// Expression-bodied properties (<c>Property => new Item()</c>) create new instances on each access,
/// breaking DAG dependency resolution which relies on object identity.
/// </para>
/// <para>
/// <strong>Solution:</strong>
/// Uses reflection to:
/// 1. Discover all IItem properties on derived classes
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
///     public IItem&lt;IEnumerable&lt;MyData&gt;&gt; MyData =>
///         CreateItem(() => new CsvCatalogItem&lt;MyData&gt;("my_data", $"{BasePath}/data.csv"));
/// }
/// </code>
/// </para>
/// <para>
/// <strong>Key Benefits:</strong>
/// - Declare catalog items ONCE (no redundant constructor code)
/// - Automatic instance caching (object identity preserved)
/// - Type-safe (compile-time checks)
/// - Zero runtime overhead after first access (cached delegates)
/// </para>
/// </remarks>
public abstract class CatalogAbstract
{
  /// <summary>
  /// Cache of property values to ensure object identity for DAG resolution.
  /// Key: Property name, Value: Cached IItem instance
  /// </summary>
  private readonly ConcurrentDictionary<string, IItem> _propertyCache = new();

  /// <summary>
  /// The display label used to identify this catalog instance in Flow metadata.
  /// Defaults to the concrete class name when not specified.
  /// </summary>
  /// <remarks>
  /// Pass an explicit label when constructing multiple instances of the same catalog type
  /// in a single Flow (e.g., per-partition or per-shard catalogs) so their items
  /// receive distinct qualified identifiers in the DAG: <c>CatalogLabel.ItemLabel</c>.
  /// </remarks>
  public string CatalogLabel { get; }

  /// <param name="catalogLabel">
  /// Optional display label for this catalog instance. When omitted, defaults to the
  /// concrete class name via <c>GetType().Name</c>.
  /// </param>
  protected CatalogAbstract(string? catalogLabel = null)
  {
    CatalogLabel = catalogLabel ?? GetType().Name;
  }

  /// <summary>
  /// Optional service provider for dependency injection into catalog items.
  /// </summary>
  /// <remarks>
  /// Set by the service layer before Flow execution to enable catalog
  /// items to resolve services (e.g., database connections, HTTP clients).
  /// </remarks>
  public IServiceProvider? Services { get; set; }

  /// <summary>
  /// Gets or creates a unified catalog item, caching it for subsequent accesses.
  /// </summary>
  /// <typeparam name="T">
  /// The data type stored in this catalog item.
  /// For singletons: Use T directly (e.g., LinearRegressionModel)
  /// For collections: Use IEnumerable&lt;T&gt; (e.g., IEnumerable&lt;FeatureRow&gt;)
  /// </typeparam>
  /// <param name="factory">Factory function to create the item on first access</param>
  /// <param name="propertyName">Auto-populated by compiler with calling property name</param>
  /// <returns>Cached catalog item instance</returns>
  /// <remarks>
  /// <para>
  /// <strong>Unified API (v0.5.0):</strong> This single method replaces CreateObject
  /// and CreateDataset. Cardinality is determined by the type parameter T.
  /// </para>
  /// <para>
  /// <strong>Usage Examples:</strong>
  /// <code>
  /// // Singleton object
  /// public IItem&lt;LinearRegressionModel&gt; Model =>
  ///     CreateItem(() =&gt; ItemFactory.Single.Memory&lt;LinearRegressionModel&gt;("model"));
  ///
  /// // Collection
  /// public IItem&lt;IEnumerable&lt;FeatureRow&gt;&gt; Features =&gt;
  ///     CreateItem(() =&gt; ItemFactory.Enumerable.Csv&lt;FeatureRow&gt;("features", "data.csv"));
  /// </code>
  /// </para>
  /// </remarks>
  protected IItem<T> CreateItem<T>(
    Func<IItem<T>> factory,
    [System.Runtime.CompilerServices.CallerMemberName] string propertyName = ""
  )
  {
    var item = _propertyCache.GetOrAdd(propertyName, _ => factory());
    if (item is Item<T> concrete)
    {
      concrete.SetOwningCatalog(CatalogLabel);
    }

    return (IItem<T>)item;
  }

  /// <summary>
  /// Initializes all catalog item properties by invoking their getters once.
  /// </summary>
  /// <remarks>
  /// <para>
  /// <strong>Purpose:</strong> Eager initialization ensures all items are cached
  /// before Flow construction begins, preventing any potential race conditions
  /// or unexpected lazy initialization behavior.
  /// </para>
  /// <para>
  /// <strong>When to Call:</strong> At the end of the derived catalog's constructor,
  /// after all configuration properties (like BasePath) are set.
  /// </para>
  /// <para>
  /// <strong>How It Works:</strong>
  /// Uses reflection to find all public instance properties that return IItem,
  /// then invokes each getter once to populate the cache.
  /// </para>
  /// </remarks>
  protected void InitializeCatalogProperties()
  {
    var catalogProperties = GetType()
      .GetProperties(BindingFlags.Public | BindingFlags.Instance)
      .Where(p => typeof(IItem).IsAssignableFrom(p.PropertyType));

    foreach (var property in catalogProperties)
    {
      // Invoke getter to populate cache
      _ = property.GetValue(this);
    }
  }

}
