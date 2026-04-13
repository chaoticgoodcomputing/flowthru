using Flowthru.Core.Steps;

namespace Flowthru.Core.Graph;

/// <summary>
/// Represents a step within a flow, wrapping the transformation function with metadata
/// about its inputs, outputs, and dependencies.
/// </summary>
/// <remarks>
/// <para>
/// FlowStep serves as the internal representation of a step during flow
/// construction and execution. It tracks:
/// - The transformation function (Func&lt;TInput, Task&lt;TOutput&gt;&gt;)
/// - Input catalog entries (what data it reads)
/// - Output catalog entries (what data it writes)
/// - Dependencies (other steps that must run first)
/// </para>
/// <para>
/// <strong>Single Producer Rule:</strong> Each catalog entry can be written by at most
/// one step in a flow. This constraint ensures deterministic dependency resolution
/// and enables simple DAG construction via topological sort.
/// </para>
/// <para>
/// Made public to enable validation hooks to inspect step properties.
/// This is necessary for extensions (e.g., Python) to validate their own step types.
/// </para>
/// </remarks>
public class FlowStep
{
  /// <summary>
  /// Unique identifier for this step within the flow.
  /// Typically the step type name or user-provided name.
  /// </summary>
  public string Label { get; }

  /// <summary>
  /// String description of the step's purpose.
  /// </summary>
  public string Description { get; }

  /// <summary>
  /// The transformation function that performs the step's work.
  /// Type-erased to Delegate since we need to store different function signatures together.
  /// </summary>
  /// <remarks>
  /// <para>
  /// At execution time, this delegate will be invoked via DynamicInvoke with the
  /// appropriate input parameter(s). The function signature can be either synchronous
  /// or asynchronous:
  /// - Sync single: Func&lt;TInput, TOutput&gt;
  /// - Async single: Func&lt;TInput, Task&lt;TOutput&gt;&gt;
  /// - Sync multi-input: Func&lt;(TIn1, TIn2, ...), TOutput&gt;
  /// - Async multi-input: Func&lt;(TIn1, TIn2, ...), Task&lt;TOutput&gt;&gt;
  /// - Sync multi-output: Func&lt;TInput, (TOut1, TOut2, ...)&gt;
  /// - Async multi-output: Func&lt;TInput, Task&lt;(TOut1, TOut2, ...)&gt;&gt;
  /// </para>
  /// <para>
  /// <strong>Optional Cancellation Support:</strong> Steps can opt-in to cancellation awareness
  /// by accepting a CancellationToken as the last parameter:
  /// - Func&lt;TInput, CancellationToken, Task&lt;TOutput&gt;&gt;
  /// - Func&lt;(TIn1, TIn2), CancellationToken, Task&lt;TOutput&gt;&gt;
  /// </para>
  /// <para>
  /// When a Step accepts a CancellationToken, the Flow will pass the runtime token during
  /// execution, allowing the step to cancel long-running operations cooperatively. Steps that
  /// do not accept a CancellationToken will only be cancelled between step executions.
  /// </para>
  /// <para>
  /// The execution engine detects whether the result is a Task and awaits it if needed.
  /// </para>
  /// </remarks>
  public Delegate TransformFunction { get; }

  /// <summary>
  /// Catalog entries that this step reads as input.
  /// These may be produced by other steps (dependencies) or be external prerequisites.
  /// </summary>
  public IReadOnlyList<INode> Inputs { get; }

  /// <summary>
  /// Catalog entries that this step writes as output.
  /// Per the single producer rule, each entry here must be unique across all steps.
  /// </summary>
  public IReadOnlyList<INode> Outputs { get; }

  /// <summary>
  /// Other Flow steps that must execute before this step.
  /// Populated during dependency analysis by checking which steps produce our inputs.
  /// </summary>
  /// <remarks>
  /// This forms the edges of the execution DAG:
  /// - If step A produces output X, and step B consumes input X, then B depends on A.
  /// - Topological sort uses these dependencies to determine execution order.
  /// </remarks>
  public List<FlowStep> Dependencies { get; } = new();

  /// <summary>
  /// Execution layer determined by topological sort.
  /// Steps in layer 0 have no dependencies. Steps in layer N depend on steps in layers 0..N-1.
  /// </summary>
  public int Layer { get; set; } = -1; // -1 indicates not yet assigned

  /// <summary>
  /// Height in the DAG: the length of the longest path from this step to any sink (leaf).
  /// Sinks have height 0. Used by critical-path scheduling to prioritise steps that unblock
  /// the most downstream work.
  /// </summary>
  /// <remarks>
  /// Populated by <see cref="DependencyAnalyzer.ComputeHeights"/> after the dependency
  /// graph has been built. A value of -1 indicates heights have not yet been computed.
  /// </remarks>
  public int Height { get; set; } = -1; // -1 indicates not yet computed

  /// <summary>
  /// Creates a new Flow step with a transformation function.
  /// </summary>
  /// <param name="label">Unique identifier for this step</param>
  /// <param name="description">Optional description of this step</param>
  /// <param name="step">The transformation function (Func&lt;TInput, Task&lt;TOutput&gt;&gt;)</param>
  /// <param name="inputs">Catalog entries this step reads</param>
  /// <param name="outputs">Catalog entries this step writes</param>
  public FlowStep(
    string label,
    string? description,
    Delegate step,
    IReadOnlyList<INode> inputs,
    IReadOnlyList<INode> outputs
  )
  {
    Label = label;
    Description = description ?? string.Empty;
    TransformFunction = step;
    Inputs = inputs;
    Outputs = outputs;
  }

  /// <summary>
  /// Returns a string representation for debugging.
  /// </summary>
  public override string ToString() =>
    $"FlowStep({Label}, Layer={Layer}, Inputs={Inputs.Count}, Outputs={Outputs.Count}, Dependencies={Dependencies.Count})";
}
