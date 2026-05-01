using Flowthru.Core.Meta;
using Flowthru.Meta.Diagnostics.Providers;
using Microsoft.Extensions.Logging;

namespace Flowthru.Meta.Diagnostics;

/// <summary>
/// Registration extensions for the diagnostics metadata providers.
/// </summary>
/// <remarks>
/// <para>
/// The umbrella <see cref="UseDiagnostics(FlowthruMetadataBuilder, Action{DiagnosticsOptions}?, ILogger?)"/>
/// method registers a curated default set of post-run diagnostic providers in one call.
/// Per-provider extension methods (<c>AddStepTimings</c>, <c>AddRowCounts</c>, etc.) are
/// available for fine-grained control.
/// </para>
/// <para>
/// All providers default to <strong>opt-in</strong> behavior consistent with the framework
/// principle that the engine does not subsidize expensive observation. <c>StepTimings</c>
/// and <c>RunSummary</c> are enabled by default in <c>UseDiagnostics</c> because they
/// post-process <c>FlowResult</c> and are free; <c>RowCounts</c> and <c>OutputExistence</c>
/// require explicit opt-in via the configuration action.
/// </para>
/// </remarks>
public static class DiagnosticsMetadataExtensions
{
  /// <summary>
  /// Registers the curated default set of diagnostic providers.
  /// </summary>
  /// <param name="meta">The metadata builder to register against.</param>
  /// <param name="configure">Optional configuration action. Defaults: <c>StepTimings</c>
  /// and <c>RunSummary</c> enabled; <c>RowCounts</c> and <c>OutputExistence</c> disabled.</param>
  /// <param name="logger">Optional logger shared by all enabled providers. When null,
  /// output is silent.</param>
  public static FlowthruMetadataBuilder UseDiagnostics(
    this FlowthruMetadataBuilder meta,
    Action<DiagnosticsOptions>? configure = null,
    ILogger? logger = null
  )
  {
    var options = new DiagnosticsOptions();
    configure?.Invoke(options);

    if (options.RunSummary.Enabled)
    {
      meta.AddProvider(new RunSummaryProvider(options.RunSummary, logger));
    }

    if (options.StepTimings.Enabled)
    {
      meta.AddProvider(new StepTimingProvider(options.StepTimings, logger));
    }

    if (options.RowCounts.Enabled)
    {
      meta.AddProvider(new RowCountProvider(options.RowCounts, logger));
    }

    if (options.OutputExistence.Enabled)
    {
      meta.AddProvider(new OutputExistenceProvider(options.OutputExistence, logger));
    }

    return meta;
  }

  /// <summary>
  /// Registers <see cref="StepTimingProvider"/> with optional configuration.
  /// </summary>
  public static FlowthruMetadataBuilder AddStepTimings(
    this FlowthruMetadataBuilder meta,
    Action<StepTimingOptions>? configure = null,
    ILogger? logger = null
  )
  {
    var options = new StepTimingOptions();
    configure?.Invoke(options);
    return meta.AddProvider(new StepTimingProvider(options, logger));
  }

  /// <summary>
  /// Registers <see cref="RowCountProvider"/> with optional configuration. Defaults to
  /// counting only items whose adapter implements <c>IHasEfficientCount</c>.
  /// </summary>
  public static FlowthruMetadataBuilder AddRowCounts(
    this FlowthruMetadataBuilder meta,
    Action<RowCountOptions>? configure = null,
    ILogger? logger = null
  )
  {
    var options = new RowCountOptions();
    configure?.Invoke(options);
    return meta.AddProvider(new RowCountProvider(options, logger));
  }

  /// <summary>
  /// Registers <see cref="OutputExistenceProvider"/> with optional configuration.
  /// </summary>
  public static FlowthruMetadataBuilder AddOutputExistence(
    this FlowthruMetadataBuilder meta,
    Action<OutputExistenceOptions>? configure = null,
    ILogger? logger = null
  )
  {
    var options = new OutputExistenceOptions();
    configure?.Invoke(options);
    return meta.AddProvider(new OutputExistenceProvider(options, logger));
  }

  /// <summary>
  /// Registers <see cref="RunSummaryProvider"/> with optional configuration.
  /// </summary>
  public static FlowthruMetadataBuilder AddRunSummary(
    this FlowthruMetadataBuilder meta,
    Action<RunSummaryOptions>? configure = null,
    ILogger? logger = null
  )
  {
    var options = new RunSummaryOptions();
    configure?.Invoke(options);
    return meta.AddProvider(new RunSummaryProvider(options, logger));
  }
}
