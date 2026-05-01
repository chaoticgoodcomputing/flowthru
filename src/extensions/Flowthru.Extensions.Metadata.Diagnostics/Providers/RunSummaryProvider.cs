using Flowthru.Core.Graph.Meta.Models;
using Flowthru.Core.Meta.Providers;
using Microsoft.Extensions.Logging;

namespace Flowthru.Meta.Diagnostics.Providers;

/// <summary>
/// Post-run provider that emits a compact, structured summary of the completed run.
/// </summary>
/// <remarks>
/// Pure post-processing of <see cref="Flowthru.Core.Flows.FlowResult"/> — no live storage
/// access. Reports total duration, success/failure counts, and the slowest single step.
/// </remarks>
public sealed class RunSummaryProvider : IMetadataProvider, IPostRunMetadataProvider
{
  private readonly RunSummaryOptions _options;
  private readonly ILogger? _logger;

  /// <summary>
  /// Initializes a new <see cref="RunSummaryProvider"/>.
  /// </summary>
  /// <param name="options">Configuration; if null, defaults are used.</param>
  /// <param name="logger">Optional logger. When null, output is silent.</param>
  public RunSummaryProvider(RunSummaryOptions? options = null, ILogger? logger = null)
  {
    _options = options ?? new RunSummaryOptions();
    _logger = logger;
  }

  /// <inheritdoc />
  public string Name => "Diagnostics.RunSummary";

  /// <inheritdoc />
  public void Consume(DagMetadata dag)
  {
    // Summary is only meaningful post-run.
  }

  /// <inheritdoc />
  public void Consume(RunMetadata run)
  {
    if (!_options.Enabled || _logger is null)
    {
      return;
    }

    var steps = run.Result.StepResults.Values.ToList();
    var successes = steps.Count(s => s.Success);
    var failures = steps.Count(s => !s.Success);
    var slowest = steps.OrderByDescending(s => s.ExecutionTime).FirstOrDefault();

    _logger.LogInformation("Diagnostics.RunSummary:");
    _logger.LogInformation(
      "  Flow:     {FlowName}",
      run.Result.FlowName ?? run.Dag.FlowName
    );
    _logger.LogInformation(
      "  Status:   {Status}",
      run.Result.Success ? "success" : "failure"
    );
    _logger.LogInformation(
      "  Duration: {Duration:F3}s",
      run.Result.ExecutionTime.TotalSeconds
    );
    _logger.LogInformation("  Steps:    {Successes} succeeded, {Failures} failed", successes, failures);

    if (slowest is not null)
    {
      _logger.LogInformation(
        "  Slowest:  {StepName} ({Duration:F3}s)",
        slowest.StepName,
        slowest.ExecutionTime.TotalSeconds
      );
    }
  }
}
