using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using Flowthru.Data.Storage;

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
/// <para>
/// <strong>Ambient resolver propagation.</strong> When a catalog is
/// constructed with an <see cref="IStorageMediumResolver"/> (the DI
/// container supplies one via the framework's hosting plumbing),
/// <see cref="CreateItem{T}"/> pushes it onto
/// <see cref="StorageMediumResolver.Current"/> for the duration of the
/// factory call. Format builders (<c>JsonSingletonBuilder</c>,
/// <c>JsonArrayBuilder</c>, <c>CsvBuilder</c>, etc.) consult that slot
/// when no explicit <c>.WithResolver(...)</c> is supplied — the net
/// effect is that <c>Item.Of&lt;T&gt;().Json().AtPath("https://…")</c>
/// works without ceremony as soon as <c>UseHttp()</c> is registered on
/// the host.
/// </para>
/// </remarks>
public abstract class CatalogAbstract
{
  private readonly ConcurrentDictionary<string, object> _propertyCache = new();
  private IStorageMediumResolver? _resolver;

  /// <summary>The display label for this catalog instance — defaults to the type name.</summary>
  public string CatalogLabel { get; }

  protected CatalogAbstract(string? catalogLabel = null)
    : this(catalogLabel, resolver: null) { }

  /// <summary>
  /// Construct a catalog with an explicit
  /// <see cref="IStorageMediumResolver"/>. Item factories invoked
  /// through <see cref="CreateItem{T}"/> observe this resolver on the
  /// ambient <see cref="StorageMediumResolver.Current"/> slot. When
  /// <paramref name="resolver"/> is <c>null</c>, factories fall through
  /// to the existing per-builder defaults
  /// (<see cref="StorageMediumResolver.Filesystem"/>).
  /// </summary>
  protected CatalogAbstract(string? catalogLabel, IStorageMediumResolver? resolver)
  {
    CatalogLabel = catalogLabel ?? GetType().Name;
    _resolver = resolver;
  }

  /// <summary>
  /// Construct a catalog with an explicit
  /// <see cref="IStorageMediumResolver"/>; label defaults to the
  /// concrete type's name.
  /// </summary>
  protected CatalogAbstract(IStorageMediumResolver? resolver)
    : this(catalogLabel: null, resolver) { }

  /// <summary>
  /// The storage-medium resolver this catalog publishes via
  /// <see cref="StorageMediumResolver.Current"/> while a factory closure
  /// runs. May be <c>null</c> — in which case the ambient slot is
  /// pushed as <c>null</c> and builders fall back to
  /// <see cref="StorageMediumResolver.Filesystem"/>.
  /// </summary>
  public IStorageMediumResolver? Resolver => _resolver;

  /// <summary>
  /// Install an <see cref="IStorageMediumResolver"/> on this catalog
  /// after construction. Used by the hosting layer to attach the
  /// DI-resolved resolver to catalogs whose user-supplied constructor
  /// did not thread one through. Idempotent — re-setting the same
  /// resolver is a no-op; replacing a non-null resolver with a
  /// different one is intentionally allowed for advanced scenarios.
  /// </summary>
  internal void AttachResolver(IStorageMediumResolver? resolver)
  {
    _resolver = resolver;
  }

  /// <summary>
  /// Gets or creates a catalog item, caching it on first access. Subsequent
  /// accesses return the same instance, preserving object identity for the
  /// DAG dependency analyzer.
  /// </summary>
  /// <typeparam name="T">The data type stored at this item.</typeparam>
  /// <param name="factory">Factory function called once on first access.</param>
  /// <param name="propertyName">Caller's property name (auto-populated).</param>
  /// <remarks>
  /// The factory closure runs inside a
  /// <see cref="StorageMediumResolver.PushAmbient(IStorageMediumResolver?)"/>
  /// scope carrying this catalog's <see cref="Resolver"/>, so format
  /// builders inside the closure consume it without ceremony.
  /// </remarks>
  protected IItem<T> CreateItem<T>(
    Func<IItem<T>> factory,
    [CallerMemberName] string propertyName = ""
  )
  {
    if (factory is null)
    {
      throw new ArgumentNullException(nameof(factory));
    }
    return (IItem<T>)_propertyCache.GetOrAdd(propertyName, _ =>
    {
      using var _scope = StorageMediumResolver.PushAmbient(_resolver);
      return factory();
    });
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
