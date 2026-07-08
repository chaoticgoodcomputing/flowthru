using Flowthru.Flow;

namespace Flowthru.Validation.PreFlight;

/// <summary>
/// Extension hook for pre-flight validation. Each registered hook
/// receives the built flow and returns a
/// <see cref="Validated{TError, TValue}"/> aggregating any failures
/// it found. The pre-flight pipeline runs every hook and combines
/// their results via <see cref="Validated.ZipAll{TError, TValue}"/>
/// — independent hooks accumulate, none short-circuit each other.
/// </summary>
/// <remarks>
/// <para>
/// Per §2.5, this is one of three pre-flight contribution layers
/// (alongside adapter-internal validation and
/// <see cref="IFlowServiceInspector{T}"/>). Hooks are how an extension
/// surfaces flow-shape checks that depend on its own concepts —
/// Python's <c>PythonStepValidator</c> uses a hook to walk all Python
/// steps and verify their decorators, for example.
/// </para>
/// <para>
/// Hooks return <see cref="FlowIO{A}"/> rather than a bare
/// <see cref="Validated{TError, TValue}"/> so that the implementation
/// can perform IO during the check (probing a remote endpoint,
/// reading a file). Failures inside the IO are surfaced as
/// <see cref="PreFlightError.InspectionFailed"/> by the pipeline
/// rather than crashing pre-flight.
/// </para>
/// <para>
/// <strong>I/O classification.</strong> Hooks self-classify via
/// <see cref="MinimumDepth"/> on the same I/O ladder as the run's
/// <see cref="Flowthru.Flow.ValidationDepth"/>, mirroring
/// <see cref="IRegistrationValidationHook.MinimumDepth"/>: a check that
/// reaches nothing outside the process declares <c>Hermetic</c> and runs
/// even in an offline smoke test; a hook that probes a live resource
/// keeps the default <c>Shallow</c> and is skipped below it.
/// </para>
/// </remarks>
public interface IFlowValidationHook
{
  /// <summary>
  /// A short, stable identifier for this hook used in diagnostic
  /// output. Conventionally the extension name + check name (e.g.,
  /// <c>"python.decorator-shape"</c>).
  /// </summary>
  string HookId { get; }

  /// <summary>
  /// The lightest <see cref="Flowthru.Flow.ValidationDepth"/> at which
  /// this hook participates. Defaults to
  /// <see cref="Flowthru.Flow.ValidationDepth.Shallow"/> — the historical
  /// behaviour, where flow hooks ran only when live-resource probing was
  /// enabled. A hook whose check reaches nothing outside the process (see
  /// the hermetic promise on <see cref="Flowthru.Flow.ValidationDepth.Hermetic"/>)
  /// should override this to
  /// <see cref="Flowthru.Flow.ValidationDepth.Hermetic"/> so it still runs
  /// in an offline smoke test. Hooks whose <c>MinimumDepth</c> exceeds the
  /// run's depth are skipped.
  /// </summary>
  Flowthru.Flow.ValidationDepth MinimumDepth => Flowthru.Flow.ValidationDepth.Shallow;

  /// <summary>
  /// Inspect <paramref name="flow"/> and return any failures found.
  /// Empty failure list = pass.
  /// </summary>
  FlowIO<Validated<PreFlightError, FlowUnit>> Validate(BuiltFlow flow);
}
