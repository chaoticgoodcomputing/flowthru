namespace Flowthru.Flow;

/// <summary>
/// Host-level execution defaults — the stable-policy knobs a user or
/// configuration legitimately sets once, as opposed to the per-run
/// <see cref="ExecutionOptions"/> the engine threads through a single
/// flow run. This is a mutable, parameterless POCO precisely so it
/// participates in the standard .NET Options pattern: bind it from
/// <c>appsettings.json</c>
/// (<c>services.Configure&lt;ExecutionDefaults&gt;(config.GetSection("Flowthru:Execution"))</c>)
/// or set it imperatively
/// (<c>flowthru.ConfigureExecution(o =&gt; o.Parallelism = 4)</c>).
/// </summary>
/// <remarks>
/// <para>
/// <strong>Why not bind <see cref="ExecutionOptions"/> directly?</strong>
/// <see cref="ExecutionOptions"/> is an immutable record that also
/// carries framework-computed, per-run state (notably
/// <see cref="ExecutionOptions.CachePlan"/>). The Options binder needs a
/// mutable target, and that per-run state must never be user-settable —
/// so the bindable surface is this narrow, knob-only type. The host maps
/// it onto a base <see cref="ExecutionOptions"/> when no per-call options
/// are supplied.
/// </para>
/// <para>
/// <strong>Fail-fast.</strong> The framework registers a validator
/// (<see cref="Parallelism"/> ≥ 1) so a bad value surfaces during
/// pre-flight — when the options are first resolved, before any flow
/// logic runs — rather than being silently clamped at runtime.
/// </para>
/// </remarks>
public sealed class ExecutionDefaults
{
  /// <summary>
  /// Default maximum number of steps that may run concurrently within a
  /// topological layer, used to seed <see cref="ExecutionOptions.Parallelism"/>
  /// when a run does not specify its own. Default <c>1</c> (sequential).
  /// Must be ≥ 1.
  /// </summary>
  public int Parallelism { get; set; } = 1;
}
