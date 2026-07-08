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
}
