using Flowthru.Flow;
using Flowthru.Prelude;
using Microsoft.Extensions.Logging;

namespace Flowthru.Diagnostics.Run;

/// <summary>
/// Post-run provider that emits a compact summary of the completed
/// run: flow label, status, total duration, success/failure counts,
/// and the slowest single step. Pure post-processing of
/// <see cref="FlowRunMetadataContext"/> — no live-storage access.
/// </summary>
public sealed class RunSummaryProvider : IPostRunMetadataProvider
{
  private readonly RunSummaryOptions _options;
  private readonly ILogger? _logger;

  public RunSummaryProvider(RunSummaryOptions? options = null, ILogger? logger = null)
  {
    _options = options ?? new RunSummaryOptions();
    _logger = logger;
  }

  /// <inheritdoc/>
  public string ProviderId => "Diagnostics.RunSummary";

  /// <inheritdoc/>
  public FlowIO<FlowUnit> Emit(FlowRunMetadataContext ctx) =>
    FlowIO.Lift(() =>
    {
      if (!_options.Enabled || _logger is null) return FlowUnit.Default;

      var steps = ctx.Result.StepResults;
      var successes = steps.Count(s => s is StepResult.Succeeded);
      var failures = steps.Count(s => s is StepResult.Failed);
      var skipped = steps.Count(s => s is StepResult.Skipped);
      var slowest = steps
        .OfType<StepResult.Succeeded>()
        .OrderByDescending(s => s.Duration)
        .FirstOrDefault();

      _logger.LogInformation("Diagnostics.RunSummary:");
      _logger.LogInformation(
        "  Flow:     {FlowName}",
        ctx.Static.RequestedFlowLabel ?? ctx.Static.EffectiveFlow.Label);
      _logger.LogInformation(
        "  Status:   {Status}", ctx.Result.IsSuccess ? "success" : "failure");
      _logger.LogInformation(
        "  Duration: {Duration:F3}s", ctx.Result.Duration.TotalSeconds);
      _logger.LogInformation(
        "  Steps:    {Successes} succeeded, {Failures} failed, {Skipped} skipped",
        successes, failures, skipped);

      if (slowest is not null)
      {
        _logger.LogInformation(
          "  Slowest:  {StepLabel} ({Duration:F3}s)",
          slowest.StepLabel, slowest.Duration.TotalSeconds);
      }
      return FlowUnit.Default;
    }, source: "Diagnostics.RunSummary");
}
