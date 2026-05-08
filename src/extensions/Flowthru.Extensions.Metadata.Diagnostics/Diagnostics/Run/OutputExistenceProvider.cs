using Flowthru.Prelude;
using Microsoft.Extensions.Logging;

namespace Flowthru.Diagnostics.Run;

/// <summary>
/// Post-run provider that calls <see cref="Flowthru.Data.Catalog.IItem.Exists"/>
/// on each step's output items and reports any that are missing.
/// Useful as a sanity check that a successful pipeline actually
/// persisted what its DAG claims it did — catches silent storage
/// misconfigurations (wrong bucket, wrong path, permissions issues
/// that don't throw on write).
/// </summary>
/// <remarks>
/// <c>Exists()</c> is typically a cheap probe (HEAD request,
/// file-stat, <c>SELECT 1</c>) — far cheaper than a row count. The
/// provider issues one such call per output of every active step.
/// </remarks>
public sealed class OutputExistenceProvider : IPostRunMetadataProvider
{
  private readonly OutputExistenceOptions _options;
  private readonly ILogger? _logger;

  public OutputExistenceProvider(
    OutputExistenceOptions? options = null,
    ILogger? logger = null
  )
  {
    _options = options ?? new OutputExistenceOptions();
    _logger = logger;
  }

  /// <inheritdoc/>
  public string ProviderId => "Diagnostics.OutputExistence";

  /// <inheritdoc/>
  public FlowIO<FlowUnit> Emit(FlowRunMetadataContext ctx) =>
    FlowIO.LiftAsync(async cancellationToken =>
    {
      if (!_options.Enabled || _logger is null) return FlowUnit.Default;

      var checks = new List<(string StepLabel, string ItemLabel, bool Exists)>();
      foreach (var step in ctx.Static.MergedFlow.Steps)
      {
        if (!ctx.Static.ActiveStepLabels.Contains(step.Label)) continue;

        foreach (var output in step.Outputs)
        {
          var existsResult = await output.Exists().Run(cancellationToken).ConfigureAwait(false);
          var exists = existsResult switch
          {
            EffResult<bool>.Success ok => ok.Value,
            EffResult<bool>.Failure failure =>
              LogAndDefault(output.Label, failure.Error.Message),
            _ => false,
          };
          checks.Add((step.Label, output.Label, exists));
        }
      }

      var missing = checks.Where(c => !c.Exists).ToList();
      if (missing.Count > 0)
      {
        _logger.LogWarning(
          "Diagnostics.OutputExistence — {Count} declared output(s) missing after run:",
          missing.Count);
        foreach (var (stepLabel, itemLabel, _) in missing)
        {
          _logger.LogWarning("  ✗ {StepLabel,-40} → {ItemLabel}", stepLabel, itemLabel);
        }
      }

      if (!_options.ReportMissingOnly)
      {
        _logger.LogInformation(
          "Diagnostics.OutputExistence — full audit ({Count} outputs):", checks.Count);
        foreach (var (stepLabel, itemLabel, exists) in checks)
        {
          _logger.LogInformation(
            "  {Mark} {StepLabel,-40} → {ItemLabel}",
            exists ? "✓" : "✗", stepLabel, itemLabel);
        }
      }
      return FlowUnit.Default;
    }, source: "Diagnostics.OutputExistence");

  private bool LogAndDefault(string itemLabel, string message)
  {
    _logger?.LogWarning(
      "Diagnostics.OutputExistence: Exists() failed for {ItemLabel}: {Message}",
      itemLabel, message);
    return false;
  }
}
