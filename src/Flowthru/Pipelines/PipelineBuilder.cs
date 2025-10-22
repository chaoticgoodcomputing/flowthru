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
/// <strong>Unified API (v0.3.0):</strong>
/// AddNode now has a single overload that handles all cases. CatalogMap&lt;T&gt; implements
/// ICatalogEntry, allowing both simple catalog entries and multi-input/output maps to be
/// passed uniformly.
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
///     // Multi-input node: CatalogMap → single output
///     builder.AddNode&lt;JoinNode&gt;(
///         input: new CatalogMap&lt;JoinInputs&gt;()
///             .Map(x => x.DataA, catalog.DataA)
///             .Map(x => x.DataB, catalog.DataB),
///         output: catalog.JoinedData
///     );
///     
///     // Multi-output node: single input → CatalogMap
///     builder.AddNode&lt;SplitNode&gt;(
///         input: catalog.Data,
///         output: new CatalogMap&lt;SplitOutputs&gt;()
///             .Map(x => x.Train, catalog.Train)
///             .Map(x => x.Test, catalog.Test)
///     );
///     
///     // Multi-input-output: CatalogMap → CatalogMap
///     builder.AddNode&lt;ComplexNode&gt;(
///         input: new CatalogMap&lt;ComplexInputs&gt;()...
///         output: new CatalogMap&lt;ComplexOutputs&gt;()...
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
  /// <remarks>
  /// The returned pipeline must have Build() called before execution.
  /// </remarks>
  public static Pipeline CreatePipeline(Action<PipelineBuilder> configure) {
    var builder = new PipelineBuilder();
    configure(builder);
    return builder._pipeline;
  }

  // ===============================================================================
  // UNIFIED API: Single AddNode Overload for All Cases
  // ===============================================================================

  /// <summary>
  /// Adds a node to the pipeline.
  /// Handles all cases: simple, multi-input, multi-output, and multi-input-output.
  /// </summary>
  /// <typeparam name="TNode">The node type (must inherit from NodeBase)</typeparam>
  /// <param name="input">
  /// Input catalog entry or CatalogMap.
  /// - For simple nodes: pass catalog entry directly (e.g., catalog.RawData)
  /// - For multi-input nodes: pass CatalogMap&lt;TInputSchema&gt; with mapped properties
  /// </param>
  /// <param name="output">
  /// Output catalog entry or CatalogMap.
  /// - For simple nodes: pass catalog entry directly (e.g., catalog.ProcessedData)
  /// - For multi-output nodes: pass CatalogMap&lt;TOutputSchema&gt; with mapped properties
  /// </param>
  /// <param name="name">Optional node name (defaults to node type name)</param>
  /// <param name="configure">Optional action to configure the node instance</param>
  /// <returns>This builder for fluent chaining</returns>
  /// <remarks>
  /// <para>
  /// <strong>Unified API (v0.3.0):</strong> CatalogMap now implements ICatalogEntry,
  /// allowing a single AddNode overload to handle all scenarios uniformly.
  /// </para>
  /// <para>
  /// <strong>Examples:</strong>
  /// </para>
  /// <code>
  /// // Simple: single input → single output
  /// builder.AddNode&lt;PreprocessNode&gt;(
  ///     input: catalog.RawData,
  ///     output: catalog.ProcessedData
  /// );
  /// 
  /// // Multi-input: CatalogMap → single output
  /// builder.AddNode&lt;JoinNode&gt;(
  ///     input: new CatalogMap&lt;JoinInputs&gt;()
  ///         .Map(x => x.DataA, catalog.DataA)
  ///         .Map(x => x.DataB, catalog.DataB),
  ///     output: catalog.JoinedData
  /// );
  /// 
  /// // Multi-output: single input → CatalogMap
  /// builder.AddNode&lt;SplitNode&gt;(
  ///     input: catalog.Data,
  ///     output: new CatalogMap&lt;SplitOutputs&gt;()
  ///         .Map(x => x.Train, catalog.Train)
  ///         .Map(x => x.Test, catalog.Test)
  /// );
  /// 
  /// // Multi-input-output: CatalogMap → CatalogMap
  /// builder.AddNode&lt;ComplexNode&gt;(
  ///     input: new CatalogMap&lt;Inputs&gt;()...,
  ///     output: new CatalogMap&lt;Outputs&gt;()...
  /// );
  /// </code>
  /// </para>
  /// </remarks>
  public PipelineBuilder AddNode<TNode>(
    ICatalogEntry input,
    ICatalogEntry output,
    string? name = null,
    Action<TNode>? configure = null)
    where TNode : class, new() {
    // Extract type parameters from node's base class
    var nodeType = typeof(TNode);
    var (tInput, tOutput, tParameters) = NodeTypeInfo.ExtractTypeArguments(nodeType);

    // Create node instance
    var node = new TNode();
    configure?.Invoke(node);

    // Determine if inputs/outputs are CatalogMaps
    var inputIsCatalogMap = input.GetType().IsGenericType &&
                            input.GetType().GetGenericTypeDefinition() == typeof(CatalogMap<>);
    var outputIsCatalogMap = output.GetType().IsGenericType &&
                             output.GetType().GetGenericTypeDefinition() == typeof(CatalogMap<>);

    // Build the pipeline node based on input/output types
    List<ICatalogEntry> inputEntries;
    List<ICatalogEntry> outputEntries;
    IReadOnlyList<CatalogMapping>? inputMappings = null;
    IReadOnlyList<CatalogMapping>? outputMappings = null;

    if (inputIsCatalogMap) {
      // Multi-input: extract entries and mappings from CatalogMap
      var catalogMapType = input.GetType();
      var getMappedEntriesMethod = catalogMapType.GetMethod("GetMappedEntries",
        System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
      var mappingsProperty = catalogMapType.GetProperty("Mappings",
        System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
      var validateMethod = catalogMapType.GetMethod("ValidateComplete");

      validateMethod?.Invoke(input, null);
      var entries = getMappedEntriesMethod?.Invoke(input, null) as IEnumerable<ICatalogEntry>;
      inputEntries = entries?.ToList() ?? new List<ICatalogEntry>();
      inputMappings = mappingsProperty?.GetValue(input) as IReadOnlyList<CatalogMapping>;
    } else {
      inputEntries = new List<ICatalogEntry> { input };
    }

    if (outputIsCatalogMap) {
      // Multi-output: extract entries and mappings from CatalogMap
      var catalogMapType = output.GetType();
      var getMappedEntriesMethod = catalogMapType.GetMethod("GetMappedEntries",
        System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
      var mappingsProperty = catalogMapType.GetProperty("Mappings",
        System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
      var validateMethod = catalogMapType.GetMethod("ValidateComplete");

      validateMethod?.Invoke(output, null);
      var entries = getMappedEntriesMethod?.Invoke(output, null) as IEnumerable<ICatalogEntry>;
      outputEntries = entries?.ToList() ?? new List<ICatalogEntry>();
      outputMappings = mappingsProperty?.GetValue(output) as IReadOnlyList<CatalogMapping>;
    } else {
      outputEntries = new List<ICatalogEntry> { output };
    }

    var pipelineNode = new PipelineNode(
      name: name ?? nodeType.Name,
      nodeInstance: node,
      inputs: inputEntries,
      outputs: outputEntries,
      inputMappings: inputMappings,
      outputMappings: outputMappings
    );

    _pipeline.AddNode(pipelineNode);
    return this;
  }

}

