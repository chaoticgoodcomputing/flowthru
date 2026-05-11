using KedroIrisFUnit.Flows.DataEngineering.Steps;
using KedroIrisFUnit.Flows.DataScience.Steps;
using Microsoft.Extensions.Configuration;

namespace KedroIrisFUnit.Data;

/// <summary>
/// Configuration catalog for the Iris classification pipeline.
/// A plain reference type registered as a DI singleton via
/// <c>RegisterCatalog</c>; flow factories declare it as a parameter
/// alongside <see cref="Catalog"/> and the framework resolves both
/// from the host service provider.
/// </summary>
/// <remarks>
/// Per §2.6 / Phase 4: catalogs are DI-resolvable values. A
/// configuration record is a "catalog" in the same sense that a
/// data catalog is — it's a value-shaped object that captures
/// services (the <see cref="IConfiguration"/>) and exposes typed
/// option records to flows. Replaces the legacy
/// <c>[FlowthruConfig]</c> source-gen + <c>[ConfigSection]</c>
/// partial-property pattern with explicit constructor binding.
/// </remarks>
public sealed class FlowConfig
{
  /// <summary>Configuration options for data splitting and one-hot encoding.</summary>
  public SplitAndEncodeStep.Options SplitOptions { get; }

  /// <summary>Configuration options for logistic regression model training.</summary>
  public TrainModelStep.Options TrainOptions { get; }

  public FlowConfig(IConfiguration configuration)
  {
    if (configuration is null) throw new ArgumentNullException(nameof(configuration));
    SplitOptions =
      configuration.GetSection("Flowthru:Flows:DataEngineering").Get<SplitAndEncodeStep.Options>()
      ?? new SplitAndEncodeStep.Options();
    TrainOptions =
      configuration.GetSection("Flowthru:Flows:DataScience").Get<TrainModelStep.Options>()
      ?? new TrainModelStep.Options();
  }
}
