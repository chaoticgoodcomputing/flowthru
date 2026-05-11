namespace Flowthru.Step;

/// <summary>
/// Per-step capability metadata. Distinct from the universal
/// <c>NodeTraits</c> — step-level flags don't inherit from
/// <c>NodeTraits</c> per §1.6 (a) and §2.10. Instead, the
/// source-generated <c>{StepName}_Metadata</c> companion record
/// carries an instance of this type alongside the universal
/// <c>NodeTraits</c>.
/// </summary>
public record StepTraits
{
  /// <summary>
  /// True if running this step twice with the same inputs always
  /// produces the same outputs and the same observable side effects.
  /// Used by the engine to permit reordering / re-execution under
  /// failure recovery.
  /// </summary>
  public bool IsIdempotent { get; init; } = false;

  /// <summary>
  /// True if this step has observable side effects beyond writing to
  /// its declared outputs (e.g., emits metrics, sends a message,
  /// mutates external state). Used by diagnostics tooling and the
  /// dry-run option.
  /// </summary>
  public bool HasSideEffects { get; init; } = false;
}
