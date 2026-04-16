using Flowthru.Core.Data.Validation;
using Flowthru.Core.Effects;
using Flowthru.Core.Graph;

namespace Flowthru.Core.Data;

/// <summary>
/// Non-generic base interface for catalog items — a specialization of <see cref="INode"/>
/// for data I/O nodes backed by storage adapters.
/// </summary>
/// <remarks>
/// <para>
/// Extends <see cref="INode"/> with data-specific operations: existence checks,
/// row counting, and two-level inspection (shallow/deep). The engine-level
/// <see cref="INode.ProduceUntyped"/> and <see cref="INode.ConsumeUntyped"/> are
/// bridged to <see cref="LoadUntyped"/> and <see cref="SaveUntyped"/> via default
/// interface implementations.
/// </para>
/// </remarks>
public interface IItem : INode
{
  /// <summary>
  /// Gets the preferred inspection level for this catalog item.
  /// </summary>
  InspectionLevel? PreferredInspectionLevel { get; }

  /// <summary>
  /// The label of the <see cref="Flowthru.Core.Data.CatalogAbstract"/>-derived class that created
  /// this item. Set automatically by <c>CreateItem</c>; null for items created outside
  /// a catalog or by custom <see cref="IItem"/> implementations.
  /// </summary>
  /// <remarks>
  /// Used by the metadata layer to produce fully-qualified item identifiers in the form
  /// <c>CatalogLabel.ItemLabel</c>. First-write-wins: cross-catalog shared items retain
  /// the label of the catalog that originally created them.
  /// </remarks>
  string? OwningCatalogLabel => null;

  /// <summary>
  /// Loads data from the catalog item as an untyped object.
  /// Returns an effect that can fail.
  /// The returned type matches the DataType property.
  /// </summary>
  FlowIO<object> LoadUntyped();

  /// <summary>
  /// Saves untyped data to the catalog item.
  /// Returns an effect that can fail.
  /// The data type must be compatible with the DataType property.
  /// </summary>
  FlowIO<FlowUnit> SaveUntyped(object data);

  /// <summary>
  /// Checks if data exists at this catalog item location.
  /// Returns an effect that can fail.
  /// </summary>
  FlowIO<bool> Exists();

  /// <summary>
  /// Gets the count of items in this catalog item.
  /// For collections (IEnumerable&lt;T&gt;), returns the enumerable count.
  /// For singletons, returns 1 if exists, 0 otherwise.
  /// </summary>
  FlowIO<int> GetCountAsync();

  /// <summary>
  /// Performs shallow validation of this catalog item.
  /// </summary>
  /// <param name="sampleSize">Number of rows/records to sample for validation</param>
  /// <returns>Effect producing validation result</returns>
  FlowIO<ValidationResult> InspectShallow(int sampleSize = 100);

  /// <summary>
  /// Performs deep validation of this catalog item.
  /// </summary>
  /// <returns>Effect producing validation result</returns>
  FlowIO<ValidationResult> InspectDeep();

  // ── INode default implementations ──

  /// <inheritdoc/>
  NodeTraits INode.Traits => new NodeTraits { CanInspect = true };

  /// <inheritdoc/>
  FlowIO<object> INode.ProduceUntyped() => LoadUntyped();

  /// <inheritdoc/>
  FlowIO<FlowUnit> INode.ConsumeUntyped(object data) => SaveUntyped(data);

  /// <summary>
  /// Validates this item by delegating to the appropriate inspection level.
  /// If <see cref="PreferredInspectionLevel"/> is set, uses that; otherwise defaults to shallow.
  /// </summary>
  FlowIO<ValidationResult> INode.Validate()
  {
    var level = PreferredInspectionLevel ?? InspectionLevel.Shallow;
    return level switch
    {
      InspectionLevel.None => FlowIO.Pure(ValidationResult.Success()),
      InspectionLevel.Deep => InspectDeep(),
      _ => InspectShallow(),
    };
  }
}

/// <summary>
/// Typed catalog item — a specialization of <see cref="INode{T}"/> for data I/O.</summary>
/// <typeparam name="T">
/// The data type stored in this catalog item.
/// Cardinality is determined by T itself:
/// - For singletons: Use T directly (e.g., LinearRegressionModel, ModelMetrics)
/// - For collections: Use IEnumerable&lt;T&gt; (e.g., IEnumerable&lt;FeatureRow&gt;)
/// </typeparam>
/// <remarks>
/// <para>
/// <see cref="Load"/> and <see cref="Save"/> are the data-specific aliases for
/// <see cref="INode{T}.Produce"/> and <see cref="INode{T}.Consume"/>.
/// Default interface implementations bridge the two: the engine calls
/// <c>Produce()</c>/<c>Consume()</c>, which delegate to <c>Load()</c>/<c>Save()</c>.
/// </para>
/// </remarks>
public interface IItem<T> : IItem, INode<T>
{
  /// <summary>
  /// Load data as an effect (can fail, is async, can be cancelled).
  /// Returns T directly, which may itself be an IEnumerable or Seq.
  /// </summary>
  FlowIO<T> Load();

  /// <summary>
  /// Save data as an effect.
  /// Accepts T directly, which may itself be an IEnumerable or Seq.
  /// </summary>
  FlowIO<FlowUnit> Save(T data);

  // ── INode<T> default implementations ──

  /// <inheritdoc/>
  FlowIO<T> INode<T>.Produce() => Load();

  /// <inheritdoc/>
  FlowIO<FlowUnit> INode<T>.Consume(T data) => Save(data);
}
