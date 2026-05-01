using Flowthru.Core.Graph.Meta.Models;
using Flowthru.Core.Meta.Providers;
using Microsoft.Extensions.Logging;

namespace Flowthru.Meta.Diagnostics.Providers;

/// <summary>
/// Post-run provider that summarizes per-step execution times.
/// </summary>
/// <remarks>
/// <para>
/// Pure post-processing of <see cref="Flowthru.Core.Flows.FlowResult.StepResults"/> —
/// no live storage access, no service-provider resolution. Cost: a sort.
/// </para>
/// <para>
/// Emits a top-N slowest-steps block to the configured logger and, if
/// <see cref="StepTimingOptions.SlowThreshold"/> is set, flags individual steps that
/// exceeded it at warning level.
/// </para>
/// </remarks>
public sealed class StepTimingProvider : IMetadataProvider, IPostRunMetadataProvider
{
  private readonly StepTimingOptions _options;
  private readonly ILogger? _logger;

  /// <summary>
  /// Initializes a new <see cref="StepTimingProvider"/>.
  /// </summary>
  /// <param name="options">Configuration; if null, defaults are used.</param>
  /// <param name="logger">Optional logger. When null, output is silent.</param>
  public StepTimingProvider(StepTimingOptions? options = null, ILogger? logger = null)
  {
    _options = options ?? new StepTimingOptions();
    _logger = logger;
  }

  /// <inheritdoc />
  public string Name => "Diagnostics.StepTimings";

  /// <inheritdoc />
  public void Consume(DagMetadata dag)
  {
    // No pre-run output — there are no timings to report yet.
  }

  /// <inheritdoc />
  public void Consume(RunMetadata run)
  {
    if (!_options.Enabled || _logger is null)
    {
      return;
    }

    var steps = run.Result.StepResults.Values.ToList();
    if (steps.Count == 0)
    {
      return;
    }

    if (_options.TopSlowest > 0)
    {
      var slowest = steps
        .OrderByDescending(s => s.ExecutionTime)
        .Take(_options.TopSlowest)
        .ToList();

      _logger.LogInformation(
        "Diagnostics.StepTimings — top {Count} slowest steps:",
        slowest.Count
      );
      foreach (var step in slowest)
      {
        _logger.LogInformation(
          "  {StepName,-40} {Duration,8:F3}s",
          step.StepName,
          step.ExecutionTime.TotalSeconds
        );
      }
    }

    if (_options.SlowThreshold is { } threshold)
    {
      var overThreshold = steps.Where(s => s.ExecutionTime > threshold).ToList();
      foreach (var step in overThreshold)
      {
        _logger.LogWarning(
          "Diagnostics.StepTimings — step {StepName} exceeded threshold ({Duration:F3}s > {Threshold:F3}s)",
          step.StepName,
          step.ExecutionTime.TotalSeconds,
          threshold.TotalSeconds
        );
      }
    }
  }
}
