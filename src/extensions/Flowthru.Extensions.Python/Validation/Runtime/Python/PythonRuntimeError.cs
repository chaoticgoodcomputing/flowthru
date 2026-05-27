using Flowthru.Validation.Runtime;

namespace Flowthru.Validation.Runtime.Python;

/// <summary>
/// Closed sum of every typed runtime failure mode the Python extension
/// can surface. Wraps into Core's
/// <see cref="RuntimeError.ExtensionError"/> via the
/// <see cref="IExtensionRuntimeError"/> contract — consumers that want
/// Python-aware diagnostics pattern-match on
/// <c>case RuntimeError.ExtensionError(PythonRuntimeError ext) =&gt; ...</c>;
/// consumers that don't care still get
/// <see cref="IExtensionRuntimeError.Message"/> through the standard
/// pipeline.
/// </summary>
/// <remarks>
/// Diagnostic codes live in the FTPY40xx range:
/// <list type="bullet">
///   <item>FTPY4007 — module-not-found</item>
///   <item>FTPY4008 — function-missing</item>
///   <item>FTPY4009 — decorator-absent</item>
///   <item>FTPY4010 — worker-error (Python exception inside step body)</item>
///   <item>FTPY4011 — marshalling-failed</item>
///   <item>FTPY4012 — worker-crashed (subprocess died / pipe broken)</item>
/// </list>
/// </remarks>
public abstract record PythonRuntimeError : IExtensionRuntimeError
{
  private PythonRuntimeError() { }

  /// <inheritdoc/>
  public abstract string Message { get; }

  /// <inheritdoc/>
  public string Category => "python";

  /// <inheritdoc/>
  public abstract string DiagnosticCode { get; }

  /// <summary>
  /// Python module could not be imported — bad sys.path, syntax error
  /// inside the module, missing dependency, etc.
  /// </summary>
  public sealed record ModuleNotFound(string Module, string Detail) : PythonRuntimeError
  {
    /// <inheritdoc/>
    public override string Message =>
      $"Python module '{Module}' could not be imported: {Detail}";
    /// <inheritdoc/>
    public override string DiagnosticCode => "FTPY4007";
  }

  /// <summary>
  /// The named function does not exist in the (otherwise importable) module.
  /// </summary>
  public sealed record FunctionMissing(string Module, string Function) : PythonRuntimeError
  {
    /// <inheritdoc/>
    public override string Message =>
      $"Python function '{Function}' not found in module '{Module}'.";
    /// <inheritdoc/>
    public override string DiagnosticCode => "FTPY4008";
  }

  /// <summary>
  /// The function exists but is not decorated with <c>@step</c> — Flowthru
  /// requires every callable Python step to advertise its contract via the
  /// decorator's <c>__flowthru_inputs__</c> / <c>__flowthru_outputs__</c>
  /// attributes.
  /// </summary>
  public sealed record DecoratorAbsent(string Module, string Function) : PythonRuntimeError
  {
    /// <inheritdoc/>
    public override string Message =>
      $"Python function '{Module}.{Function}' is missing the @step decorator. "
        + "Decorate the function with @flowthru.step(inputs=[...], outputs=[...]) "
        + "to make it callable from a Flowthru pipeline.";
    /// <inheritdoc/>
    public override string DiagnosticCode => "FTPY4009";
  }

  /// <summary>
  /// The Python function raised an exception during step execution (the
  /// step's transform body itself failed — KeyError, ValueError, etc.).
  /// </summary>
  public sealed record WorkerError(string Module, string Function, string PythonMessage)
    : PythonRuntimeError
  {
    /// <inheritdoc/>
    public override string Message =>
      $"Python step '{Module}.{Function}' raised: {PythonMessage}";
    /// <inheritdoc/>
    public override string DiagnosticCode => "FTPY4010";
  }

  /// <summary>
  /// Wire-format conversion failed — Arrow encode/decode mismatch, scalar
  /// type unsupported, dtype coercion overflow, etc.
  /// </summary>
  public sealed record MarshallingFailed(string Source, string Detail) : PythonRuntimeError
  {
    /// <inheritdoc/>
    public override string Message =>
      $"Python marshalling failure in {Source}: {Detail}";
    /// <inheritdoc/>
    public override string DiagnosticCode => "FTPY4011";
  }

  /// <summary>
  /// The subprocess Python worker crashed or its IPC pipe broke.
  /// Distinct from <see cref="WorkerError"/>: this is a transport-level
  /// failure, not a user-code error.
  /// </summary>
  public sealed record WorkerCrashed(string Detail) : PythonRuntimeError
  {
    /// <inheritdoc/>
    public override string Message =>
      $"Python subprocess worker crashed: {Detail}";
    /// <inheritdoc/>
    public override string DiagnosticCode => "FTPY4012";
  }
}
