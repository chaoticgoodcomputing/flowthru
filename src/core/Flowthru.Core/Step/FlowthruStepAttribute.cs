using System.Diagnostics.CodeAnalysis;

namespace Flowthru.Step;

/// <summary>
/// Marks a static class as a Flowthru step. The
/// <c>StepMetadataGenerator</c> source generator emits a companion
/// <c>{ClassName}_Metadata</c> record describing the step (label,
/// archetype, traits) — used by diagnostics, metadata exporters, and
/// architecture tests that walk every step in an assembly.
/// </summary>
/// <remarks>
/// <para>
/// Per §2.4, every <c>[FlowthruStep]</c>-decorated class follows the
/// single canonical authoring shape:
/// <code>
/// [FlowthruStep]
/// public static class FooStep
/// {
///   public static Func&lt;TIn, TOut&gt; Create() => input => { /* … */ };
/// }
/// </code>
/// </para>
/// <para>
/// The framework calls <c>Create</c> once at flow-construction time,
/// captures the returned delegate, and wraps it as the
/// <see cref="IStepNode{TIn, TOut}.Transform"/>. Service injection
/// happens via <c>Create</c>'s parameters (Reader-shaped closure). The
/// transform delegate's signature is the canonical declaration of the
/// step's input/output types — there is no second mechanism.
/// </para>
/// </remarks>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
[ExcludeFromCodeCoverage] // Decorator only; behaviour exercised through StepMetadataGenerator output.
public sealed class FlowthruStepAttribute : Attribute
{
  /// <summary>
  /// Optional override for the step's display label. Defaults to the
  /// step class name.
  /// </summary>
  public string? Label { get; init; }

  /// <summary>True if the step is idempotent (rerunnable safely).</summary>
  public bool IsIdempotent { get; init; }

  /// <summary>True if the step has side effects beyond declared outputs.</summary>
  public bool HasSideEffects { get; init; }

  /// <summary>
  /// Optional explicit code-identity override. When set, the source
  /// generator emits this value verbatim as the step's
  /// <c>CodeVersion</c> companion constant; when left null, the
  /// generator computes a SHA-256 prefix over the step class's
  /// normalized source text (trivia stripped) and emits that hex
  /// digest instead.
  /// </summary>
  /// <remarks>
  /// <para>
  /// Used by downstream cache-plan logic to decide when a step's
  /// recorded output can be reused. Set this explicitly when you want
  /// a stable cross-machine identity that survives cosmetic refactors
  /// the trivia-stripper misses, or when you want to deliberately
  /// invalidate every cached run by bumping a version string. Leaving
  /// it null is the recommended default — the computed digest invalidates
  /// only when the step's actual logic changes.
  /// </para>
  /// </remarks>
  public string? CodeVersion { get; init; }
}
