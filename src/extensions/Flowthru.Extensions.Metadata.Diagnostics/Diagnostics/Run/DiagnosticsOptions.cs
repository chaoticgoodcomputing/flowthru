namespace Flowthru.Diagnostics.Run;

/// <summary>
/// Aggregated options for the four built-in diagnostic providers
/// (<see cref="StepTimingOptions"/>, <see cref="RunSummaryOptions"/>,
/// <see cref="RowCountOptions"/>, <see cref="OutputExistenceOptions"/>).
/// Used by the umbrella <c>UseDiagnostics()</c> extension; per-provider
/// extensions (<c>AddStepTimings</c>, <c>AddRunSummary</c>, etc.)
/// accept the individual option types directly.
/// </summary>
/// <remarks>
/// <para>
/// <strong>Cost discipline.</strong> By default
/// <see cref="StepTimings"/> and <see cref="RunSummary"/> are enabled
/// — both are pure post-processing of the
/// <see cref="Flowthru.Flow.FlowResult"/> the scheduler already
/// produced. <see cref="RowCounts"/> and <see cref="OutputExistence"/>
/// are disabled by default because they touch live storage; the
/// framework does not subsidise that cost.
/// </para>
/// </remarks>
public sealed class DiagnosticsOptions
{
  /// <summary>Step-timing summary (enabled by default — pure post-processing).</summary>
  public StepTimingOptions StepTimings { get; } = new();

  /// <summary>Row-count summary (disabled by default — touches live storage).</summary>
  public RowCountOptions RowCounts { get; } = new() { Enabled = false };

  /// <summary>Output-existence audit (disabled by default — touches live storage).</summary>
  public OutputExistenceOptions OutputExistence { get; } = new() { Enabled = false };

  /// <summary>Run-summary block (enabled by default — pure post-processing).</summary>
  public RunSummaryOptions RunSummary { get; } = new();
}

/// <summary>Configuration for <see cref="StepTimingProvider"/>.</summary>
public sealed class StepTimingOptions
{
  /// <summary>Whether the step-timing summary is emitted post-run. Default: true.</summary>
  public bool Enabled { get; set; } = true;

  /// <summary>
  /// Number of slowest steps to report in the summary. Default: 5.
  /// Set to 0 to skip the top-N section.
  /// </summary>
  public int TopSlowest { get; set; } = 5;

  /// <summary>
  /// Optional threshold; steps that exceed this duration are flagged
  /// at warning level. Default: null (no threshold flagging).
  /// </summary>
  public TimeSpan? SlowThreshold { get; set; }
}

/// <summary>Configuration for <see cref="RowCountProvider"/>.</summary>
public sealed class RowCountOptions
{
  /// <summary>Whether the row-count summary is emitted post-run. Default: true (when registered).</summary>
  public bool Enabled { get; set; } = true;

  /// <summary>Whether to include input items. Default: false (outputs only).</summary>
  public bool IncludeInputs { get; set; } = false;

  /// <summary>Whether to include output items. Default: true.</summary>
  public bool IncludeOutputs { get; set; } = true;

  /// <summary>
  /// When false (default), only items whose storage adapter
  /// implements <see cref="Flowthru.Data.Storage.IHasEfficientCount"/>
  /// are counted; others are reported as <c>?</c>. When true, every
  /// item is counted, which may force materialisation for
  /// non-efficient adapters. <strong>Default: false — flip only if
  /// you have measured the cost.</strong>
  /// </summary>
  public bool ForceCountAll { get; set; } = false;
}

/// <summary>Configuration for <see cref="OutputExistenceProvider"/>.</summary>
public sealed class OutputExistenceOptions
{
  /// <summary>Whether the output-existence audit runs post-run. Default: true (when registered).</summary>
  public bool Enabled { get; set; } = true;

  /// <summary>
  /// When true (default), only missing outputs are reported. When
  /// false, every output's status (present/missing) is logged.
  /// </summary>
  public bool ReportMissingOnly { get; set; } = true;
}

/// <summary>Configuration for <see cref="RunSummaryProvider"/>.</summary>
public sealed class RunSummaryOptions
{
  /// <summary>Whether the run-summary block is emitted post-run. Default: true.</summary>
  public bool Enabled { get; set; } = true;
}
