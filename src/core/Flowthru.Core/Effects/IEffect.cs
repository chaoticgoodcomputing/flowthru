using Flowthru.Core.Data.Validation;
using Flowthru.Core.Graph;

namespace Flowthru.Core.Effects;

/// <summary>
/// A DAG node representing a general side effect — an operation that interacts
/// with an external system (webhook, deployment, DDL mutation, notification, etc.).
/// </summary>
/// <typeparam name="T">
/// The result type of the effect. Use <see cref="FlowUnit"/> for fire-and-forget
/// effects that produce no meaningful return value.
/// </typeparam>
/// <remarks>
/// <para>
/// <see cref="Execute"/> is the domain-specific alias for <see cref="INode{T}.Produce"/>.
/// <see cref="INode{T}.Consume"/> triggers the effect with a payload.
/// </para>
/// <para>
/// <see cref="INode.Validate"/> is required — effect nodes without validation are
/// incomplete. Implementations should perform healthchecks or reachability probes
/// appropriate to the external system.
/// </para>
/// </remarks>
public interface IEffect<T> : INode<T>
{
  /// <summary>
  /// Executes the side effect and returns a typed result.
  /// This is the domain alias for <see cref="INode{T}.Produce"/>.
  /// </summary>
  FlowIO<T> Execute();

  /// <summary>
  /// Effect-specific capability metadata.
  /// </summary>
  EffectTraits EffectTraits { get; }

  /// <inheritdoc/>
  FlowIO<T> INode<T>.Produce() => Execute();

  /// <inheritdoc/>
  NodeTraits INode.Traits => EffectTraits;
}
