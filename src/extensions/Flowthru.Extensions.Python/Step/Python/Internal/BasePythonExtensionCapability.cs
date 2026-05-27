namespace Flowthru.Step.Python.Internal;

/// <summary>
/// The Python extension's own Python-side floor — the packages
/// <c>SubprocessPythonExecutor</c>'s worker assumes are available in
/// the user's venv. Registered as the first
/// <see cref="IPythonCapability"/> by <c>UsePython()</c>; folded with
/// every other declarer (launchers, future service-registration
/// requirements) by
/// <see cref="Flowthru.Validation.PreFlight.Python.PythonRequirementsValidationHook"/>
/// per the requirements algebra.
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
// Attributes are the *single* source of truth: the
// PythonRequirementsAnalyzer reads them statically via Roslyn metadata
// (FTPY1501 / FTPY1502 against uv.lock), and the runtime Requirements
// property derives from the same attributes via reflection. No
// hardcoded duplicate list means no drift possible.
// pyarrow is the only base requirement that's a genuine pip-installed
// package. The `flowthru` Python companion is shipped by the .NET
// extension as a sys.path-resolvable directory under the project's
// output (see Flowthru.Extensions.Python.targets); declaring it here
// would surface false positives — `pip list` / `uv.lock` doesn't
// know about it because it's not pip-managed.
[PythonPackageRequirement(
  package: "pyarrow",
  versionConstraint: ">=14",
  reason: "Required by PythonStepExtension (Arrow IPC marshaller)"
)]
internal sealed class BasePythonExtensionCapability : IPythonCapability
{
  private static readonly IReadOnlyList<PythonPackageRequirement> _requirements =
    typeof(BasePythonExtensionCapability)
      .GetCustomAttributes(typeof(PythonPackageRequirementAttribute), inherit: false)
      .Cast<PythonPackageRequirementAttribute>()
      .Select(a => new PythonPackageRequirement(a.Package, a.VersionConstraint, a.Reason))
      .ToList();

  /// <inheritdoc/>
  public IReadOnlyList<PythonPackageRequirement> Requirements => _requirements;
}
