using Flowthru.Core.Data;
using SpaceflightsDistributed.DataScience.Flows.DataScience.Steps;

namespace SpaceflightsDistributed.DataScience.Data;

/// <summary>
/// Configuration catalog for the DataScience pipeline library.
/// Properties are bound from appsettings.json via the source-generated constructor.
/// </summary>
[FlowthruConfig]
public partial class DataScienceFlowConfig
{
  /// <summary>Configuration options for data splitting and model training.</summary>
  [ConfigSection("Flowthru:Flows:DataScience:ModelOptions")]
  public IItem<SplitDataStep.ModelOptions> ModelOptions { get; }
}
