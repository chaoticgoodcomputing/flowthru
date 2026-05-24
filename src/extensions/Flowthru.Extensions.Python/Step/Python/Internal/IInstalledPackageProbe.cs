using System.Collections.Immutable;

namespace Flowthru.Step.Python.Internal;

/// <summary>
/// Abstraction over "tell me which Python packages are installed in
/// the configured venv". Default implementation
/// (<see cref="SubprocessInstalledPackageProbe"/>) shells out to
/// <c>python -m pip list --format=json</c>; the interface exists so
/// <see cref="Flowthru.Validation.PreFlight.Python.PythonRequirementsValidationHook"/>
/// can be unit-tested against a stub instead of a real venv.
/// </summary>
public interface IInstalledPackageProbe
{
  /// <summary>
  /// Probe the configured venv. Returns a case-insensitive map of
  /// <c>package name</c> → <c>installed version string</c>, or
  /// <c>null</c> when the probe could not be invoked at all (broken
  /// venv, missing pip, subprocess failure). Callers treat null as
  /// "uncheckable" rather than "no packages installed".
  /// </summary>
  ImmutableDictionary<string, string>? TryProbe();
}
