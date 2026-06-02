using Flowthru.Step.Python;

namespace Flowthru.Validation.Runtime.Python;

/// <summary>
/// Declares the <see cref="ServiceProfile"/> of the shared
/// <see cref="IPythonExecutor"/> — the conflict resource every Python step
/// depends on (see <c>PythonStep&lt;,&gt;.ExecutorDependency</c>). The
/// shipped <c>SubprocessPythonExecutor</c> is a single, lock-serialized
/// worker process, so concurrent Python steps must not co-run; this
/// contributor reports the executor's
/// <see cref="IPythonExecutor.MaxConcurrency"/> as the executor key's
/// capacity, and the <c>ParallelFlowScheduler</c> gates accordingly.
/// </summary>
/// <remarks>
/// <para>
/// Registered by <c>UsePython()</c> and aggregated by Core's
/// <c>CompositeServiceProfileProvider</c> alongside every other
/// extension's contributor. It speaks only for the
/// <see cref="IPythonExecutor"/> dependency and returns <c>null</c> for
/// everything else.
/// </para>
/// <para>
/// <see cref="ServiceProfile.AffectsOutputs"/> is <c>false</c>: the
/// executor's runtime identity adds no caching information beyond what
/// the step's <c>CodeVersion</c> already fingerprints (interpreter
/// version, source, lockfile), so declaring it must not uncacheabilise
/// Python steps.
/// </para>
/// <para>
/// Capacity is read from the resolved executor rather than hardcoded:
/// the conservative serial floor is the interface default, but an
/// executor that genuinely supports concurrent invocation (a process
/// pool, an RPC fan-out) raises its <see cref="IPythonExecutor.MaxConcurrency"/>
/// and the scheduler lets its steps overlap. Hardcoding <c>1</c> here
/// would pin every executor to serial, since the composite provider's
/// meet can only lower a capacity, never raise it.
/// </para>
/// </remarks>
internal sealed class PythonExecutorProfileContributor : IServiceProfileContributor
{
  private readonly IPythonExecutor _executor;

  public PythonExecutorProfileContributor(IPythonExecutor executor) =>
    _executor = executor ?? throw new ArgumentNullException(nameof(executor));

  /// <inheritdoc/>
  public ServiceProfile? Contribute(ServiceDependency dependency) =>
    dependency is ServiceDependency.CSharp cs && cs.ServiceType == typeof(IPythonExecutor)
      ? new ServiceProfile
        {
          Capacity = _executor.MaxConcurrency,
          AffectsOutputs = false,
        }
      : null;
}
