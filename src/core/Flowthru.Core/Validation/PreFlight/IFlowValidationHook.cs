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
/// <see cref="IFlowthruInspector{T}"/>). Hooks are how an extension
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
  /// Inspect <paramref name="flow"/> and return any failures found.
  /// Empty failure list = pass.
  /// </summary>
  FlowIO<Validated<PreFlightError, FlowUnit>> Validate(BuiltFlow flow);
}
