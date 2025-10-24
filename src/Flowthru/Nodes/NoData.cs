namespace Flowthru.Nodes;

/// <summary>
/// Marker type representing "no meaningful data" for nodes with side-effects or data generation.
/// Used as input/output type in NodeBase when a node doesn't consume or produce meaningful data.
/// </summary>
/// <remarks>
/// <para>
/// <strong>Design Rationale:</strong> NoData provides a type-safe way to represent nodes that:
/// - Generate data without inputs (e.g., synthetic data generation, seeding)
/// - Perform side-effects without outputs (e.g., validation, logging, diagnostics)
/// </para>
/// <para>
/// This pattern is inspired by functional programming's "Unit" type but uses more intuitive
/// naming for .NET developers unfamiliar with functional terminology.
/// </para>
/// <para>
/// <strong>Usage Examples:</strong>
/// </para>
/// <code>
/// // Node with no inputs (data generation)
/// public class GenerateDataNode : NodeBase&lt;NoData, OutputSchema&gt;
/// {
///     protected override Task&lt;IEnumerable&lt;OutputSchema&gt;&gt; Transform(IEnumerable&lt;NoData&gt; input)
///     {
///         // Generate data from scratch...
///         return Task.FromResult(generatedData);
///     }
/// }
/// 
/// // Node with no outputs (side-effects only)
/// public class ValidateNode : NodeBase&lt;InputSchema, NoData&gt;
/// {
///     protected override Task&lt;IEnumerable&lt;NoData&gt;&gt; Transform(IEnumerable&lt;InputSchema&gt; input)
///     {
///         // Perform validation, logging, etc...
///         return Task.FromResult(Enumerable.Repeat(NoData.Value, 1));
///     }
/// }
/// </code>
/// <para>
/// <strong>Pipeline Registration:</strong> Use NoData type directly - it automatically converts
/// to a unique NullCatalogDataset instance:
/// </para>
/// <code>
/// // Simple syntax with automatic unique key generation
/// pipeline.AddNode&lt;ValidationNode&gt;(
///     input: catalog.InputData,
///     output: NoData.Output  // or just: NoData.Discard
/// );
/// 
/// pipeline.AddNode&lt;GenerateDataNode&gt;(
///     input: NoData.Input,  // or just: NoData.None
///     output: catalog.GeneratedData
/// );
/// </code>
/// </remarks>
public sealed class NoData {
  private static int _uniqueIdCounter = 0;

  /// <summary>
  /// Singleton instance of NoData.
  /// Use this value when returning NoData from node transformations.
  /// </summary>
  public static readonly NoData Value = new();

  /// <summary>
  /// Creates a unique NullCatalogDataset for use as a node input (no-input nodes).
  /// Each call generates a new instance with a unique key to avoid DAG conflicts.
  /// </summary>
  /// <remarks>
  /// Alias for readability in pipeline declarations where nodes don't consume external inputs.
  /// </remarks>
  public static Data.Implementations.NullCatalogDataset<NoData> Input =>
    new($"_nodata_input_{Interlocked.Increment(ref _uniqueIdCounter)}");

  /// <summary>
  /// Creates a unique NullCatalogDataset for use as a node output (side-effect-only nodes).
  /// Each call generates a new instance with a unique key to avoid DAG conflicts.
  /// </summary>
  /// <remarks>
  /// Alias for readability in pipeline declarations where nodes produce no meaningful output.
  /// </remarks>
  public static Data.Implementations.NullCatalogDataset<NoData> Output =>
    new($"_nodata_output_{Interlocked.Increment(ref _uniqueIdCounter)}");

  /// <summary>
  /// Creates a unique NullCatalogDataset for use as a node output (side-effect-only nodes).
  /// Semantic alias for Output - use whichever reads better in context.
  /// </summary>
  public static Data.Implementations.NullCatalogDataset<NoData> Discard =>
    new($"_nodata_discard_{Interlocked.Increment(ref _uniqueIdCounter)}");

  /// <summary>
  /// Creates a unique NullCatalogDataset for use as a node input (no-input nodes).
  /// Semantic alias for Input - use whichever reads better in context.
  /// </summary>
  public static Data.Implementations.NullCatalogDataset<NoData> None =>
    new($"_nodata_none_{Interlocked.Increment(ref _uniqueIdCounter)}");

  /// <summary>
  /// Returns the standard NoData result for side-effect-only nodes.
  /// Use this at the end of Transform() methods that return NoData.
  /// </summary>
  /// <returns>Singleton collection containing NoData.Value</returns>
  /// <remarks>
  /// <para>
  /// This helper eliminates the verbose <c>Task.FromResult(Enumerable.Repeat(NoData.Value, 1))</c>
  /// boilerplate. Simply return <c>NoData.Result()</c>.
  /// </para>
  /// <example>
  /// <code>
  /// // Instead of:
  /// return Task.FromResult(Enumerable.Repeat(NoData.Value, 1));
  /// 
  /// // Use:
  /// return NoData.Result();
  /// </code>
  /// </example>
  /// </remarks>
  public static System.Threading.Tasks.Task<IEnumerable<NoData>> Result() {
    return System.Threading.Tasks.Task.FromResult(Enumerable.Repeat(Value, 1));
  }

  // Private constructor ensures only the singleton instance can exist
  private NoData() { }
}
