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
}
