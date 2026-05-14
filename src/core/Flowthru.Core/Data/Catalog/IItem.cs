using Flowthru.Data.Storage;

namespace Flowthru.Data.Catalog;

/// <summary>
/// Catalog item — a typed place where data lives. The "place" archetype
/// of <see cref="INode"/>: refines the umbrella with data operations
/// (<see cref="Load"/>, <see cref="Save"/>, existence, three inspection
/// levels) plus an untyped dispatch surface the engine uses to read and
/// write items without naming their element type.
/// </summary>
/// <typeparam name="T">
/// The data type stored at this item. Cardinality is encoded in <c>T</c>
/// itself: <c>IEnumerable&lt;TRow&gt;</c> for collections, <c>TRow</c>
/// directly for singletons.
/// </typeparam>
public interface IItem<T> : IItem
{
  /// <summary>Loads data from the underlying storage.</summary>
  FlowIO<T> Load();

  /// <summary>Saves data into the underlying storage.</summary>
  FlowIO<FlowUnit> Save(T data);

  // ── IItem (untyped) default implementations ──

  /// <inheritdoc/>
  Type IItem.DataType => typeof(T);

  /// <inheritdoc/>
  FlowIO<object> IItem.LoadUntyped() => Load().Map(value => (object)value!);

  /// <inheritdoc/>
  FlowIO<FlowUnit> IItem.SaveUntyped(object data) => Save((T)data);
}

/// <summary>
/// Marker interface for catalog items whose <see cref="IItem{T}.Save"/>
/// always fails — i.e. items that participate in flows only as inputs.
/// Canonical implementer is
/// <see cref="Flowthru.Data.Configuration.ConfigurationItem{T}"/>; any
/// catalog item whose backing source is logically read-only (a remote
/// service status feed, a frozen reference table, etc.) may opt in by
/// implementing this interface.
/// </summary>
/// <remarks>
/// <para>
/// The marker exists so the framework can reject misuse at the earliest
/// possible error phase. Source-gen diagnostic <c>FT1102</c> fires at
/// build time when an item declaring this interface is passed to the
/// <c>outputs:</c> position of a <c>FlowBuilder.AddStep</c> call,
/// turning what would otherwise be a runtime failure into a red
/// squiggle. See <c>docs/scratch/smart-caching-and-slicing/phase-5-config-as-catalog.md</c>.
/// </para>
/// <para>
/// Implementers should also override <see cref="IItem{T}.Save"/> to
/// return a deterministic failure (typically a
/// <see cref="RuntimeError.External"/> wrapping an
/// <see cref="InvalidOperationException"/>) so direct invocation
/// outside the framework's wiring path still fails fast.
/// </para>
/// </remarks>
/// <typeparam name="T">The data type stored at this item.</typeparam>
public interface IReadOnlyItem<T> : IItem<T>
{
}

/// <summary>
/// Untyped item facet. The engine names this when reading inputs and
/// writing outputs in a per-step loop without knowing each item's
/// element type.
/// </summary>
/// <remarks>
/// End users never name <see cref="IItem"/> directly — they hold
/// <see cref="IItem{T}"/> instances. This umbrella exists so the engine
/// can iterate over a step's <c>Inputs</c>/<c>Outputs</c> uniformly.
/// </remarks>
public interface IItem : INode
{
  /// <summary>The runtime type of the value this item holds.</summary>
  Type DataType { get; }

  /// <summary>Untyped load — boxes the typed value as <see cref="object"/>.</summary>
  FlowIO<object> LoadUntyped();

  /// <summary>Untyped save — unboxes the supplied <see cref="object"/> to the item's element type.</summary>
  FlowIO<FlowUnit> SaveUntyped(object data);

  /// <summary>True if data currently exists at this item.</summary>
  FlowIO<bool> Exists();

  /// <summary>Shallow inspection — existence + sample-size check.</summary>
  FlowIO<ValidationResult> InspectShallow(int sampleSize = 100);

  /// <summary>Deep inspection — full-dataset validation.</summary>
  FlowIO<ValidationResult> InspectDeep();

  /// <summary>Reachability check on a write target.</summary>
  FlowIO<ValidationResult> InspectTarget();

  /// <summary>
  /// Optional per-item ceiling on pre-flight inspection depth. When
  /// set, <see cref="Validation.PreFlight.PreFlightPipeline"/> uses
  /// the minimum of (caller-requested level, this cap) for this item.
  /// Default <c>null</c> means use whatever level the caller requested.
  /// Catalog authors set the cap via
  /// <c>IItem&lt;T&gt;.WithMaxInspectionLevel(...)</c> to keep
  /// expensive inspections off this item without changing the global
  /// <c>ExecutionOptions.ValidationDepth</c>.
  /// </summary>
  InspectionLevel? MaxInspectionLevel => null;

  /// <summary>
  /// True when the underlying storage adapter implements
  /// <see cref="IHasEfficientCount"/> — i.e. a row count is available
  /// without materialising the dataset (a SQL <c>COUNT(*)</c>, a
  /// directory's file count, etc.). Diagnostic providers gate count
  /// calls on this flag so they don't accidentally trigger expensive
  /// load-and-count fallbacks.
  /// </summary>
  bool HasEfficientCount => false;

  /// <summary>
  /// Returns the row count from the underlying storage adapter when
  /// it implements <see cref="IHasEfficientCount"/>. Throws via the
  /// FlowIO failure channel when called on an item whose adapter
  /// doesn't claim the capability — callers should check
  /// <see cref="HasEfficientCount"/> first.
  /// </summary>
  FlowIO<int> GetCountAsync() =>
    FlowIO.Fail<int>(new Validation.Runtime.RuntimeError.External(
      "IItem.GetCountAsync",
      new InvalidOperationException(
        "Item's storage adapter does not implement IHasEfficientCount. "
        + "Check IItem.HasEfficientCount before calling GetCountAsync.")));
}
