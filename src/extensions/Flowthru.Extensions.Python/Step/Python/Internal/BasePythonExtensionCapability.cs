namespace Flowthru.Step.Python.Internal;

/// <summary>
/// The Python extension's own Python-side floor — the packages
/// <c>SubprocessPythonExecutor</c>'s worker assumes are available in
/// the user's venv. Registered as the first
/// <see cref="IPythonCapability"/> by <c>UsePython()</c>; folded with
/// every other declarer (launchers, future service-registration
/// requirements) by
/// <see cref="Flowthru.Validation.PreFlight.Python.PythonRequirementsValidationHook"/>
/// per ADR-0013.
/// </summary>
/// <remarks>
/// <para>
/// Two declarations in v1:
/// <list type="bullet">
///   <item>
///     <c>pyarrow</c> — Arrow IPC marshaller. Floor chosen to cover
///     the schema-encoding paths the worker actually uses; specific
///     pin can be tightened later if a real version-skew bug surfaces.
///   </item>
///   <item>
///     <c>flowthru</c> — the Python companion package. No version
///     pin in this slice; later work folds the .NET assembly version
///     in (so a stale companion fails design-time) but the API for
///     deriving that is a slice-3 concern.
///   </item>
/// </list>
/// </para>
/// <para>
/// Adjusting these declarations is a Core / Extension change — they
/// describe what the *framework* itself needs. Adjusting them per
/// project is incorrect; per-project Python deps go in
/// <c>pyproject.toml</c> as always.
/// </para>
/// </remarks>
internal sealed class BasePythonExtensionCapability : IPythonCapability
{
  private static readonly IReadOnlyList<PythonPackageRequirement> _requirements = new[]
  {
    new PythonPackageRequirement(
      Package: "pyarrow",
      VersionConstraint: ">=14",
      Reason: "Required by PythonStepExtension (Arrow IPC marshaller)"
    ),
    new PythonPackageRequirement(
      Package: "flowthru",
      VersionConstraint: null,
      Reason: "Required by PythonStepExtension (Python companion package)"
    ),
  };

  /// <inheritdoc/>
  public IReadOnlyList<PythonPackageRequirement> Requirements => _requirements;
}
