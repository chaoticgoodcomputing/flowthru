using Flowthru.Core.Data;
using Flowthru.Core.Graph.Meta.Models;
using Flowthru.Core.Meta.Providers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Flowthru.Meta.Diagnostics.Providers;

/// <summary>
/// Post-run provider that calls <see cref="IItem.Exists"/> on each step's output items
/// and reports any that are missing.
/// </summary>
/// <remarks>
/// <para>
/// <c>Exists()</c> is typically a cheap check (HEAD request, file-stat, <c>SELECT 1</c>) —
/// far cheaper than a row count. The provider issues one such call per output item.
/// </para>
/// <para>
/// Useful as a sanity check that a successful pipeline actually persisted what its DAG
/// claims it did — catches silent storage misconfigurations (wrong bucket, wrong path,
/// permissions issues that don't throw on write).
/// </para>
/// </remarks>
public sealed class OutputExistenceProvider : IMetadataProvider, IPostRunMetadataProvider
{
  private readonly OutputExistenceOptions _options;
  private readonly ILogger? _logger;

  /// <summary>
  /// Initializes a new <see cref="OutputExistenceProvider"/>.
  /// </summary>
  /// <param name="options">Configuration; if null, defaults are used.</param>
  /// <param name="logger">Optional logger. When null, output is silent.</param>
  public OutputExistenceProvider(
    OutputExistenceOptions? options = null,
    ILogger? logger = null
  )
  {
    _options = options ?? new OutputExistenceOptions();
    _logger = logger;
  }

  /// <inheritdoc />
  public string Name => "Diagnostics.OutputExistence";

  /// <inheritdoc />
  public void Consume(DagMetadata dag)
  {
    // Existence is only meaningful post-run.
  }

  /// <inheritdoc />
  public void Consume(RunMetadata run)
  {
    _logger?.LogDebug(
      "Diagnostics.OutputExistence skipped: invoked without a service provider — register via "
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
      _logger.LogDebug(
        "Diagnostics.OutputExistence: no CatalogAbstract services registered."
      );
      return;
    }

    var checks = new List<(string StepName, string ItemLabel, bool Exists)>();

    foreach (var step in run.Dag.Steps)
    {
      foreach (var outputKey in step.Outputs)
      {
        if (!liveItems.TryGetValue(outputKey, out var item))
        {
          continue;
        }

        bool exists;
        try
        {
          exists = item.Exists().Run(default).AsTask().GetAwaiter().GetResult();
        }
        catch (Exception ex)
        {
          _logger.LogWarning(
            ex,
            "Diagnostics.OutputExistence: Exists() failed for {ItemLabel}: {Message}",
            item.Label,
            ex.Message
          );
          continue;
        }

        checks.Add((step.Label, outputKey, exists));
      }
    }

    var missing = checks.Where(c => !c.Exists).ToList();

    if (missing.Count > 0)
    {
      _logger.LogWarning(
        "Diagnostics.OutputExistence — {Count} declared output(s) missing after run:",
        missing.Count
      );
      foreach (var (stepName, itemLabel, _) in missing)
      {
        _logger.LogWarning("  ✗ {StepName,-40} → {ItemLabel}", stepName, itemLabel);
      }
    }

    if (!_options.ReportMissingOnly)
    {
      _logger.LogInformation(
        "Diagnostics.OutputExistence — full audit ({Count} outputs):",
        checks.Count
      );
      foreach (var (stepName, itemLabel, exists) in checks)
      {
        _logger.LogInformation(
          "  {Mark} {StepName,-40} → {ItemLabel}",
          exists ? "✓" : "✗",
          stepName,
          itemLabel
        );
      }
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
        dict.TryAdd(item.Label, item);
      }
    }
    return dict;
  }
}
