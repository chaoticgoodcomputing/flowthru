using System.Collections.Concurrent;
using System.Runtime.CompilerServices;

namespace Flowthru.Data.Catalog;

/// <summary>
/// Base class for typed catalog declarations. Properties declared via
/// <see cref="CreateItem{T}(Func{IItem{T}}, string)"/> are cached on
/// first access so the DAG sees a single <see cref="IItem{T}"/> instance
/// per property — object identity is what makes catalogs usable as DAG
/// vertices.
/// </summary>
/// <remarks>
/// <para>
/// Per the §3.3 catalog API simplification, the typical authoring shape
/// is the attribute-driven partial property:
/// <code>
/// [JsonItem("Datasets/iris.json")]
/// public partial IItem&lt;IEnumerable&lt;IrisRawSchema&gt;&gt; IrisRaw { get; }
/// </code>
/// where the source generator emits the property body. Manual
/// <c>CreateItem(() => …)</c> remains as the fallback for cases the
/// attributes don't cover.
/// </para>
/// </remarks>
public abstract class CatalogAbstract
{
  private readonly ConcurrentDictionary<string, object> _propertyCache = new();

  /// <summary>The display label for this catalog instance — defaults to the type name.</summary>
  public string CatalogLabel { get; }

  protected CatalogAbstract(string? catalogLabel = null)
  {
    CatalogLabel = catalogLabel ?? GetType().Name;
  }

  /// <summary>
  /// Gets or creates a catalog item, caching it on first access. Subsequent
  /// accesses return the same instance, preserving object identity for the
  /// DAG dependency analyzer.
  /// </summary>
  /// <typeparam name="T">The data type stored at this item.</typeparam>
  /// <param name="factory">Factory function called once on first access.</param>
  /// <param name="propertyName">Caller's property name (auto-populated).</param>
  protected IItem<T> CreateItem<T>(
    Func<IItem<T>> factory,
    [CallerMemberName] string propertyName = ""
  )
  {
    if (factory is null)
    {
      throw new ArgumentNullException(nameof(factory));
    }
    return (IItem<T>)_propertyCache.GetOrAdd(propertyName, _ => factory());
  }

  /// <summary>
  /// Optional managed resource owned by this catalog. The runtime
  /// acquires the resource before flow execution and releases it
  /// LIFO afterwards (across every registered catalog). Default
  /// returns <c>null</c> — most catalogs hold no managed resources
  /// (in-memory items, JSON files, etc., do not need acquire/release).
  /// </summary>
  /// <remarks>
  /// <para>
  /// Override when a catalog wraps a connection, transaction, or
  /// other handle that needs lifecycle management — e.g., a Postgres
  /// catalog opens a pooled connection in acquire, returns it to the
  /// pool in release. <see cref="FlowResource{TScope}"/> bundles the
  /// pair so a catalog cannot publish acquire without release.
  /// </para>
  /// <para>
  /// Resources acquire <em>before</em> pre-flight runs so probes can
  /// exercise the live handle; they release <em>after</em> post-run
  /// metadata. On failure the release closure receives the body's
  /// <see cref="RuntimeError"/> so it can apply policies like
  /// "preserve on failure."
  /// </para>
  /// </remarks>
  public virtual IFlowResource? Resource => null;
}
