namespace Flowthru.Step.DuckDb;

/// <summary>
/// Host-level configuration for the embedded DuckDB engine. Bound from
/// the <c>Flowthru:DuckDb</c> configuration section by <c>UseDuckDb()</c>,
/// or set code-first via <c>UseDuckDb(opts =&gt; ...)</c>.
/// </summary>
/// <remarks>
/// <para>
/// A DuckDB transform can use up to <see cref="MemoryLimit"/> of RAM
/// (spilling to <see cref="TempDirectory"/> beyond it), so
/// <see cref="MaxConcurrentTransforms"/> is the knob that keeps a
/// flow's peak engine memory at
/// <c>MaxConcurrentTransforms × MemoryLimit</c>. The default of
/// <c>1</c> is the conservative floor: one engine transform at a time,
/// each free to use the full budget. Raise it on hosts with memory to
/// spare and flows with independent transforms.
/// </para>
/// </remarks>
public sealed class DuckDbEngineOptions
{
  /// <summary>
  /// Maximum number of DuckDB transforms the scheduler may run
  /// concurrently. Default <c>1</c> — engine transforms serialize, so
  /// each gets the full memory/disk budget. Must be ≥ 1.
  /// </summary>
  public int MaxConcurrentTransforms { get; set; } = 1;

  /// <summary>
  /// Memory ceiling per transform, in DuckDB's size syntax (e.g.
  /// <c>"4GB"</c>, <c>"512MB"</c>). <c>null</c> uses DuckDB's default
  /// (80% of available RAM). Work beyond the ceiling spills to
  /// <see cref="TempDirectory"/> instead of failing.
  /// </summary>
  public string? MemoryLimit { get; set; }

  /// <summary>
  /// Number of threads DuckDB may use per transform. <c>null</c> uses
  /// DuckDB's default (all cores).
  /// </summary>
  public int? Threads { get; set; }

  /// <summary>
  /// Directory for DuckDB's larger-than-memory spill files. <c>null</c>
  /// uses DuckDB's default alongside the (in-memory) database.
  /// </summary>
  public string? TempDirectory { get; set; }

  /// <summary>
  /// Directory DuckDB looks in for (and installs) loadable engine
  /// extensions — applied as DuckDB's <c>extension_directory</c> setting.
  /// <c>null</c> uses DuckDB's default (<c>~/.duckdb/extensions</c>).
  /// </summary>
  /// <remarks>
  /// Transforms with <c>s3://</c> endpoints need the <c>httpfs</c>
  /// extension, which the bundled engine does <em>not</em> statically
  /// link. For air-gapped hosts, pre-provision
  /// <c>httpfs.duckdb_extension</c> into this directory (run
  /// <c>INSTALL httpfs</c> once on a networked machine with the same
  /// DuckDB version/platform, or bake it into the container image) and
  /// set <see cref="AllowExtensionDownload"/> to <c>false</c>.
  /// </remarks>
  public string? ExtensionDirectory { get; set; }

  /// <summary>
  /// Whether the engine may run <c>INSTALL httpfs</c> — a one-time
  /// network download from DuckDB's extension repository — when a
  /// transform needs <c>httpfs</c> (an <c>s3://</c> endpoint) and the
  /// extension isn't already present in the extension directory.
  /// Default <c>true</c>, matching DuckDB's own autoinstall posture.
  /// </summary>
  /// <remarks>
  /// Set <c>false</c> on hosts that must never reach the network at
  /// transform time. With the flag off, a missing <c>httpfs</c> fails
  /// the step with the typed <c>FTDDB4003</c> error (never a silent
  /// download), and DuckDB's own <c>autoinstall_known_extensions</c> is
  /// disabled for the transform's connection so nothing downloads
  /// implicitly either. Purely local transforms never touch
  /// <c>httpfs</c> and are unaffected.
  /// </remarks>
  public bool AllowExtensionDownload { get; set; } = true;
}
