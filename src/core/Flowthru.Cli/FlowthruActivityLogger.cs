using System.Diagnostics;
using Microsoft.Extensions.Logging;

namespace Flowthru.Cli;

/// <summary>
/// Bridges Core's <see cref="FlowthruActivitySource"/> events into
/// <see cref="ILogger"/> log lines so any host wiring
/// <c>ILoggerFactory</c> through DI gets the legacy-style
/// "→ executing… ✓ done" run output for free. Subscribes via
/// <see cref="ActivityListener"/>; Core itself has no logging
/// dependency.
/// </summary>
/// <remarks>
/// <para>
/// Per the Phase-7 follow-up, the runtime's responsibility is to
/// emit structured events through <see cref="ActivitySource"/>;
/// rendering them is the consumer's concern. The CLI is the
/// canonical consumer — it's the human-host entry point — so the
/// bridge lives here. Other hosts (background workers, tests,
/// OpenTelemetry exporters) can register their own
/// <see cref="ActivityListener"/> subscribers without consulting
/// this class.
/// </para>
/// <para>
/// Lifetime: instantiate once before the flow runs, dispose after.
/// <see cref="FlowthruCli.RunStandaloneAsync"/> wraps the bridge in
/// a <c>using</c> block scoped to the run. While instantiated, every
/// activity emitted by Core's <see cref="ActivitySource"/> with a
/// matching name is rendered as a structured log line.
/// </para>
/// </remarks>
public sealed class FlowthruActivityLogger : IDisposable
{
  private readonly ActivityListener _listener;
  private readonly ILogger _logger;

  public FlowthruActivityLogger(ILogger logger)
  {
    _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    _listener = new ActivityListener
    {
      ShouldListenTo = source => source.Name == FlowthruActivitySource.SourceName,
      Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData,
      ActivityStarted = OnStarted,
      ActivityStopped = OnStopped,
    };
    ActivitySource.AddActivityListener(_listener);
  }

  /// <summary>
  /// Convenience overload that takes an
  /// <see cref="ILoggerFactory"/> and creates an
  /// <c>ILogger&lt;FlowthruActivityLogger&gt;</c> from it.
  /// </summary>
  public FlowthruActivityLogger(ILoggerFactory loggerFactory)
    : this(
      (loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory)))
        .CreateLogger<FlowthruActivityLogger>()
    )
  { }

  /// <inheritdoc/>
  public void Dispose() => _listener.Dispose();

  private void OnStarted(Activity activity)
  {
    switch (activity.OperationName)
    {
      case FlowthruActivitySource.RunActivityName:
        var label = activity.GetTagItem(FlowthruActivitySource.TagFlowLabel) as string ?? "(merged)";
        var stepCount = activity.GetTagItem(FlowthruActivitySource.TagFlowStepCount);
        var sliced = activity.GetTagItem(FlowthruActivitySource.TagFlowSliced) is true;
        if (sliced)
        {
          _logger.LogInformation(
            "→ Running flow '{FlowLabel}' ({StepCount} step(s) after slicing)",
            label, stepCount
          );
        }
        else
        {
          _logger.LogInformation(
            "→ Running merged DAG ({StepCount} step(s))",
            stepCount
          );
        }
        break;
      case FlowthruActivitySource.PreFlightActivityName:
        _logger.LogInformation("→ Pre-flight checks…");
        break;
      case FlowthruActivitySource.StepActivityName:
        var stepLabel = activity.GetTagItem(FlowthruActivitySource.TagStepLabel) as string ?? "?";
        _logger.LogInformation("  → {StepLabel} executing…", stepLabel);
        break;
      case FlowthruActivitySource.CacheUncacheableActivityName:
        // Pre-flight cache-plan post-processing emits one of these per
        // step the plan marked uncacheable. Per-step rendering lets
        // flow authors audit cache eligibility at Information level
        // instead of bisecting through cache-plan-builder source.
        var uncacheableLabel = activity.GetTagItem(FlowthruActivitySource.TagStepLabel) as string ?? "?";
        var reason = activity.GetTagItem(FlowthruActivitySource.TagCacheUncacheableReason) as string
          ?? "(unknown)";
        _logger.LogInformation(
          "  ⊘ {StepLabel} uncacheable: {Reason}", uncacheableLabel, reason);
        break;
    }
  }

  private void OnStopped(Activity activity)
  {
    var ms = activity.Duration.TotalMilliseconds;
    switch (activity.OperationName)
    {
      case FlowthruActivitySource.StepActivityName:
        var stepLabel = activity.GetTagItem(FlowthruActivitySource.TagStepLabel) as string ?? "?";
        if (activity.Status == ActivityStatusCode.Error)
        {
          _logger.LogWarning(
            "  ✗ {StepLabel} failed in {Duration:F2} ms: {Reason}",
            stepLabel, ms, activity.StatusDescription
          );
        }
        else
        {
          _logger.LogInformation(
            "  ✓ {StepLabel} ({Duration:F2} ms)",
            stepLabel, ms
          );
        }
        break;
      case FlowthruActivitySource.PreFlightActivityName:
        if (activity.Status == ActivityStatusCode.Error)
        {
          var errorCount = activity.GetTagItem(FlowthruActivitySource.TagPreFlightErrorCount);
          _logger.LogWarning(
            "  ✗ Pre-flight failed with {ErrorCount} error(s) in {Duration:F2} ms",
            errorCount, ms
          );
        }
        else
        {
          _logger.LogInformation("  ✓ Pre-flight passed ({Duration:F2} ms)", ms);
        }
        break;
      case FlowthruActivitySource.RunActivityName:
        if (activity.Status == ActivityStatusCode.Error)
        {
          _logger.LogWarning(
            "Flow run finished with failures in {Duration:F2} ms: {Reason}",
            ms, activity.StatusDescription
          );
        }
        else
        {
          _logger.LogInformation("Flow run finished in {Duration:F2} ms", ms);
        }
        break;
    }
  }
}
