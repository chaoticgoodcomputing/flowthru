using Flowthru.Core.Data;
using Flowthru.Core.Graph.Meta.Models;
using Flowthru.Core.Meta.Providers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Flowthru.Meta.Diagnostics.Providers;

/// <summary>
/// Post-run provider that reports row counts for items produced (and optionally consumed)
/// by the executed pipeline.
/// </summary>
/// <remarks>
/// <para>
/// <strong>Cost discipline.</strong> By default, this provider only counts items whose
/// storage adapter implements <see cref="Flowthru.Core.Data.Storage.IHasEfficientCount"/>
/// (e.g., a SQL <c>COUNT(*)</c> path). Items without that capability are reported as
/// <c>?</c> rather than triggering a forced materialization. Set
/// <see cref="RowCountOptions.ForceCountAll"/> to <c>true</c> only after measuring the cost.
/// </para>
/// <para>
/// Items are resolved from <see cref="CatalogAbstract"/> services registered with the
/// host's DI container. The provider cross-references each step's outputs (and inputs,
/// if <see cref="RowCountOptions.IncludeInputs"/> is set) against the live catalog by
/// item label.
/// </para>
/// </remarks>
public sealed class RowCountProvider : IMetadataProvider, IPostRunMetadataProvider
{
  private readonly RowCountOptions _options;
  private readonly ILogger? _logger;

  /// <summary>
  /// Initializes a new <see cref="RowCountProvider"/>.
  /// </summary>
  /// <param name="options">Configuration; if null, defaults are used.</param>
  /// <param name="logger">Optional logger. When null, output is silent.</param>
  public RowCountProvider(RowCountOptions? options = null, ILogger? logger = null)
  {
    _options = options ?? new RowCountOptions();
    _logger = logger;
  }

  /// <inheritdoc />
  public string Name => "Diagnostics.RowCounts";

  /// <inheritdoc />
  public void Consume(DagMetadata dag)
  {
    // Counts are only meaningful post-run.
  }

  /// <inheritdoc />
  public void Consume(RunMetadata run)
  {
    // Without IServiceProvider we have no live catalog to resolve. The framework calls
    // the service-aware overload below; this fallback is for the rare case a host
    // wires the provider through a code path that bypasses the engine.
    _logger?.LogDebug(
      "Diagnostics.RowCounts skipped: invoked without a service provider — register via "
        + "ConfigureMetadata so the engine can supply DI."
    );
  }

  /// <inheritdoc />
  public void Consume(RunMetadata run, IServiceProvider services)
  {
    if (!_options.Enabled || _logger is null)
    {
      return;
    }

    var liveItems = ResolveLiveItems(services);
    if (liveItems.Count == 0)
    {
      _logger.LogDebug("Diagnostics.RowCounts: no CatalogAbstract services registered.");
      return;
    }

    var rows = new List<(string StepName, string ItemLabel, string Direction, string Count)>();

    foreach (var step in run.Dag.Steps)
    {
      if (_options.IncludeOutputs)
      {
        foreach (var outputKey in step.Outputs)
        {
          if (liveItems.TryGetValue(outputKey, out var item))
          {
            rows.Add((step.Label, outputKey, "→", FormatCount(item)));
          }
        }
      }

      if (_options.IncludeInputs)
      {
        foreach (var inputKey in step.Inputs)
        {
          if (liveItems.TryGetValue(inputKey, out var item))
          {
            rows.Add((step.Label, inputKey, "←", FormatCount(item)));
          }
        }
      }
    }

    if (rows.Count == 0)
    {
      return;
    }

    _logger.LogInformation("Diagnostics.RowCounts — per-step item row counts:");
    foreach (var (stepName, itemLabel, direction, count) in rows)
    {
      _logger.LogInformation(
        "  {StepName,-40} {Direction} {ItemLabel,-30} {Count}",
        stepName,
        direction,
        itemLabel,
        count
      );
    }
  }

  private string FormatCount(IItem item)
  {
    if (!item.HasEfficientCount && !_options.ForceCountAll)
    {
      return "? (no efficient count)";
    }

    try
    {
      var count = item.GetCountAsync().Run(default).AsTask().GetAwaiter().GetResult();
      return count.ToString();
    }
    catch (Exception ex)
    {
      _logger?.LogWarning(
        ex,
        "Diagnostics.RowCounts: count failed for {ItemLabel}: {Message}",
        item.Label,
        ex.Message
      );
      return "? (error)";
    }
  }

  private static Dictionary<string, IItem> ResolveLiveItems(IServiceProvider services)
  {
    var catalogs = services.GetServices<CatalogAbstract>();
    var dict = new Dictionary<string, IItem>(StringComparer.OrdinalIgnoreCase);
    foreach (var catalog in catalogs)
    {
      foreach (var item in catalog.GetAllItems())
      {
        // First-seen wins — matches first-write-wins semantics for cross-catalog shared items.
        dict.TryAdd(item.Label, item);
      }
    }
    return dict;
  }
}
