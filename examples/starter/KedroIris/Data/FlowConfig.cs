using KedroIris.Flows.DataEngineering.Steps;
using KedroIris.Flows.DataScience.Steps;
using Microsoft.Extensions.Configuration;

namespace KedroIris.Data;

/// <summary>
/// Configuration catalog for the Iris classification pipeline. A plain reference type
/// registered as a DI singleton via <c>RegisterCatalog</c>; flow factories declare it
/// as a parameter alongside <see cref="Catalog"/> and the framework resolves both
/// from the host service provider.
/// </summary>
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
