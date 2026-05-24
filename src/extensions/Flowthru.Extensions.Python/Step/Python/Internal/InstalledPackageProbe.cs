using System.Collections.Immutable;
using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Extensions.Options;

namespace Flowthru.Step.Python.Internal;

/// <summary>
/// Default <see cref="IInstalledPackageProbe"/> — one-shot subprocess
/// that lists the venv's installed Python packages via
/// <c>python -m pip list --format=json</c>. Mirrors the short-lived
/// <c>--version</c> probe in <see cref="SubprocessPythonExecutor"/> —
/// the result feeds the requirements algebra's pre-flight check
/// (ADR-0013) so the user sees missing or wrong-version Python deps
/// before any Step's logic runs.
/// </summary>
internal sealed class SubprocessInstalledPackageProbe : IInstalledPackageProbe
{
  private static readonly TimeSpan _defaultTimeout = TimeSpan.FromSeconds(30);

  private readonly PythonRuntimeOptions _options;
  private readonly TimeSpan _timeout;

  public SubprocessInstalledPackageProbe(IOptions<PythonRuntimeOptions> options)
    : this(options, _defaultTimeout) { }

  // Internal seam for tests that want to tighten the timeout.
  internal SubprocessInstalledPackageProbe(IOptions<PythonRuntimeOptions> options, TimeSpan timeout)
  {
    _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
    _timeout = timeout;
  }

  /// <inheritdoc/>
  public ImmutableDictionary<string, string>? TryProbe()
  {
    string pyExe;
    try
    {
      pyExe = PythonEnvironmentResolver.ResolvePythonExe(_options);
    }
    catch
    {
      return null;
    }

    try
    {
      var psi = new ProcessStartInfo
      {
        FileName = pyExe,
        UseShellExecute = false,
        RedirectStandardOutput = true,
        RedirectStandardError = true,
        CreateNoWindow = true,
      };
      psi.ArgumentList.Add("-m");
      psi.ArgumentList.Add("pip");
      psi.ArgumentList.Add("list");
      psi.ArgumentList.Add("--format=json");
      psi.ArgumentList.Add("--disable-pip-version-check");

      using var proc = Process.Start(psi);
      if (proc is null) return null;

      if (!proc.WaitForExit(_timeout))
      {
        try { proc.Kill(entireProcessTree: true); }
        catch { /* best-effort */ }
        return null;
      }
      if (proc.ExitCode != 0) return null;

      var stdout = proc.StandardOutput.ReadToEnd();
      if (string.IsNullOrWhiteSpace(stdout)) return ImmutableDictionary<string, string>.Empty;

      // pip list --format=json emits an array of {"name": "...",
      // "version": "..."} objects. Tolerate occasional non-array shapes
      // (some pip versions emit a wrapper) by inspecting the parsed
      // JsonNode rather than binding to a fixed type.
      var node = JsonNode.Parse(stdout);
      var array = node as JsonArray ?? (node?["packages"] as JsonArray);
      if (array is null) return null;

      var builder = ImmutableDictionary.CreateBuilder<string, string>(StringComparer.OrdinalIgnoreCase);
      foreach (var entry in array)
      {
        var name = entry?["name"]?.GetValue<string>();
        var version = entry?["version"]?.GetValue<string>();
        if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(version)) continue;
        // Last write wins — pip should not emit duplicates but we
        // don't want a duplicate to crash the probe.
        builder[name] = version;
      }

      return builder.ToImmutable();
    }
    catch (JsonException)
    {
      return null;
    }
    catch
    {
      return null;
    }
  }
}
