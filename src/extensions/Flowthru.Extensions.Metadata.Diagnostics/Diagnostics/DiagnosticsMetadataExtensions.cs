using Flowthru.Diagnostics.Run;
using Microsoft.Extensions.Logging;

namespace Flowthru.Diagnostics;

/// <summary>
/// Extension methods that contribute the run-diagnostics providers
/// into <see cref="FlowthruMetadataBuilder"/>. End users see them as
/// <c>builder.UseDiagnostics(opt =&gt; ...)</c> via a single
/// <c>using Flowthru.Diagnostics;</c> import.
/// </summary>
/// <remarks>
/// <para>
/// Per-provider entry points (<see cref="AddStepTimings"/>,
/// <see cref="AddRowCounts"/>, <see cref="AddOutputExistence"/>,
/// <see cref="AddRunSummary"/>) are available for fine-grained
/// control. The umbrella <see cref="UseDiagnostics"/> registers a
/// curated default set in one call.
/// </para>
/// <para>
/// All providers default to <strong>opt-in</strong> behaviour
/// consistent with the framework principle that the engine does not
/// subsidise expensive observation. <see cref="StepTimingProvider"/>
/// and <see cref="RunSummaryProvider"/> are enabled by default in
/// <see cref="UseDiagnostics"/> because they post-process
/// <c>FlowResult</c> and are free; <see cref="RowCountProvider"/> and
/// <see cref="OutputExistenceProvider"/> require explicit opt-in.
/// </para>
/// </remarks>
public static class DiagnosticsMetadataExtensions
{
  /// <summary>
  /// Register the curated default set of diagnostic providers.
  /// </summary>
  public static FlowthruMetadataBuilder UseDiagnostics(
    this FlowthruMetadataBuilder builder,
    Action<DiagnosticsOptions>? configure = null,
    ILogger? logger = null
  )
  {
    if (builder is null) throw new ArgumentNullException(nameof(builder));

    var options = new DiagnosticsOptions();
    configure?.Invoke(options);

    if (options.RunSummary.Enabled)
      builder.AddProvider(new RunSummaryProvider(options.RunSummary, logger));
    if (options.StepTimings.Enabled)
      builder.AddProvider(new StepTimingProvider(options.StepTimings, logger));
    if (options.RowCounts.Enabled)
      builder.AddProvider(new RowCountProvider(options.RowCounts, logger));
    if (options.OutputExistence.Enabled)
      builder.AddProvider(new OutputExistenceProvider(options.OutputExistence, logger));

    return builder;
  }

  /// <summary>Register <see cref="StepTimingProvider"/> with optional configuration.</summary>
  public static FlowthruMetadataBuilder AddStepTimings(
    this FlowthruMetadataBuilder builder,
    Action<StepTimingOptions>? configure = null,
    ILogger? logger = null
  )
  {
    if (builder is null) throw new ArgumentNullException(nameof(builder));
    var options = new StepTimingOptions();
    configure?.Invoke(options);
    return builder.AddProvider(new StepTimingProvider(options, logger));
  }

  /// <summary>Register <see cref="RunSummaryProvider"/> with optional configuration.</summary>
  public static FlowthruMetadataBuilder AddRunSummary(
    this FlowthruMetadataBuilder builder,
    Action<RunSummaryOptions>? configure = null,
    ILogger? logger = null
  )
  {
    if (builder is null) throw new ArgumentNullException(nameof(builder));
    var options = new RunSummaryOptions();
    configure?.Invoke(options);
    return builder.AddProvider(new RunSummaryProvider(options, logger));
  }

  /// <summary>Register <see cref="RowCountProvider"/> with optional configuration.</summary>
  public static FlowthruMetadataBuilder AddRowCounts(
    this FlowthruMetadataBuilder builder,
    Action<RowCountOptions>? configure = null,
    ILogger? logger = null
  )
  {
    if (builder is null) throw new ArgumentNullException(nameof(builder));
    var options = new RowCountOptions();
    configure?.Invoke(options);
    return builder.AddProvider(new RowCountProvider(options, logger));
  }

  /// <summary>Register <see cref="OutputExistenceProvider"/> with optional configuration.</summary>
  public static FlowthruMetadataBuilder AddOutputExistence(
    this FlowthruMetadataBuilder builder,
    Action<OutputExistenceOptions>? configure = null,
    ILogger? logger = null
  )
  {
    if (builder is null) throw new ArgumentNullException(nameof(builder));
    var options = new OutputExistenceOptions();
    configure?.Invoke(options);
    return builder.AddProvider(new OutputExistenceProvider(options, logger));
  }
}
