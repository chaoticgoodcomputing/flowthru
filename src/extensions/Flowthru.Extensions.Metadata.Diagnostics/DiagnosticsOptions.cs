namespace Flowthru.Meta.Diagnostics;

/// <summary>
/// Configuration for the umbrella <c>UseDiagnostics</c> registration.
/// </summary>
/// <remarks>
/// <para>
/// Each property configures one of the built-in diagnostic providers. By default,
/// <see cref="StepTimings"/> and <see cref="RunSummary"/> are enabled (they are free,
/// purely post-processing the existing <c>FlowResult</c>); <see cref="RowCounts"/>
/// and <see cref="OutputExistence"/> are disabled because they touch live storage and
/// the framework will not subsidize that cost.
/// </para>
/// <para>
/// Enabling <see cref="RowCounts"/> defaults to "only count items whose adapter
/// implements <see cref="Flowthru.Core.Data.Storage.IHasEfficientCount"/>" — opt-in
/// is required to force counts on adapters that would have to materialize.
/// </para>
/// </remarks>
public sealed class DiagnosticsOptions
{
  /// <summary>
  /// Step timing summary configuration. Enabled by default — pure post-processing
  /// of <c>FlowResult.StepResults</c>, no live storage access.
  /// </summary>
  public StepTimingOptions StepTimings { get; } = new();

  /// <summary>
  /// Row count configuration. Disabled by default. When enabled, defaults to counting
  /// only items whose adapter implements <c>IHasEfficientCount</c>.
  /// </summary>
  public RowCountOptions RowCounts { get; } = new() { Enabled = false };

  /// <summary>
  /// Output existence audit configuration. Disabled by default. When enabled,
  /// post-run calls <c>Exists()</c> on each step's output items and reports any
  /// that are missing.
  /// </summary>
  public OutputExistenceOptions OutputExistence { get; } = new() { Enabled = false };

  /// <summary>
  /// Run summary configuration. Enabled by default — pure post-processing.
  /// </summary>
  public RunSummaryOptions RunSummary { get; } = new();
}

/// <summary>
/// Configuration for <see cref="Providers.StepTimingProvider"/>.
/// </summary>
public sealed class StepTimingOptions
{
  /// <summary>Whether the step timing summary is emitted post-run. Default: true.</summary>
  public bool Enabled { get; set; } = true;

  /// <summary>
  /// Number of slowest steps to report in the summary. Default: 5. Set to 0 to skip
  /// the top-N section.
  /// </summary>
  public int TopSlowest { get; set; } = 5;

  /// <summary>
  /// Optional threshold; steps that exceed this duration are flagged at warning level.
  /// Default: null (no threshold flagging).
  /// </summary>
  public TimeSpan? SlowThreshold { get; set; }
}

/// <summary>
/// Configuration for <see cref="Providers.RowCountProvider"/>.
/// </summary>
public sealed class RowCountOptions
{
  /// <summary>Whether the row count summary is emitted post-run. Default: false.</summary>
  public bool Enabled { get; set; } = true;

  /// <summary>
  /// Whether to include input items in the count. Default: false (outputs only —
  /// post-run input state is usually not the subject of interest).
  /// </summary>
  public bool IncludeInputs { get; set; } = false;

  /// <summary>
  /// Whether to include output items in the count. Default: true.
  /// </summary>
  public bool IncludeOutputs { get; set; } = true;

  /// <summary>
  /// When false (default), only items whose storage adapter implements
  /// <see cref="Flowthru.Core.Data.Storage.IHasEfficientCount"/> are counted; others
  /// are reported as <c>?</c>. When true, every item is counted, which may force
  /// materialization for non-efficient adapters. <strong>Default: false — flip
  /// only if you have measured the cost.</strong>
  /// </summary>
  public bool ForceCountAll { get; set; } = false;
}

/// <summary>
/// Configuration for <see cref="Providers.OutputExistenceProvider"/>.
/// </summary>
public sealed class OutputExistenceOptions
{
  /// <summary>Whether the output existence audit runs post-run. Default: false.</summary>
  public bool Enabled { get; set; } = true;

  /// <summary>
  /// When true (default), only missing outputs are reported. When false, every
  /// output's status (present/missing) is logged.
  /// </summary>
  public bool ReportMissingOnly { get; set; } = true;
}

/// <summary>
/// Configuration for <see cref="Providers.RunSummaryProvider"/>.
/// </summary>
public sealed class RunSummaryOptions
{
  /// <summary>Whether the run summary block is emitted post-run. Default: true.</summary>
  public bool Enabled { get; set; } = true;
}
