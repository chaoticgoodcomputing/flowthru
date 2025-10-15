using Flowthru.Data;
using Flowthru.Nodes;
using Flowthru.Pipelines.Mapping;

namespace Flowthru.Pipelines;

/// <summary>
/// Fluent builder for constructing type-safe data pipelines.
/// </summary>
/// <remarks>
/// <para>
/// PipelineBuilder provides a fluent API for adding nodes to a pipeline with compile-time
/// type safety. All catalog references are validated at compile-time, ensuring that node
/// input/output types match their catalog entries.
/// </para>
/// <para>
/// <strong>Four AddNode Overloads:</strong>
/// </para>
/// <list type="number">
/// <item>Simple: Single input → single output (most common)</item>
/// <item>Multi-Input: CatalogMap&lt;TInput&gt; → single output</item>
/// <item>Multi-Output: Single input → CatalogMap&lt;TOutput&gt;</item>
/// <item>Multi-Input-Output: CatalogMap&lt;TInput&gt; → CatalogMap&lt;TOutput&gt;</item>
/// </list>
/// <para>
/// <strong>Usage Pattern:</strong>
/// </para>
/// <code>
/// var pipeline = PipelineBuilder.CreatePipeline(builder =>
/// {
///     // Simple node
///     builder.AddNode&lt;PreprocessNode&gt;(
///         input: catalog.RawData,
///         output: catalog.ProcessedData
///     );
///     
///     // Multi-input node
///     var inputs = new CatalogMap&lt;JoinInputs&gt;()
///         .Map(x => x.DataA, catalog.DataA)
///         .Map(x => x.DataB, catalog.DataB);
///     
///     builder.AddNode&lt;JoinNode&gt;(
///         input: inputs,
///         output: catalog.JoinedData
///     );
/// });
/// 
/// pipeline.Build();
/// await pipeline.ExecuteAsync();
/// </code>
/// </remarks>
public class PipelineBuilder
{
  private readonly Pipeline _pipeline = new();

  /// <summary>
  /// Creates and configures a new pipeline using the builder pattern.
  /// </summary>
  /// <param name="configure">Action to configure the pipeline by adding nodes</param>
  /// <returns>Configured but not yet built pipeline</returns>
  /// <remarks>
  /// The returned pipeline must have Build() called before execution.
  /// </remarks>
  public static Pipeline CreatePipeline(Action<PipelineBuilder> configure)
  {
    var builder = new PipelineBuilder();
    configure(builder);
    return builder._pipeline;
  }

  // ═══════════════════════════════════════════════════════════════════════════════
  // OVERLOAD 1: Simple (Single Input → Single Output)
  // ═══════════════════════════════════════════════════════════════════════════════

  /// <summary>
  /// Adds a simple node with single input and single output to the pipeline.
  /// </summary>
  /// <typeparam name="TNode">The node type (must inherit from NodeBase)</typeparam>
  /// <typeparam name="TItem">The input item type (what the node processes)</typeparam>
  /// <typeparam name="TOutputItem">The output item type (what the node produces)</typeparam>
  /// <typeparam name="TParameters">The parameters type (defaults to NoParams)</typeparam>
  /// <param name="input">Catalog entry containing a collection of input items</param>
  /// <param name="output">Catalog entry to store a collection of output items</param>
  /// <param name="name">Optional node name (defaults to node type name)</param>
  /// <param name="configure">Optional action to configure the node instance</param>
  /// <returns>This builder for fluent chaining</returns>
  /// <remarks>
  /// <para>
  /// This is the most common overload for simple transformations. The node receives
  /// data directly from the input catalog entry and writes directly to the output entry.
  /// </para>
  /// <para>
  /// <strong>Type Parameter Semantics:</strong>
  /// TItem and TOutputItem represent individual item types (e.g., CompanySchema), 
  /// while catalog entries store collections (e.g., IEnumerable&lt;CompanySchema&gt;).
  /// This matches the semantic meaning in NodeBase where TInput refers to the item type.
  /// </para>
  /// </remarks>
  public PipelineBuilder AddNode<TNode, TItem, TOutputItem, TParameters>(
    ICatalogEntry<IEnumerable<TItem>> input,
    ICatalogEntry<IEnumerable<TOutputItem>> output,
    string? name = null,
    Action<TNode>? configure = null)
    where TNode : NodeBase<TItem, TOutputItem, TParameters>, new()
    where TParameters : new()
  {
    var node = new TNode();
    configure?.Invoke(node);

    var pipelineNode = new PipelineNode(
      name: name ?? typeof(TNode).Name,
      nodeInstance: node,
      inputs: new[] { input },
      outputs: new[] { output }
    );

    _pipeline.AddNode(pipelineNode);
    return this;
  }

  // ═══════════════════════════════════════════════════════════════════════════════
  // OVERLOAD 2: Multi-Input → Single Output
  // ═══════════════════════════════════════════════════════════════════════════════

  /// <summary>
  /// Adds a multi-input node to the pipeline using CatalogMap for input mapping.
  /// </summary>
  /// <typeparam name="TNode">The node type (must inherit from NodeBase)</typeparam>
  /// <typeparam name="TInputSchema">The input schema type containing multiple properties</typeparam>
  /// <typeparam name="TOutputItem">The output item type (what the node produces)</typeparam>
  /// <typeparam name="TParameters">The parameters type (defaults to NoParams)</typeparam>
  /// <param name="input">CatalogMap that maps input schema properties to catalog entries</param>
  /// <param name="output">Catalog entry to store a collection of output items</param>
  /// <param name="name">Optional node name (defaults to node type name)</param>
  /// <param name="configure">Optional action to configure the node instance</param>
  /// <returns>This builder for fluent chaining</returns>
  /// <remarks>
  /// <para>
  /// Use this overload when a node needs to read from multiple catalog entries.
  /// The CatalogMap bundles multiple entries into a single input schema instance.
  /// </para>
  /// <para>
  /// <strong>Type Parameter Semantics:</strong>
  /// TInputSchema is the multi-input schema type (e.g., JoinInputs),
  /// while TOutputItem is the individual output item type (e.g., JoinedRecord).
  /// The output catalog entry stores IEnumerable&lt;TOutputItem&gt;.
  /// </para>
  /// <para>
  /// Example:
  /// <code>
  /// var inputs = new CatalogMap&lt;JoinInputs&gt;()
  ///     .Map(x => x.TableA, catalog.TableA)
  ///     .Map(x => x.TableB, catalog.TableB);
  /// 
  /// builder.AddNode&lt;JoinNode, JoinInputs, JoinedRecord, NoParams&gt;(
  ///     input: inputs, 
  ///     output: catalog.Joined);
  /// </code>
  /// </para>
  /// </remarks>
  public PipelineBuilder AddNode<TNode, TInputSchema, TOutputItem, TParameters>(
    CatalogMap<TInputSchema> input,
    ICatalogEntry<IEnumerable<TOutputItem>> output,
    string? name = null,
    Action<TNode>? configure = null)
    where TNode : NodeBase<TInputSchema, TOutputItem, TParameters>, new()
    where TInputSchema : new()
    where TParameters : new()
  {
    var node = new TNode();
    configure?.Invoke(node);

    // Validate that all required input properties are mapped
    input.ValidateComplete();

    // Extract all catalog entries from the input map
    var inputEntries = input.GetMappedEntries();

    var pipelineNode = new PipelineNode(
      name: name ?? typeof(TNode).Name,
      nodeInstance: node,
      inputs: inputEntries.ToList(),
      outputs: new[] { output },
      inputMappings: input.Mappings
    );

    _pipeline.AddNode(pipelineNode);
    return this;
  }

  // ═══════════════════════════════════════════════════════════════════════════════
  // OVERLOAD 3: Single Input → Multi-Output
  // ═══════════════════════════════════════════════════════════════════════════════

  /// <summary>
  /// Adds a multi-output node to the pipeline using CatalogMap for output mapping.
  /// </summary>
  /// <typeparam name="TNode">The node type (must inherit from NodeBase)</typeparam>
  /// <typeparam name="TItem">The input item type (what the node processes)</typeparam>
  /// <typeparam name="TOutputSchema">The output schema type containing multiple properties</typeparam>
  /// <typeparam name="TParameters">The parameters type (defaults to NoParams)</typeparam>
  /// <param name="input">Catalog entry containing a collection of input items</param>
  /// <param name="output">CatalogMap that maps output schema properties to catalog entries</param>
  /// <param name="name">Optional node name (defaults to node type name)</param>
  /// <param name="configure">Optional action to configure the node instance</param>
  /// <returns>This builder for fluent chaining</returns>
  /// <remarks>
  /// <para>
  /// Use this overload when a node produces multiple outputs (e.g., train/test split).
  /// The CatalogMap distributes output schema properties to separate catalog entries.
  /// </para>
  /// <para>
  /// <strong>Type Parameter Semantics:</strong>
  /// TItem is the individual input item type (e.g., ModelInputSchema),
  /// while TOutputSchema is the multi-output schema type (e.g., SplitDataOutputs).
  /// The input catalog entry stores IEnumerable&lt;TItem&gt;.
  /// </para>
  /// <para>
  /// Example:
  /// <code>
  /// var outputs = new CatalogMap&lt;SplitOutputs&gt;()
  ///     .Map(x => x.Train, catalog.TrainData)
  ///     .Map(x => x.Test, catalog.TestData);
  /// 
  /// builder.AddNode&lt;SplitNode, ModelInput, SplitOutputs, NoParams&gt;(
  ///     input: catalog.FullData, 
  ///     output: outputs);
  /// </code>
  /// </para>
  /// </remarks>
  public PipelineBuilder AddNode<TNode, TItem, TOutputSchema, TParameters>(
    ICatalogEntry<IEnumerable<TItem>> input,
    CatalogMap<TOutputSchema> output,
    string? name = null,
    Action<TNode>? configure = null)
    where TNode : NodeBase<TItem, TOutputSchema, TParameters>, new()
    where TOutputSchema : new()
    where TParameters : new()
  {
    var node = new TNode();
    configure?.Invoke(node);

    // Validate that all required output properties are mapped
    output.ValidateComplete();

    // Extract all catalog entries from the output map
    var outputEntries = output.GetMappedEntries();

    var pipelineNode = new PipelineNode(
      name: name ?? typeof(TNode).Name,
      nodeInstance: node,
      inputs: new[] { input },
      outputs: outputEntries.ToList()
    );

    _pipeline.AddNode(pipelineNode);
    return this;
  }

  // ═══════════════════════════════════════════════════════════════════════════════
  // OVERLOAD 4: Multi-Input → Multi-Output
  // ═══════════════════════════════════════════════════════════════════════════════

  /// <summary>
  /// Adds a multi-input, multi-output node to the pipeline using CatalogMaps.
  /// </summary>
  /// <typeparam name="TNode">The node type (must inherit from NodeBase)</typeparam>
  /// <typeparam name="TInputSchema">The input schema type containing multiple properties</typeparam>
  /// <typeparam name="TOutputSchema">The output schema type containing multiple properties</typeparam>
  /// <typeparam name="TParameters">The parameters type (defaults to NoParams)</typeparam>
  /// <param name="input">CatalogMap that maps input schema properties to catalog entries</param>
  /// <param name="output">CatalogMap that maps output schema properties to catalog entries</param>
  /// <param name="name">Optional node name (defaults to node type name)</param>
  /// <param name="configure">Optional action to configure the node instance</param>
  /// <returns>This builder for fluent chaining</returns>
  /// <remarks>
  /// <para>
  /// This overload handles the most complex scenario: multiple inputs and multiple outputs.
  /// Both inputs and outputs are bundled via CatalogMaps.
  /// </para>
  /// <para>
  /// <strong>Type Parameter Semantics:</strong>
  /// Both TInputSchema and TOutputSchema are schema types containing multiple properties
  /// that represent collections of data from different catalog entries.
  /// </para>
  /// </remarks>
  public PipelineBuilder AddNode<TNode, TInputSchema, TOutputSchema, TParameters>(
    CatalogMap<TInputSchema> input,
    CatalogMap<TOutputSchema> output,
    string? name = null,
    Action<TNode>? configure = null)
    where TNode : NodeBase<TInputSchema, TOutputSchema, TParameters>, new()
    where TInputSchema : new()
    where TOutputSchema : new()
    where TParameters : new()
  {
    var node = new TNode();
    configure?.Invoke(node);

    // Validate that all required properties are mapped
    input.ValidateComplete();
    output.ValidateComplete();

    // Extract all catalog entries
    var inputEntries = input.GetMappedEntries();
    var outputEntries = output.GetMappedEntries();

    var pipelineNode = new PipelineNode(
      name: name ?? typeof(TNode).Name,
      nodeInstance: node,
      inputs: inputEntries.ToList(),
      outputs: outputEntries.ToList()
    );

    _pipeline.AddNode(pipelineNode);
    return this;
  }
}
