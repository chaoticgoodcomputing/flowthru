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
///   <item>FTDDB4001 — remote-bytes-unsupported (endpoint behind a remote URI whose scheme the engine can't reach)</item>
///   <item>FTDDB4002 — engine-failed (DuckDB rejected or aborted the transform)</item>
///   <item>FTDDB4003 — httpfs-unavailable (the httpfs extension an s3:// endpoint needs could not be loaded)</item>
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
  /// An endpoint's bytes live behind a remote URI whose scheme the
  /// DuckDB transform can't reach — it reads and writes local files and
  /// <c>s3://</c> objects. Point the item at local or S3 storage, or
  /// stage the object before the transform.
  /// </summary>
  public sealed record RemoteBytesUnsupported(string ItemLabel, Uri Uri) : DuckDbRuntimeError
  {
    /// <inheritdoc/>
    public override string Message =>
      $"Item '{ItemLabel}' locates its bytes at remote URI '{Uri}', but the DuckDB "
      + $"transform reaches local files and s3:// objects only — '{Uri.Scheme}://' "
      + "endpoints aren't supported. Back the item with local or S3 storage, or "
      + "stage the object before this step.";
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

  /// <summary>
  /// A transform has an <c>s3://</c> endpoint, but DuckDB's
  /// <c>httpfs</c> extension — which the bundled engine does not
  /// statically link — could not be loaded. Either the extension isn't
  /// present locally and downloads are disabled
  /// (<c>DuckDbEngineOptions.AllowExtensionDownload = false</c>), or the
  /// one-time <c>INSTALL httpfs</c> download failed (typically: no
  /// network path to DuckDB's extension repository). The
  /// <see cref="Detail"/> carries DuckDB's own message plus the remedy:
  /// pre-provision <c>httpfs.duckdb_extension</c> into
  /// <c>DuckDbEngineOptions.ExtensionDirectory</c> (or DuckDB's default
  /// <c>~/.duckdb</c>), or allow the download on a networked host.
  /// </summary>
  public sealed record HttpfsUnavailable(string StepLabel, string Detail) : DuckDbRuntimeError
  {
    /// <inheritdoc/>
    public override string Message =>
      $"DuckDB transform '{StepLabel}' has an s3:// endpoint, but the httpfs "
      + $"extension could not be loaded: {Detail}";
    /// <inheritdoc/>
    public override string DiagnosticCode => "FTDDB4003";
  }
}
