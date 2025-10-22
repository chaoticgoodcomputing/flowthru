using Flowthru.Data;
using Flowthru.Nodes;

namespace Flowthru.Pipelines;

/// <summary>
/// Represents a node within a pipeline, wrapping the node instance with metadata
/// about its inputs, outputs, and dependencies.
/// </summary>
/// <remarks>
/// <para>
/// PipelineNode serves as the internal representation of a node during pipeline
/// construction and execution. It tracks:
/// - The node instance (transformation logic)
/// - Input catalog entries (what data it reads)
/// - Output catalog entries (what data it writes)
/// - Dependencies (other nodes that must run first)
/// </para>
/// <para>
/// <strong>Single Producer Rule:</strong> Each catalog entry can be written by at most
/// one node in a pipeline. This constraint ensures deterministic dependency resolution
/// and enables simple DAG construction via topological sort.
/// </para>
/// </remarks>
internal class PipelineNode {
  /// <summary>
  /// Unique identifier for this node within the pipeline.
  /// Typically the node type name or user-provided name.
  /// </summary>
  public string Name { get; }

  /// <summary>
  /// The node instance that performs the transformation.
  /// Type-erased to object since we need to store different node types together.
  /// </summary>
  /// <remarks>
  /// At execution time, this will be cast to the appropriate NodeBase&lt;TInput, TOutput, TParameters&gt;
  /// type and invoked via reflection or compiled expressions.
  /// </remarks>
  public object NodeInstance { get; }

  /// <summary>
  /// Catalog entries that this node reads as input.
  /// These may be produced by other nodes (dependencies) or be external prerequisites.
  /// </summary>
  public IReadOnlyList<ICatalogEntry> Inputs { get; }

  /// <summary>
  /// Catalog entries that this node writes as output.
  /// Per the single producer rule, each entry here must be unique across all nodes.
  /// </summary>
  public IReadOnlyList<ICatalogEntry> Outputs { get; }

  /// <summary>
  /// Other pipeline nodes that must execute before this node.
  /// Populated during dependency analysis by checking which nodes produce our inputs.
  /// </summary>
  /// <remarks>
  /// This forms the edges of the execution DAG:
  /// - If node A produces output X, and node B consumes input X, then B depends on A.
  /// - Topological sort uses these dependencies to determine execution order.
  /// </remarks>
  public List<PipelineNode> Dependencies { get; } = new();

  /// <summary>
  /// Execution layer determined by topological sort.
  /// Nodes in layer 0 have no dependencies. Nodes in layer N depend on nodes in layers 0..N-1.
  /// </summary>
  /// <remarks>
  /// Layers enable both sequential and parallel execution:
  /// - Sequential: Execute all nodes in layer 0, then layer 1, then layer 2, etc.
  /// - Parallel: Execute all nodes within the same layer concurrently (Phase 2)
  /// </remarks>
  public int Layer { get; set; } = -1; // -1 indicates not yet assigned

  /// <summary>
  /// Property-to-catalog mappings for multi-input nodes.
  /// Null for single-input nodes (pass-through).
  /// </summary>
  /// <remarks>
  /// When a node has multiple inputs (via CatalogMap), this stores the mapping
  /// between property names on the input schema and catalog entries.
  /// Used during execution to construct the input object correctly.
  /// </remarks>
  public IReadOnlyList<Mapping.CatalogMapping>? InputMappings { get; }

  /// <summary>
  /// Property-to-catalog mappings for multi-output nodes.
  /// Null for single-output nodes (pass-through).
  /// </summary>
  /// <remarks>
  /// When a node has multiple outputs (via CatalogMap), this stores the mapping
  /// between property names on the output schema and catalog entries.
  /// Used during execution to save the output properties to correct catalog entries.
  /// </remarks>
  public IReadOnlyList<Mapping.CatalogMapping>? OutputMappings { get; }

  /// <summary>
  /// Creates a new pipeline node.
  /// </summary>
  /// <param name="name">Unique identifier for this node</param>
  /// <param name="nodeInstance">The node instance that performs the transformation</param>
  /// <param name="inputs">Catalog entries this node reads</param>
  /// <param name="outputs">Catalog entries this node writes</param>
  /// <param name="inputMappings">Optional property-to-catalog mappings for multi-input nodes</param>
  /// <param name="outputMappings">Optional property-to-catalog mappings for multi-output nodes</param>
  public PipelineNode(
    string name,
    object nodeInstance,
    IReadOnlyList<ICatalogEntry> inputs,
    IReadOnlyList<ICatalogEntry> outputs,
    IReadOnlyList<Mapping.CatalogMapping>? inputMappings = null,
    IReadOnlyList<Mapping.CatalogMapping>? outputMappings = null) {
    Name = name;
    NodeInstance = nodeInstance;
    Inputs = inputs;
    Outputs = outputs;
    InputMappings = inputMappings;
    OutputMappings = outputMappings;
  }

  /// <summary>
  /// Returns a string representation for debugging.
  /// </summary>
  public override string ToString() =>
    $"PipelineNode({Name}, Layer={Layer}, Inputs={Inputs.Count}, Outputs={Outputs.Count}, Dependencies={Dependencies.Count})";
}
