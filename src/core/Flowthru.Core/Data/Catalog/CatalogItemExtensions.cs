using Flowthru.Data.Storage;

namespace Flowthru.Data.Catalog;

/// <summary>
/// Pure-function combinators on <see cref="IItem{T}"/>. Each method
/// returns a new <see cref="IItem{T}"/> with the requested
/// transformation applied; the original item is unchanged. The
/// FP-aligned shape lets catalog authors compose item adjustments
/// inline at the catalog declaration site.
/// </summary>
public static class CatalogItemExtensions
{
  /// <summary>
  /// Narrow the trait surface of a catalog item — e.g. mark it
  /// read-only via <c>item.Constrain(t =&gt; t with { CanWrite = false })</c>.
  /// </summary>
  /// <remarks>
  /// <para>
  /// Constraints are a one-way ratchet: each trait can only narrow
  /// (true → false), never widen (false → true). A widening attempt
  /// throws at catalog wire-up time so misuses fail loud at the
  /// declaration site, not mid-flow.
  /// </para>
  /// <para>
  /// At runtime, operations the constraint forbids surface as
  /// <see cref="RuntimeError.ConstraintViolated"/> rather than
  /// reaching the underlying adapter — distinct from
  /// <see cref="RuntimeError.External"/> (real system error) and
  /// <see cref="RuntimeError.InvariantViolated"/> (Flowthru bug).
  /// </para>
  /// </remarks>
  public static IItem<T> Constrain<T>(
    this IItem<T> item,
    Func<StorageTraits, StorageTraits> narrow
  )
  {
    if (item is null) throw new ArgumentNullException(nameof(item));
    if (narrow is null) throw new ArgumentNullException(nameof(narrow));

    var inner = ResolveStorageAdapter(item)
      ?? throw new ArgumentException(
        $"Constrain requires the item to expose its underlying IStorageAdapter<{typeof(T).Name}>. "
        + $"Item '{item.Label}' (type {item.GetType().Name}) does not expose one — wrap it in "
        + "an Item<T> via the standard ItemFactory smart constructors before constraining.",
        nameof(item)
      );

    var narrowed = narrow(inner.Traits);
    return new Item<T>(item.Label, new ConstrainedStorageAdapter<T>(inner, narrowed, item.Label));
  }

  /// <summary>
  /// Cap pre-flight inspection at <paramref name="max"/> for this item.
  /// The pre-flight pipeline runs <c>min(globalLevel, max)</c> for
  /// this item; other items run at the global level. Use this to opt
  /// expensive items down (e.g. a 50M-row DB table) without changing
  /// the global <c>ExecutionOptions.ValidationDepth</c>.
  /// </summary>
  /// <remarks>
  /// Caps are pure-function — the original item is unchanged. Caps
  /// can only narrow: a cap of <c>Deep</c> on an item already capped
  /// at <c>Shallow</c> stays <c>Shallow</c> (the tighter of the two
  /// wins). Caps and constraints compose freely.
  /// </remarks>
  public static IItem<T> WithMaxInspectionLevel<T>(this IItem<T> item, InspectionLevel max)
  {
    if (item is null) throw new ArgumentNullException(nameof(item));

    // Tighter-of-two semantics: chained caps narrow rather than
    // overwrite. A consumer can call .WithMaxInspectionLevel(Deep)
    // on a Shallow-capped item and the cap stays Shallow.
    var existing = item.MaxInspectionLevel;
    var effective = existing is null
      ? max
      : (InspectionLevel)Math.Min((int)existing, (int)max);

    return new InspectionCappedItem<T>(item, effective);
  }

  /// <summary>
  /// Derive a read-only <em>streaming</em> view of a collection item:
  /// <c>IItem&lt;IEnumerable&lt;TRow&gt;&gt;</c> →
  /// <c>IReadOnlyItem&lt;FlowSource&lt;TRow&gt;&gt;</c>. The view's
  /// <c>Load()</c> yields a deferred <see cref="Prelude.FlowSource{TRow}"/>
  /// whose peak read memory is O(batch), not O(file).
  /// </summary>
  /// <remarks>
  /// Read-only because composed streaming <em>writes</em> are out of scope
  /// (ADR-0023). Gated to streaming-capable composed formats: calling this on a
  /// direct adapter (EFCore, Sheets, GQL) or a non-streaming format throws at
  /// wire-up — a design-time error, never a silent O(file) materialise. A
  /// <c>.Constrain()</c> wrapper is unwrapped to reach the composed format.
  /// </remarks>
  public static IReadOnlyItem<FlowSource<TRow>> AsStream<TRow>(this IItem<IEnumerable<TRow>> item)
    where TRow : notnull
  {
    if (item is null) throw new ArgumentNullException(nameof(item));

    var adapter = ResolveStorageAdapter(item);
    if (adapter is ConstrainedStorageAdapter<IEnumerable<TRow>> constrained)
    {
      adapter = constrained.Inner;
    }

    if (adapter is not ISupportsStreamingView<TRow> streamable || !streamable.SupportsStreaming)
    {
      throw new ArgumentException(
        $"AsStream() requires item '{item.Label}' to be backed by a streaming-capable composed "
        + $"format (an IFormatStreamReader<{typeof(TRow).Name}>). Its adapter "
        + $"({adapter?.GetType().Name ?? "none"}) does not support streaming reads — direct "
        + "adapters (EFCore, Sheets, GQL) and non-streaming formats cannot stream. Use the "
        + "eager item, or a streaming-capable format.",
        nameof(item)
      );
    }

    return new StreamingItem<TRow>(item.Label, streamable, item);
  }

  /// <summary>
  /// Best-effort extraction of the underlying <see cref="IStorageAdapter{T}"/>
  /// from a catalog item. Recognises <see cref="Item{T}"/> directly and
  /// <see cref="ConstrainedStorageAdapter{T}"/>-wrapped items; falls back to
  /// <c>null</c> for custom <see cref="IItem{T}"/> implementations that
  /// don't expose an adapter.
  /// </summary>
  private static IStorageAdapter<T>? ResolveStorageAdapter<T>(IItem<T> item) =>
    item is Item<T> standard ? standard.Storage : null;
}

/// <summary>
/// Item wrapper that carries a per-item inspection-depth cap. Delegates
/// every operation to the wrapped item; only the
/// <see cref="IItem.MaxInspectionLevel"/> property is overridden so
/// the pre-flight pipeline can consult the cap.
/// </summary>
internal sealed class InspectionCappedItem<T> : IItem<T>
{
  private readonly IItem<T> _inner;

  internal InspectionCappedItem(IItem<T> inner, InspectionLevel cap)
  {
    _inner = inner;
    MaxInspectionLevel = cap;
  }

  public string Label => _inner.Label;
  public NodeTraits Traits => _inner.Traits;
  public Type DataType => _inner.DataType;
  public InspectionLevel? MaxInspectionLevel { get; }

  public FlowIO<T> Load() => _inner.Load();
  public FlowIO<FlowUnit> Save(T data) => _inner.Save(data);
  public FlowIO<bool> Exists() => _inner.Exists();
  public FlowIO<ValidationResult> InspectShallow(int sampleSize = 100) =>
    _inner.InspectShallow(sampleSize);
  public FlowIO<ValidationResult> InspectDeep() => _inner.InspectDeep();
  public FlowIO<ValidationResult> InspectTarget() => _inner.InspectTarget();

  public FlowIO<object> LoadUntyped() => Load().Map(value => (object)value!);
  public FlowIO<FlowUnit> SaveUntyped(object data) => Save((T)data);
  public FlowIO<ValidationResult> Validate() => InspectShallow();
}
