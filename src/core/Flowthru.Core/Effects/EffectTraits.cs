using Flowthru.Core.Graph;

namespace Flowthru.Core.Effects;

/// <summary>
/// Capability metadata for side-effect nodes.
/// </summary>
/// <remarks>
/// Extends <see cref="NodeTraits"/> with properties specific to effects —
/// operations that interact with external systems (webhooks, deployments,
/// DDL mutations, notifications).
/// </remarks>
public record EffectTraits : NodeTraits
{
    /// <summary>
    /// Whether the effect is safe to retry without changing the outcome.
    /// </summary>
    public bool IsIdempotent { get; init; }

    /// <summary>
    /// Whether the effect modifies external state when executed.
    /// </summary>
    public bool HasSideEffects { get; init; } = true;
}
