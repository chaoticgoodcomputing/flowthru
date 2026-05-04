namespace Flowthru.Extensions.EFCore.Lifecycle;

/// <summary>
/// Configuration knobs for
/// <see cref="EFCoreResources.EphemeralDatabase{TContext}"/>.
/// </summary>
/// <remarks>
/// <para>
/// Mutable to support the fluent <c>configure: o =&gt; { o.Property = ... }</c>
/// pattern. Defaults reflect the strict-cleanup expectation: any leftover
/// staging artifact is wrong unless the consumer explicitly opts in to
/// preservation for debugging.
/// </para>
/// </remarks>
public sealed class EphemeralDatabaseOptions
{
  /// <summary>
  /// When <c>true</c>, the database is preserved if the flow fails, so the
  /// developer can inspect intermediate state. Default: <c>false</c> — the
  /// database is always dropped on flow exit.
  /// </summary>
  /// <remarks>
  /// The framework provides the body's primary exception to the resource's
  /// release closure; this option simply gates whether the drop runs when
  /// that exception is non-null.
  /// </remarks>
  public bool PreserveOnFailure { get; set; } = false;
}
