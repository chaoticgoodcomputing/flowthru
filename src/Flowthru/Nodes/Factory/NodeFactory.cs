namespace Flowthru.Nodes.Factory;

/// <summary>
/// Factory for creating node instances using TypeActivator.
/// </summary>
/// <remarks>
/// <para>
/// <strong>Design Pattern:</strong> Factory Pattern - provides a centralized location for
/// node instantiation logic.
/// </para>
/// <para>
/// This is a thin wrapper around TypeActivator, providing a domain-specific API for
/// creating nodes. Could be extended in the future with:
/// - Node validation logic
/// - Pre/post-creation hooks
/// - Node decoration/wrapping
/// </para>
/// </remarks>
public static class NodeFactory {
  /// <summary>
  /// Creates a new instance of the specified node type.
  /// </summary>
  /// <typeparam name="TNode">The node type to instantiate</typeparam>
  /// <returns>A new node instance</returns>
  /// <remarks>
  /// <para>
  /// <strong>Requirements:</strong>
  /// - TNode must inherit from NodeBase&lt;TInput, TOutput&gt;
  /// - TNode must have a parameterless constructor
  /// </para>
  /// <para>
  /// These requirements are enforced at compile-time via generic constraints in
  /// PipelineBuilder.AddNode methods.
  /// </para>
  /// </remarks>
  public static TNode Create<TNode>() where TNode : new() {
    return TypeActivator.Create<TNode>();
  }
}
