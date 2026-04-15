using Flowthru.Core.Data.Validation;
using Flowthru.Core.Effects;

namespace Flowthru.Core.Graph;

/// <summary>
/// Engine-level contract for all DAG nodes. The execution engine dispatches
/// <see cref="ProduceUntyped"/>, <see cref="ConsumeUntyped"/>, and <see cref="Validate"/>
/// without knowing the node's specific archetype.
/// </summary>
/// <remarks>
/// <para>
/// Concrete node types (<see cref="Flowthru.Core.Data.IItem"/>,
/// <see cref="IEffect{T}"/>) implement this interface with archetype-appropriate
/// semantics. <see cref="Flowthru.Core.Graph.DependencyAnalyzer"/> resolves dependencies
/// using only <see cref="Label"/> — the engine is archetype-agnostic.
/// </para>
/// </remarks>
public interface INode
{
    /// <summary>
    /// Unique label identifying this node within the DAG.
    /// Used for dependency resolution and wiring.
    /// </summary>
    string Label { get; }

    /// <summary>
    /// The runtime type of the value this node produces.
    /// For singletons: typeof(T). For collections: typeof(IEnumerable&lt;T&gt;).
    /// </summary>
    Type DataType { get; }

    /// <summary>
    /// Capability metadata describing this node's properties and constraints.
    /// </summary>
    NodeTraits Traits { get; }

    /// <summary>
    /// Pre-flight validation. Semantics vary by archetype:
    /// data items check existence and schema, effects perform healthchecks,
    /// steps return success (correctness validated via tests).
    /// </summary>
    FlowIO<ValidationResult> Validate();

    /// <summary>
    /// Produces this node's value as an untyped object.
    /// The engine calls this to load input data for downstream steps.
    /// </summary>
    FlowIO<object> ProduceUntyped();

    /// <summary>
    /// Consumes an untyped value into this node.
    /// The engine calls this to save output data from upstream steps.
    /// </summary>
    FlowIO<FlowUnit> ConsumeUntyped(object data);
}

/// <summary>
/// Typed DAG node contract. Adds strongly-typed <see cref="Produce"/> and
/// <see cref="Consume"/> operations alongside the untyped engine dispatch surface.
/// </summary>
/// <typeparam name="T">
/// The data type this node produces and consumes.
/// Cardinality is encoded in T itself (e.g., IEnumerable&lt;TRow&gt; for collections).
/// </typeparam>
/// <remarks>
/// <para>
/// Default interface implementations bridge typed operations to the untyped engine surface:
/// <see cref="INode.ProduceUntyped"/> boxes the result of <see cref="Produce"/>,
/// and <see cref="INode.ConsumeUntyped"/> casts and delegates to <see cref="Consume"/>.
/// </para>
/// </remarks>
public interface INode<T> : INode
{
    /// <summary>
    /// Produces this node's value as a typed effect.
    /// </summary>
    FlowIO<T> Produce();

    /// <summary>
    /// Consumes a typed value into this node.
    /// </summary>
    FlowIO<FlowUnit> Consume(T data);

    /// <inheritdoc/>
    FlowIO<object> INode.ProduceUntyped() => Produce().Map(value => (object)value!);

    /// <inheritdoc/>
    FlowIO<FlowUnit> INode.ConsumeUntyped(object data) => Consume((T)data);
}
