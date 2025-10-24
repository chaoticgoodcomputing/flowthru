namespace Flowthru.Data.Implementations;

/// <summary>
/// Null-object pattern catalog dataset that accepts any type but performs no I/O.
/// Used for wiring nodes with <see cref="Nodes.NoData"/> inputs or outputs in pipelines.
/// </summary>
/// <typeparam name="T">The nominal type parameter (typically <see cref="Nodes.NoData"/>)</typeparam>
/// <remarks>
/// <para>
/// <strong>Design Rationale:</strong> NullCatalogDataset enables type-safe pipeline wiring
/// for nodes that don't consume or produce meaningful data. It satisfies the ICatalogDataset
/// interface without performing actual I/O operations.
/// </para>
/// <para>
/// <strong>Use Cases:</strong>
/// - Wiring nodes with NoData input type (data generation nodes)
/// - Wiring nodes with NoData output type (side-effect/validation nodes)
/// - Replacing "throwaway" or "dummy" catalog entries with explicit intent
/// </para>
/// <para>
/// <strong>Behavior:</strong>
/// - Load() returns a singleton collection containing a single default(T) instance
/// - Save() accepts data but discards it immediately
/// - Exists() always returns true (conceptual data source)
/// - GetCountAsync() returns 1 (singleton behavior)
/// </para>
/// <para>
/// <strong>Thread Safety:</strong> This implementation is thread-safe (stateless, no shared mutable state).
/// </para>
/// <para>
/// <strong>Memory Footprint:</strong> Minimal - no data storage, only metadata (key).
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Simplified syntax using NoData static properties (recommended)
/// pipeline.AddNode&lt;ValidationNode&gt;(
///     input: catalog.InputData,
///     output: NoData.Discard  // Automatically creates unique NullCatalogDataset
/// );
/// 
/// pipeline.AddNode&lt;GenerateDataNode&gt;(
///     input: NoData.Input,  // Automatically creates unique NullCatalogDataset
///     output: catalog.GeneratedData
/// );
/// 
/// // Multiple no-output nodes in same pipeline (no conflicts)
/// pipeline.AddNode&lt;ValidationNode1&gt;(input: data1, output: NoData.Discard);
/// pipeline.AddNode&lt;ValidationNode2&gt;(input: data2, output: NoData.Discard);
/// 
/// // Explicit construction (rarely needed - use NoData.Input/Output/Discard instead)
/// pipeline.AddNode&lt;ValidationNode&gt;(
///     input: catalog.InputData,
///     output: new NullCatalogDataset&lt;NoData&gt;("custom_key")
/// );
/// </code>
/// </example>
public class NullCatalogDataset<T> : CatalogDatasetBase<T> {
  /// <summary>
  /// Creates a new null catalog dataset with the specified key.
  /// </summary>
  /// <param name="key">Unique identifier for this catalog entry (for pipeline DAG only)</param>
  public NullCatalogDataset(string key) : base(key) {
  }

  /// <inheritdoc/>
  /// <remarks>
  /// Returns a singleton collection containing default(T).
  /// For NoData, this will be NoData.Value (via default initialization).
  /// </remarks>
  public override Task<IEnumerable<T>> Load() {
    // Return singleton collection with default value
    // For NoData, default(NoData) is null, but we'll return a valid singleton
    var value = typeof(T) == typeof(Nodes.NoData)
        ? (T)(object)Nodes.NoData.Value
        : default(T)!;
    
    return Task.FromResult(Enumerable.Repeat(value, 1));
  }

  /// <inheritdoc/>
  /// <remarks>
  /// Accepts data but performs no I/O - data is immediately discarded.
  /// </remarks>
  public override Task Save(IEnumerable<T> data) {
    // Null-object pattern: accept data but do nothing with it
    return Task.CompletedTask;
  }

  /// <inheritdoc/>
  /// <remarks>
  /// Always returns true to indicate the "null data source" is conceptually available.
  /// </remarks>
  public override Task<bool> Exists() {
    return Task.FromResult(true);
  }

  /// <inheritdoc/>
  /// <remarks>
  /// Returns 1 to indicate singleton behavior (single NoData instance).
  /// </remarks>
  public override Task<int> GetCountAsync() {
    return Task.FromResult(1);
  }
}
