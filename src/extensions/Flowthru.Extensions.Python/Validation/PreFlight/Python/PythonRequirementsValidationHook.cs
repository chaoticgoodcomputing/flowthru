using Flowthru.Flow;
using Flowthru.Prelude;
using Flowthru.Step.Python;
using Flowthru.Step.Python.Internal;

namespace Flowthru.Validation.PreFlight.Python;

/// <summary>
/// Pre-flight enforcement of the Python requirements algebra
/// (ADR-0013). Collects every <see cref="IPythonCapability"/>
/// registered in the DI container plus the active
/// <see cref="IPythonLauncher"/>'s declared requirements, folds them
/// into per-package effective constraints, probes the configured venv
/// for installed packages, and emits typed
/// <see cref="PythonPreFlightError"/>s for missing packages or
/// version-constraint failures.
/// </summary>
/// <remarks>
/// <para>
/// One subprocess invocation total: <c>python -m pip list
/// --format=json</c> against the configured venv. The result is
/// folded against every package-level requirement in one pass; the
/// hook accumulates failures rather than short-circuiting so the user
/// sees every missing or wrong-version dep at once.
/// </para>
/// <para>
/// When the probe cannot run (venv missing / broken interpreter / pip
/// not present), the hook reports a single
/// <see cref="PythonPreFlightError.ServiceInspectionFailed"/>
/// describing the gap rather than emitting per-requirement failures
/// — the underlying environment is broken in a way that precedes any
/// requirement check.
/// </para>
/// <para>
/// Symbolic conflict detection ("these declarers' constraints can
/// never coexist") is deferred to the design-time analyzer in slice 3.
/// In this slice an unsatisfiable intersection just surfaces as a
/// <see cref="PythonPreFlightError.VersionConstraintNotSatisfied"/>
/// whose constraint string includes every contributing clause, so
/// the user can spot the conflict from the message.
/// </para>
/// </remarks>
public sealed class PythonRequirementsValidationHook : IFlowValidationHook
{
  private readonly IInstalledPackageProbe _probe;
  private readonly IEnumerable<IPythonCapability> _capabilities;
  private readonly IPythonLauncher _launcher;

  /// <summary>
  /// Construct the hook with the capability sources it folds plus the
  /// probe that lists installed packages. All three dependencies
  /// resolve from the standard <c>UsePython()</c> DI registrations;
  /// tests substitute a stub probe to avoid spawning a real
  /// subprocess.
  /// </summary>
  public PythonRequirementsValidationHook(
    IInstalledPackageProbe probe,
    IEnumerable<IPythonCapability> capabilities,
    IPythonLauncher launcher
  )
  {
    _probe = probe ?? throw new ArgumentNullException(nameof(probe));
    _capabilities = capabilities ?? throw new ArgumentNullException(nameof(capabilities));
    _launcher = launcher ?? throw new ArgumentNullException(nameof(launcher));
  }

  /// <inheritdoc/>
  public string HookId => "python.requirements";

  /// <inheritdoc/>
  public FlowIO<Validated<PreFlightError, FlowUnit>> Validate(BuiltFlow flow)
  {
    if (flow is null) throw new ArgumentNullException(nameof(flow));

    return FlowIO.LiftAsync<Validated<PreFlightError, FlowUnit>>(
      ct => Task.Run(() => ValidateCore(), ct),
      source: HookId
    );
  }

  private Validated<PreFlightError, FlowUnit> ValidateCore()
  {
    var allRequirements = _capabilities
      .SelectMany(c => c.Requirements ?? Array.Empty<PythonPackageRequirement>())
      .Concat(_launcher.Requirements ?? Array.Empty<PythonPackageRequirement>())
      .ToList();

    if (allRequirements.Count == 0)
    {
      // No capability declares anything — the algebra has nothing to
      // enforce. Common in tests with bare DirectPythonLauncher and
      // no base capability registered.
      return Validated<PreFlightError, FlowUnit>.Pure(FlowUnit.Default);
    }

    var folded = PythonRequirementsAlgebra.Fold(allRequirements);

    var installed = _probe.TryProbe();
    if (installed is null)
    {
      // Venv unreachable — surface as a single inspection failure
      // rather than fan out per-requirement failures. The user
      // resolves by fixing the venv, not by chasing 13 separate
      // missing-package diagnostics.
      return Validated<PreFlightError, FlowUnit>.Fail(
        new PreFlightError.External(new PythonPreFlightError.ServiceInspectionFailed(
          ServiceClassPath: "(venv)",
          Detail: "Could not probe installed packages via `python -m pip list --format=json`. "
            + "Check that the configured venv path is valid and `pip` is installed."
        ))
      );
    }

    var failures = new List<PreFlightError>();
    foreach (var req in folded)
    {
      var declarers = req.Declarers.Select(d => d.ToString()).ToList();

      if (!installed.TryGetValue(req.Package, out var installedRaw))
      {
        failures.Add(new PreFlightError.External(new PythonPreFlightError.MissingRequirement(
          Package: req.Package,
          RequiredConstraint: req.Constraint.ToString(),
          Declarers: declarers
        )));
        continue;
      }

      // Empty constraint always satisfies — the declarer asked for
      // "any version", and we found a version, so we're done.
      if (req.Constraint.Clauses.Length == 0) continue;

      if (!PythonVersion.TryParse(installedRaw, out var installedVersion))
      {
        // pip reported a version our parser can't represent (likely
        // an epoch / local / post / dev tail). Be lenient: don't
        // fail validation just because we can't model the exact
        // installed string — the requirement may well be satisfied
        // by what pip considers a compatible version. Slice 3
        // tightens this by delegating to packaging via subprocess.
        continue;
      }

      if (!req.Constraint.Satisfies(installedVersion))
      {
        failures.Add(new PreFlightError.External(
          new PythonPreFlightError.VersionConstraintNotSatisfied(
            Package: req.Package,
            InstalledVersion: installedRaw,
            RequiredConstraint: req.Constraint.ToString(),
            Declarers: declarers
          )
        ));
      }
    }

    return failures.Count == 0
      ? Validated<PreFlightError, FlowUnit>.Pure(FlowUnit.Default)
      : Validated<PreFlightError, FlowUnit>.Fail(failures);
  }
}
