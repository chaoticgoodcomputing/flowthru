namespace Flowthru.Core.Steps;

/// <summary>
/// Capability metadata for a step, extracted from <see cref="FlowthruStepAttribute"/>
/// at compile time and emitted into a sibling <c>_Metadata</c> static class by
/// <c>StepMetadataGenerator</c>.
/// </summary>
/// <param name="IsIdempotent">
/// Whether the step is safe to retry without changing the outcome.
/// Mirrors <see cref="FlowthruStepAttribute.IsIdempotent"/>.
/// </param>
/// <param name="HasSideEffects">
/// Whether the step modifies external state when executed.
/// Mirrors <see cref="FlowthruStepAttribute.HasSideEffects"/>.
/// </param>
/// <remarks>
/// Phase 4 of the effects-as-steps initiative emits this metadata but does not yet
/// consume it at runtime. The DAG metadata generator (Phase 6) will use it to render
/// step characteristics; future engine work may use it for retry-policy selection
/// and dry-run filtering.
/// </remarks>
public readonly record struct StepTraits(bool IsIdempotent, bool HasSideEffects);
