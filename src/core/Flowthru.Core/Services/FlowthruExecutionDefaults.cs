namespace Flowthru.Core.Services;

/// <summary>
/// Carries service-level execution defaults registered by <see cref="FlowthruServiceBuilder.ConfigureExecution"/>.
/// Injected into <see cref="FlowthruService"/> and applied when a per-call
/// <see cref="Flowthru.Core.Flows.ExecutionOptions"/> does not specify a value.
/// </summary>
internal sealed class FlowthruExecutionDefaults
{
    /// <summary>
    /// Service-level default for <see cref="Flowthru.Core.Flows.ExecutionOptions.MaxDegreeOfParallelism"/>.
    /// <c>null</c> means "not configured at this layer; fall back to 1".
    /// </summary>
    public int? MaxDegreeOfParallelism { get; init; }
}
