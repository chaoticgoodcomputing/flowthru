using System.Diagnostics;

namespace Flowthru.Step.Python;

/// <summary>
/// Default <see cref="IPythonLauncher"/> — spawns
/// <c>[pyExe] [workerScript]</c> with no wrapper, replicating the
/// behaviour <see cref="Internal.SubprocessPythonExecutor"/> shipped
/// with before the launcher seam landed. Registered as the
/// <c>TryAddSingleton&lt;IPythonLauncher&gt;</c> default by
/// <c>UsePython()</c>, so every existing call site is unaffected.
/// </summary>
/// <remarks>
/// <para>
/// Direct launch carries no Python-side package requirements beyond
/// what the base Python extension already declares, no distributed-
/// coordination preconditions, and no launcher-specific identity
/// folding (the default <see cref="IPythonLauncher.Identity"/>
/// suffices). All three optional members on
/// <see cref="IPythonLauncher"/> use their default implementations.
/// </para>
/// </remarks>
public sealed class DirectPythonLauncher : IPythonLauncher
{
  /// <inheritdoc/>
  public ProcessStartInfo Build(
    string pyExe,
    string workerScript,
    IReadOnlyDictionary<string, string> envVars
  )
  {
    var psi = new ProcessStartInfo
    {
      FileName = pyExe,
      UseShellExecute = false,
      RedirectStandardInput = true,
      RedirectStandardOutput = true,
      // stderr carries Python `logging` records (as JSON frames the
      // worker emits via _FlowthruJsonLogHandler) plus raw print()
      // output. SubprocessPythonExecutor.ReadStderrLoopAsync forwards
      // each line to the engine's ILogger via StderrLineClassifier.
      RedirectStandardError = true,
      CreateNoWindow = true,
    };
    psi.ArgumentList.Add(workerScript);

    foreach (var (key, value) in envVars)
    {
      psi.EnvironmentVariables[key] = value;
    }

    return psi;
  }
}
