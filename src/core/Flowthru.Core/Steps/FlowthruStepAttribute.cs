using System.Diagnostics.CodeAnalysis;

namespace Flowthru.Core.Steps;

/// <summary>
/// Marker attribute identifying a class as a Flowthru step definition.
/// </summary>
/// <remarks>
/// <para>
/// This attribute enables step discovery by source generators and tooling:
/// </para>
/// <list type="bullet">
/// <item>FUnit source generators use it to discover steps and warn about missing tests.</item>
/// <item><c>StepMetadataGenerator</c> emits a sibling <c>{StepClassName}_Metadata</c>
///   static class carrying <see cref="StepTraits"/> and the inferred service-dependency
///   list, consumed at flow-construction time to populate
///   <see cref="Graph.FlowStep.ServiceDependencies"/>.</item>
/// </list>
/// <para>
/// Follows the same pattern as <c>[FlowthruSchema]</c> — a core marker attribute
/// that downstream generators consume.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// [FlowthruStep(IsIdempotent = true, HasSideEffects = true)]
/// public static class ApplyDeltasStep
/// {
///     public static Func&lt;IEnumerable&lt;Delta&gt;, Task&lt;IEnumerable&lt;SyncRow&gt;&gt;&gt;
///         Create(IRemoteClient client) =&gt; …;
/// }
/// </code>
/// </example>
// Coverage: Roslyn-only attribute — constructor never fires at runtime.
// Consumed by FUnit and Core source generators via Roslyn semantic models.
[ExcludeFromCodeCoverage]
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
public class FlowthruStepAttribute : Attribute
{
  /// <summary>
  /// Whether the step is safe to retry without changing the outcome.
  /// </summary>
  /// <remarks>
  /// Defaults to <c>false</c> (conservative). Pure data transforms with no side effects
  /// can safely declare <c>true</c>; steps that talk to external services must reason
  /// about the service's idempotency contract before declaring this.
  /// </remarks>
  public bool IsIdempotent { get; init; }

  /// <summary>
  /// Whether the step modifies external state when executed.
  /// </summary>
  /// <remarks>
  /// Defaults to <c>false</c>. Steps with no service dependencies and no I/O typically
  /// leave this as <c>false</c>; steps that talk to external services declare <c>true</c>.
  /// </remarks>
  public bool HasSideEffects { get; init; }
}
