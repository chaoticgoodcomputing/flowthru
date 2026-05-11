using Flowthru.Flow;
using Flowthru.Prelude;
using Microsoft.Extensions.Logging;

namespace Flowthru.Diagnostics.Run;

/// <summary>
/// Post-run provider that summarises per-step execution times. Pure
/// post-processing of <see cref="FlowResult.StepResults"/> — no
/// live-storage access. Cost: a sort.
/// </summary>
/// <remarks>
/// Emits the top-N slowest steps to the configured logger and, if
/// <see cref="StepTimingOptions.SlowThreshold"/> is set, flags
/// individual steps exceeding it at warning level.
/// </remarks>
public sealed class StepTimingProvider : IPostRunMetadataProvider
{
  private readonly StepTimingOptions _options;
  private readonly ILogger? _logger;

  public StepTimingProvider(StepTimingOptions? options = null, ILogger? logger = null)
  {
    _options = options ?? new StepTimingOptions();
    _logger = logger;
  }

  /// <inheritdoc/>
  public string ProviderId => "Diagnostics.StepTimings";

  /// <inheritdoc/>
  public FlowIO<FlowUnit> Emit(FlowRunMetadataContext ctx) =>
    FlowIO.Lift(() =>
    {
      if (!_options.Enabled || _logger is null) return FlowUnit.Default;

      var succeeded = ctx.Result.StepResults
        .OfType<StepResult.Succeeded>()
        .ToList();
      if (succeeded.Count == 0) return FlowUnit.Default;

      if (_options.TopSlowest > 0)
      {
        var slowest = succeeded
          .OrderByDescending(s => s.Duration)
          .Take(_options.TopSlowest)
          .ToList();

        _logger.LogInformation(
          "Diagnostics.StepTimings — top {Count} slowest steps:", slowest.Count);
        foreach (var step in slowest)
        {
          _logger.LogInformation(
            "  {StepLabel,-40} {Duration,8:F3}s",
            step.StepLabel, step.Duration.TotalSeconds);
        }
      }

      if (_options.SlowThreshold is { } threshold)
      {
        var overThreshold = succeeded.Where(s => s.Duration > threshold).ToList();
        foreach (var step in overThreshold)
        {
          _logger.LogWarning(
            "Diagnostics.StepTimings — step {StepLabel} exceeded threshold "
            + "({Duration:F3}s > {Threshold:F3}s)",
            step.StepLabel, step.Duration.TotalSeconds, threshold.TotalSeconds);
        }
      }
      return FlowUnit.Default;
    }, source: "Diagnostics.StepTimings");
}
