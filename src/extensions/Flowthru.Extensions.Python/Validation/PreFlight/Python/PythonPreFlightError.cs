using Flowthru.Validation.PreFlight;

namespace Flowthru.Validation.PreFlight.Python;

/// <summary>
/// Discriminator for which side of the step contract — input or output —
/// a schema-shape failure occurred on. Closed via the <c>enum</c>
/// boundary; consumers exhaustively switch.
/// </summary>
public enum PythonSchemaSide
{
  /// <summary>Failure relates to the step's declared inputs.</summary>
  Input,

  /// <summary>Failure relates to the step's declared outputs.</summary>
  Output,
}

/// <summary>
/// Closed sum of every typed pre-flight failure mode the Python
/// extension's flow validator can surface. Wraps into Core's
/// <see cref="PreFlightError.External"/> via the
/// <see cref="IExtensionPreFlightError"/> contract.
/// </summary>
/// <remarks>
/// Diagnostic codes live in the FTPY30xx range:
/// <list type="bullet">
///   <item>FTPY3007 — schema count mismatch</item>
///   <item>FTPY3008 — schema name mismatch at a positional index</item>
///   <item>FTPY3009 — function arity mismatch</item>
///   <item>FTPY3010 — service inspection failed</item>
///   <item>FTPY3011 — Python package missing from venv</item>
///   <item>FTPY3012 — installed Python package version doesn't satisfy declared constraints</item>
/// </list>
/// </remarks>
public abstract record PythonPreFlightError : IExtensionPreFlightError
{
  private PythonPreFlightError() { }

  /// <inheritdoc/>
  public abstract string Message { get; }

  /// <inheritdoc/>
  public string Category => "python";

  /// <inheritdoc/>
  public abstract string DiagnosticCode { get; }

  /// <summary>
  /// The number of schemas declared in the <c>@step</c> decorator does
  /// not match the C# generic-parameter count.
  /// </summary>
  public sealed record SchemaCountMismatch(
    string StepLabel,
    PythonSchemaSide Side,
    int Expected,
    int Actual
  ) : PythonPreFlightError
  {
    /// <inheritdoc/>
    public override string Message =>
      $"Python step '{StepLabel}' {Side.ToString().ToLowerInvariant()} schema count: "
        + $"C# expects {Expected}, decorator declares {Actual}.";
    /// <inheritdoc/>
    public override string DiagnosticCode => "FTPY3007";
  }

  /// <summary>
  /// At positional index <see cref="Position"/>, the decorator-declared
  /// schema name does not match the C# generic type's name.
  /// </summary>
  public sealed record SchemaNameMismatch(
    string StepLabel,
    PythonSchemaSide Side,
    int Position,
    string ExpectedName,
    string ActualName
  ) : PythonPreFlightError
  {
    /// <inheritdoc/>
    public override string Message =>
      $"Python step '{StepLabel}' {Side.ToString().ToLowerInvariant()} schema mismatch at "
        + $"position {Position + 1}: C# declares '{ExpectedName}', "
        + $"decorator declares '{ActualName}'.";
    /// <inheritdoc/>
    public override string DiagnosticCode => "FTPY3008";
  }

  /// <summary>
  /// The Python function's parameter count does not match the C#
  /// declared input count.
  /// </summary>
  public sealed record ArityMismatch(
    string StepLabel,
    string Module,
    string Function,
    int Expected,
    int Actual
  ) : PythonPreFlightError
  {
    /// <inheritdoc/>
    public override string Message =>
      $"Python function '{Module}.{Function}' has {Actual} parameter(s), "
        + $"but step '{StepLabel}' is registered with {Expected} input(s).";
    /// <inheritdoc/>
    public override string DiagnosticCode => "FTPY3009";
  }

  /// <summary>
  /// A Python service's sidecar inspector reported a failure during
  /// pre-flight.
  /// </summary>
  public sealed record ServiceInspectionFailed(
    string ServiceClassPath,
    string Detail
  ) : PythonPreFlightError
  {
    /// <inheritdoc/>
    public override string Message =>
      $"Python service '{ServiceClassPath}' inspection failed: {Detail}";
    /// <inheritdoc/>
    public override string DiagnosticCode => "FTPY3010";
  }

  /// <summary>
  /// A capability declared a Python-side package requirement (via
  /// <see cref="Step.Python.IPythonCapability.Requirements"/> or
  /// <see cref="Step.Python.IPythonLauncher.Requirements"/>) but the
  /// package is absent from the configured venv. Surfaced by the
  /// requirements algebra (ADR-0013) during pre-flight.
  /// </summary>
  /// <param name="Package">PyPI package name.</param>
  /// <param name="RequiredConstraint">
  /// Folded constraint string the declarers collectively asked for.
  /// </param>
  /// <param name="Declarers">
  /// Human-readable list of the capabilities that declared the
  /// requirement — used so the user knows what's asking and can
  /// resolve the gap with a single <c>uv add</c>.
  /// </param>
  public sealed record MissingRequirement(
    string Package,
    string RequiredConstraint,
    IReadOnlyList<string> Declarers
  ) : PythonPreFlightError
  {
    /// <inheritdoc/>
    public override string Message
    {
      get
      {
        var declarerList = Declarers.Count == 0
          ? "(no declarer attribution)"
          : string.Join("; ", Declarers);
        var constraint = string.IsNullOrWhiteSpace(RequiredConstraint) ? "*" : RequiredConstraint;
        return
          $"Python package '{Package}' (constraint: {constraint}) is required but not installed "
          + $"in the configured venv. Declared by: {declarerList}. "
          + $"Run `uv add {Package}{(constraint == "*" ? "" : constraint)}` to resolve.";
      }
    }

    /// <inheritdoc/>
    public override string DiagnosticCode => "FTPY3011";
  }

  /// <summary>
  /// A capability-declared Python-side package is present in the venv
  /// but the installed version does not satisfy the folded constraint.
  /// Per ADR-0013, this single variant covers both
  /// "installed-version-too-old" and conflicting-declarer cases —
  /// users see every contributing capability so internal
  /// inconsistencies are diagnosable from the error itself. Symbolic
  /// "unsatisfiable intersection" detection (a sharper diagnostic) is
  /// deferred to the design-time analyzer (slice 3).
  /// </summary>
  public sealed record VersionConstraintNotSatisfied(
    string Package,
    string InstalledVersion,
    string RequiredConstraint,
    IReadOnlyList<string> Declarers
  ) : PythonPreFlightError
  {
    /// <inheritdoc/>
    public override string Message
    {
      get
      {
        var declarerList = Declarers.Count == 0
          ? "(no declarer attribution)"
          : string.Join("; ", Declarers);
        return
          $"Python package '{Package}' is installed at version '{InstalledVersion}' "
          + $"but the folded constraint '{RequiredConstraint}' is not satisfied. "
          + $"Declared by: {declarerList}.";
      }
    }

    /// <inheritdoc/>
    public override string DiagnosticCode => "FTPY3012";
  }
}
