using Flowthru.Data.Catalog;
using Flowthru.Prelude;
using Microsoft.Extensions.Logging;

namespace Flowthru.Diagnostics.Run;

/// <summary>
/// Post-run provider that reports row counts for items the executed
/// pipeline produced (and optionally consumed). Reads the
/// <see cref="IItem"/> references directly off the merged DAG in
/// <see cref="FlowMetadataContext.MergedFlow"/> — no DI lookup
/// required.
/// </summary>
/// <remarks>
/// <para>
/// <strong>Cost discipline.</strong> By default, only items whose
/// adapter implements <see cref="Flowthru.Data.Storage.IHasEfficientCount"/>
/// (e.g. a SQL <c>COUNT(*)</c> path, a directory-listing length) are
/// counted. Items lacking that capability are reported as <c>?</c>
/// rather than triggering a forced materialisation. Set
/// <see cref="RowCountOptions.ForceCountAll"/> to <c>true</c> only
/// after measuring the cost.
/// </para>
/// </remarks>
public sealed class RowCountProvider : IPostRunMetadataProvider
{
  private readonly RowCountOptions _options;
  private readonly ILogger? _logger;

  public RowCountProvider(RowCountOptions? options = null, ILogger? logger = null)
  {
    _options = options ?? new RowCountOptions();
    _logger = logger;
  }

  /// <inheritdoc/>
  public string ProviderId => "Diagnostics.RowCounts";

  /// <inheritdoc/>
  public FlowIO<FlowUnit> Emit(FlowRunMetadataContext ctx) =>
    FlowIO.LiftAsync(async cancellationToken =>
    {
      if (!_options.Enabled || _logger is null) return FlowUnit.Default;

      var rows = new List<(string StepLabel, string ItemLabel, string Direction, string Count)>();
      foreach (var step in ctx.Static.MergedFlow.Steps)
      {
        // Only counts for steps that actually ran in this slice.
        if (!ctx.Static.ActiveStepLabels.Contains(step.Label)) continue;

        if (_options.IncludeOutputs)
        {
          foreach (var output in step.Outputs)
          {
            rows.Add((step.Label, output.Label, "→",
              await FormatCountAsync(output, cancellationToken).ConfigureAwait(false)));
          }
        }
        if (_options.IncludeInputs)
        {
          foreach (var input in step.Inputs)
          {
            rows.Add((step.Label, input.Label, "←",
              await FormatCountAsync(input, cancellationToken).ConfigureAwait(false)));
          }
        }
      }

      if (rows.Count == 0) return FlowUnit.Default;

      _logger.LogInformation("Diagnostics.RowCounts — per-step item row counts:");
      foreach (var (stepLabel, itemLabel, direction, count) in rows)
      {
        _logger.LogInformation(
          "  {StepLabel,-40} {Direction} {ItemLabel,-30} {Count}",
          stepLabel, direction, itemLabel, count);
      }
      return FlowUnit.Default;
    }, source: "Diagnostics.RowCounts");

  private async Task<string> FormatCountAsync(IItem item, CancellationToken ct)
  {
    if (!item.HasEfficientCount && !_options.ForceCountAll)
    {
      return "? (no efficient count)";
    }

    var result = await item.GetCountAsync().Run(ct).ConfigureAwait(false);
    return result switch
    {
      EffResult<int>.Success ok => ok.Value.ToString(),
      EffResult<int>.Failure failure => Log(failure.Error.Message),
      _ => "? (unreachable)",
    };
  }

  private string Log(string message)
  {
    _logger?.LogWarning("Diagnostics.RowCounts: count failed: {Message}", message);
    return "? (error)";
  }
}
