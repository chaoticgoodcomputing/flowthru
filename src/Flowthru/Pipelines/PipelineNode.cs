using Flowthru.Data;
using Flowthru.Nodes;

namespace Flowthru.Pipelines;

/// <summary>
/// Represents a node within a pipeline, wrapping the transformation function with metadata
/// about its inputs, outputs, and dependencies.
/// </summary>
/// <remarks>
/// <para>
/// PipelineNode serves as the internal representation of a node during pipeline
/// construction and execution. It tracks:
/// - The transformation function (Func&lt;TInput, Task&lt;TOutput&gt;&gt;)
/// - Input catalog entries (what data it reads)
/// - Output catalog entries (what data it writes)
/// - Dependencies (other nodes that must run first)
/// </para>
/// <para>
/// <strong>Single Producer Rule:</strong> Each catalog entry can be written by at most
/// one node in a pipeline. This constraint ensures deterministic dependency resolution
/// and enables simple DAG construction via topological sort.
/// </para>
/// <para>
/// <strong>Function-Based Architecture (v0.5.0):</strong> Nodes are now pure transformation
/// functions instead of class instances. This enables compile-time type safety through
/// generic type inference at the pipeline construction site.
/// </para>
/// </remarks>
internal class PipelineNode
{
  /// <summary>
  /// Unique identifier for this node within the pipeline.
  /// Typically the node type name or user-provided name.
  /// </summary>
  public string Label { get; }

  /// <summary>
  /// String description of the node's purpose.
  /// </summary>
  public string Description { get; }

  /// <summary>
  /// The transformation function that performs the node's work.
  /// Type-erased to Delegate since we need to store different function signatures together.
  /// </summary>
  /// <remarks>
  /// <para>
  /// At execution time, this delegate will be invoked via DynamicInvoke with the
  /// appropriate input parameter(s). The function signature is:
  /// - Single input: Func&lt;TInput, Task&lt;TOutput&gt;&gt;
  /// - Multi-input: Func&lt;(TIn1, TIn2, ...), Task&lt;TOutput&gt;&gt;
  /// - Multi-output: Func&lt;TInput, Task&lt;(TOut1, TOut2, ...)&gt;&gt;
  /// </para>
  /// </remarks>
  public Delegate TransformFunction { get; }

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
  public int Layer { get; set; } = -1; // -1 indicates not yet assigned

  /// <summary>
  /// Creates a new pipeline node with a transformation function.
  /// </summary>
  /// <param name="label">Unique identifier for this node</param>
  /// <param name="node">The transformation function (Func&lt;TInput, Task&lt;TOutput&gt;&gt;)</param>
  /// <param name="inputs">Catalog entries this node reads</param>
  /// <param name="outputs">Catalog entries this node writes</param>
  public PipelineNode(
    string label,
    string? description,
    Delegate node,
    IReadOnlyList<ICatalogEntry> inputs,
    IReadOnlyList<ICatalogEntry> outputs
  )
  {
    Label = label;
    Description = description ?? string.Empty;
    TransformFunction = node;
    Inputs = inputs;
    Outputs = outputs;
  }

  /// <summary>
  /// Returns a string representation for debugging.
  /// </summary>
  public override string ToString() =>
    $"PipelineNode({Label}, Layer={Layer}, Inputs={Inputs.Count}, Outputs={Outputs.Count}, Dependencies={Dependencies.Count})";
}
