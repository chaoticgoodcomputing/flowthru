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
/// Diagnostic codes live in the FT30xx range:
/// <list type="bullet">
///   <item>FT3007 — schema count mismatch</item>
///   <item>FT3008 — schema name mismatch at a positional index</item>
///   <item>FT3009 — function arity mismatch</item>
///   <item>FT3010 — service inspection failed</item>
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
    public override string DiagnosticCode => "FT3007";
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
    public override string DiagnosticCode => "FT3008";
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
    public override string DiagnosticCode => "FT3009";
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
    public override string DiagnosticCode => "FT3010";
  }
}
