using Microsoft.Extensions.Configuration;
using SpaceflightsDistributed.DataScience.Flows.DataScience.Steps;

namespace SpaceflightsDistributed.DataScience.Data;

/// <summary>
/// Configuration catalog for the DataScience pipeline library.
/// </summary>
public sealed class DataScienceFlowConfig
{
  public SplitDataStep.ModelOptions ModelOptions { get; }

  public DataScienceFlowConfig(IConfiguration configuration)
  {
    if (configuration is null) throw new ArgumentNullException(nameof(configuration));
    ModelOptions =
      configuration.GetSection("Flowthru:Flows:DataScience:ModelOptions").Get<SplitDataStep.ModelOptions>()
      ?? new SplitDataStep.ModelOptions();
  }
}
