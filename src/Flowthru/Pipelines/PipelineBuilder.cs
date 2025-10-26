using Flowthru.Data;
using Flowthru.Nodes;

namespace Flowthru.Pipelines;

/// <summary>
/// Fluent builder for constructing type-safe data pipelines with tuple-based multi-input/output.
/// </summary>
/// <remarks>
/// <para>
/// <strong>Tuple-Based Design (v0.4.0):</strong>
/// Multi-input/output nodes use C# tuples instead of schema classes:
/// - Simple: catalog entry → catalog entry
/// - Multi-input: tuple of catalog entries → catalog entry
/// - Multi-output: catalog entry → tuple of catalog entries
/// - Multi-input-output: tuple → tuple
/// </para>
/// <para>
/// <strong>Usage Patterns:</strong>
/// </para>
/// <code>
/// var pipeline = PipelineBuilder.CreatePipeline(builder =>
/// {
///     // Simple node: single input → single output
///     builder.AddNode&lt;PreprocessNode&gt;(
///         input: catalog.RawData,
///         output: catalog.ProcessedData
///     );
///     
///     // Multi-input node: tuple → single output
///     builder.AddNode&lt;TrainModelNode&gt;(
///         input: (catalog.XTrain, catalog.YTrain),
///         output: catalog.Model
///     );
///     
///     // Multi-output node: single input → tuple
///     builder.AddNode&lt;SplitDataNode&gt;(
///         input: catalog.Data,
///         output: (catalog.XTrain, catalog.XTest, catalog.YTrain, catalog.YTest)
///     );
///     
///     // Multi-input-output: tuple → tuple
///     builder.AddNode&lt;ComplexNode&gt;(
///         input: (catalog.Input1, catalog.Input2),
///         output: (catalog.Output1, catalog.Output2)
///     );
/// });
/// 
/// pipeline.Build();
/// await pipeline.ExecuteAsync();
/// </code>
/// </remarks>
public class PipelineBuilder {
  private readonly Pipeline _pipeline = new();

  /// <summary>
  /// Creates and configures a new pipeline using the builder pattern.
  /// </summary>
  /// <param name="configure">Action to configure the pipeline by adding nodes</param>
  /// <returns>Configured but not yet built pipeline</returns>
  public static Pipeline CreatePipeline(Action<PipelineBuilder> configure) {
    var builder = new PipelineBuilder();
    configure(builder);
    return builder._pipeline;
  }

  /// <summary>
  /// Adds a simple node (single input → single output).
  /// </summary>
  public PipelineBuilder AddNode<TNode>(
    ICatalogEntry input,
    ICatalogEntry output,
    string? label = null,
    Action<TNode>? configure = null)
    where TNode : class, new() {

    var node = new TNode();
    configure?.Invoke(node);

    var pipelineNode = new PipelineNode(
      name: label ?? typeof(TNode).Name,
      nodeInstance: node,
      inputs: new List<ICatalogEntry> { input },
      outputs: new List<ICatalogEntry> { output }
    );

    _pipeline.AddNode(pipelineNode);
    return this;
  }

  /// <summary>
  /// Adds a node with tuple-based inputs/outputs (extracted via reflection).
  /// Handles all tuple combinations automatically.
  /// </summary>
  public PipelineBuilder AddNode<TNode>(
    object input,  // Can be ICatalogEntry or ITuple
    object output, // Can be ICatalogEntry or ITuple
    string? label = null,
    Action<TNode>? configure = null)
    where TNode : class, new() {

    var node = new TNode();
    configure?.Invoke(node);

    // Extract catalog entries from input (either single or tuple)
    var inputEntries = ExtractCatalogEntries(input);
    var outputEntries = ExtractCatalogEntries(output);

    var pipelineNode = new PipelineNode(
      name: label ?? typeof(TNode).Name,
      nodeInstance: node,
      inputs: inputEntries,
      outputs: outputEntries
    );

    _pipeline.AddNode(pipelineNode);
    return this;
  }

  private List<ICatalogEntry> ExtractCatalogEntries(object obj) {
    if (obj is ICatalogEntry singleEntry) {
      return new List<ICatalogEntry> { singleEntry };
    }

    if (obj is System.Runtime.CompilerServices.ITuple tuple) {
      var entries = new List<ICatalogEntry>();
      for (int i = 0; i < tuple.Length; i++) {
        if (tuple[i] is ICatalogEntry entry) {
          entries.Add(entry);
        } else {
          throw new InvalidOperationException(
            $"Tuple element {i} is not an ICatalogEntry. Got: {tuple[i]?.GetType().Name ?? "null"}");
        }
      }
      return entries;
    }

    throw new InvalidOperationException(
      $"Input/output must be ICatalogEntry or tuple of ICatalogEntry. Got: {obj.GetType().Name}");
  }
}

