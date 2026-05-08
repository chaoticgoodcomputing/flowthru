namespace Flowthru.Data.Storage.EFCore;

/// <summary>
/// Configuration knobs for
/// <see cref="EFCoreLifecycleExtensions.EphemeralDatabase"/>.
/// </summary>
/// <remarks>
/// Mutable to support the fluent <c>configure: o =&gt; o.Property = ...</c>
/// pattern. Defaults reflect the strict-cleanup expectation: a
/// leftover staging artifact is wrong unless the caller explicitly
/// opts in to preservation for debugging.
/// </remarks>
public sealed class EphemeralDatabaseOptions
{
  /// <summary>
  /// When <c>true</c>, the database is preserved if the flow fails so
  /// the developer can inspect intermediate state. Default
  /// <c>false</c> — the database is always dropped on flow exit.
  /// </summary>
  /// <remarks>
  /// The framework hands the body's primary
  /// <see cref="Flowthru.Validation.Runtime.RuntimeError"/> to the
  /// resource's release closure; this option simply gates whether the
  /// drop runs when that error is non-null.
  /// </remarks>
  public bool PreserveOnFailure { get; set; } = false;
}
