using Flowthru.Validation.Runtime;

namespace Flowthru.Validation.Runtime.DuckDb;

/// <summary>
/// Closed sum of every typed runtime failure mode the DuckDB extension
/// can surface. Wraps into Core's
/// <see cref="RuntimeError.ExtensionError"/> via the
/// <see cref="IExtensionRuntimeError"/> contract — consumers that want
/// DuckDB-aware diagnostics pattern-match on
/// <c>case RuntimeError.ExtensionError(DuckDbRuntimeError ext) =&gt; ...</c>;
/// consumers that don't care still get
/// <see cref="IExtensionRuntimeError.Message"/> through the standard
/// pipeline.
/// </summary>
/// <remarks>
/// <para>
/// A result-schema mismatch is <em>not</em> a case here — it surfaces
/// as Core's <see cref="RuntimeError.SchemaMismatch"/>, the same typed
/// variant every format adapter uses, so schema failures pattern-match
/// uniformly whether they came from a file read or an engine transform.
/// </para>
/// <para>
/// Diagnostic codes live in the FTDDB40xx range:
/// <list type="bullet">
///   <item>FTDDB4001 — remote-bytes-unsupported (input/output located behind a remote URI)</item>
///   <item>FTDDB4002 — engine-failed (DuckDB rejected or aborted the transform)</item>
/// </list>
/// </para>
/// </remarks>
public abstract record DuckDbRuntimeError : IExtensionRuntimeError
{
  private DuckDbRuntimeError() { }

  /// <inheritdoc/>
  public abstract string Message { get; }

  /// <inheritdoc/>
  public string Category => "duckdb";

  /// <inheritdoc/>
  public abstract string DiagnosticCode { get; }

  /// <summary>
  /// An endpoint's bytes live behind a remote URI (e.g.
  /// <c>s3://...</c>), which the DuckDB transform can't reach yet —
  /// only local files are supported. Point the item at local storage,
  /// or stage the object locally before the transform.
  /// </summary>
  public sealed record RemoteBytesUnsupported(string ItemLabel, Uri Uri) : DuckDbRuntimeError
  {
    /// <inheritdoc/>
    public override string Message =>
      $"Item '{ItemLabel}' locates its bytes at remote URI '{Uri}', but the DuckDB "
      + "transform currently reads and writes local files only. Back the item with "
      + "local storage, or stage the object to a local path before this step.";
    /// <inheritdoc/>
    public override string DiagnosticCode => "FTDDB4001";
  }

  /// <summary>
  /// DuckDB rejected or aborted the transform — a SQL syntax or binder
  /// error, an unreadable input file, an out-of-disk spill, etc. The
  /// <see cref="Detail"/> carries DuckDB's own message, which names the
  /// offending clause or file.
  /// </summary>
  public sealed record EngineFailed(string StepLabel, string Detail) : DuckDbRuntimeError
  {
    /// <inheritdoc/>
    public override string Message =>
      $"DuckDB transform '{StepLabel}' failed inside the engine: {Detail}";
    /// <inheritdoc/>
    public override string DiagnosticCode => "FTDDB4002";
  }
}
