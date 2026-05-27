using System.Diagnostics;
using Flowthru.Prelude;
using Flowthru.Validation.PreFlight;

namespace Flowthru.Step.Python;

/// <summary>
/// Strategy for constructing the <see cref="ProcessStartInfo"/> that
/// <see cref="Internal.SubprocessPythonExecutor"/> uses to spawn its
/// Python worker. The default <see cref="DirectPythonLauncher"/>
/// preserves Flowthru's historical behaviour
/// (<c>[python, flowthru_worker.py]</c>); alternative launchers — e.g.
/// <c>TorchrunLauncher</c>, <c>AccelerateLauncher</c> shipped in later
/// slices — substitute distributed-training entry points without
/// forking the executor.
/// </summary>
/// <remarks>
/// <para>
/// Per-step launcher selection falls out of per-step executor
/// selection (already a parameter on every <c>AddPythonStep</c>
/// overload). A flow that wants both single-process and distributed
/// Python steps constructs two executor instances — one with
/// <see cref="DirectPythonLauncher"/>, one with a distributed launcher
/// — and threads each to the appropriate step. The launcher is *not*
/// a parameter on <c>AddPythonStep</c>: it is a
/// <see cref="Internal.SubprocessPythonExecutor"/>-specific concern,
/// and alternative <see cref="IPythonExecutor"/> implementations
/// (Python.NET in-process, gRPC, etc.) have no launcher concept at
/// all.
/// </para>
/// <para>
/// The full interface ships in Slice 1 even though only
/// <see cref="Build"/> has a consumer today. <see cref="Identity"/>
/// folds into <c>PythonCodeVersion.Derive</c> in a later slice;
/// <see cref="Probe"/> participates in the pre-flight requirements
/// algebra; <see cref="Requirements"/> feeds the same algebra.
/// Defining all four members now means
/// bespoke <see cref="IPythonLauncher"/> implementations (a user's
/// in-house launcher, a community NuGet package) lock the interface
/// shape from day one — later slices add *behaviour* against existing
/// surface rather than breaking the contract.
/// </para>
/// </remarks>
public interface IPythonLauncher
{
  /// <summary>
  /// Construct the <see cref="ProcessStartInfo"/> the executor will
  /// pass to <see cref="Process.Start(ProcessStartInfo)"/>. The
  /// returned PSI must redirect stdin/stdout (the JSON-over-stdio
  /// protocol depends on both) and stderr (logging bridge).
  /// </summary>
  /// <param name="pyExe">
  /// Absolute path to the venv's Python interpreter, as resolved by
  /// <c>PythonEnvironmentResolver.ResolvePythonExe</c>.
  /// </param>
  /// <param name="workerScript">
  /// Absolute path to <c>flowthru_worker.py</c>, located at
  /// <see cref="System.AppContext.BaseDirectory"/>.
  /// </param>
  /// <param name="envVars">
  /// Configuration-derived environment variables produced by
  /// <c>IPythonConfigurationFlattener</c>. The launcher owns the
  /// final merge order: launcher-set variables (e.g. <c>RANK</c>,
  /// <c>WORLD_SIZE</c> set by <c>torchrun</c>) overlay on top of
  /// these so distributed-training launchers can compose with the
  /// existing IConfiguration→env-var bridge without losing either.
  /// </param>
  ProcessStartInfo Build(
    string pyExe,
    string workerScript,
    IReadOnlyDictionary<string, string> envVars
  );

  /// <summary>
  /// Stable launcher identity string folded into a step's
  /// <c>CodeVersion</c> so a launcher change invalidates cached
  /// results. Distributed-training launchers produce
  /// non-bitwise-reproducible outputs versus single-process runs;
  /// treating them as cache-equivalent would be wrong. Default:
  /// the implementation's full type name.
  /// </summary>
  string Identity => GetType().FullName ?? GetType().Name;

  /// <summary>
  /// Pre-flight probe — fail fast on launcher misconfiguration before
  /// any Step's logic runs. This is what distinguishes per-launcher
  /// classes from a generic process launcher: domain-
  /// specific checks (GPU count vs <c>nproc_per_node</c>, framework
  /// config validity, NCCL availability) that a generic launcher
  /// could not express. Default: no-op
  /// (<see cref="Validated{TError,TValue}.Pure"/>) — appropriate for
  /// launchers like <see cref="DirectPythonLauncher"/> whose only
  /// preconditions are also Python's preconditions.
  /// </summary>
  Validated<PreFlightError, FlowUnit> Probe() =>
    Validated<PreFlightError, FlowUnit>.Pure(FlowUnit.Default);

  /// <summary>
  /// Python-side packages this launcher requires in the venv (e.g.
  /// <c>accelerate</c> for <c>AccelerateLauncher</c>). Aggregated
  /// with every other declared
  /// <see cref="PythonPackageRequirement"/> across the live DI
  /// container and enforced by the requirements algebra.
  /// Default: empty.
  /// </summary>
  IReadOnlyList<PythonPackageRequirement> Requirements =>
    Array.Empty<PythonPackageRequirement>();
}
