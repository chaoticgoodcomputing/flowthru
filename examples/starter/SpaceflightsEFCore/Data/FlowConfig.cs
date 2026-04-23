using Flowthru.Core.Data;
using SpaceflightsEFCore.Flows.DataScience.Steps;
using SpaceflightsEFCore.Flows.Reporting.Steps;

namespace SpaceflightsEFCore.Data;

/// <summary>
/// Configuration catalog for the SpaceflightsEFCore pipeline.
/// Properties are bound from appsettings.json via the source-generated constructor.
/// </summary>
[FlowthruConfig]
public partial class FlowConfig
{
  /// <summary>Configuration options for data splitting and model training.</summary>
  [ConfigSection("Flowthru:Flows:DataScience:ModelOptions")]
  public partial IItem<SplitDataStep.ModelOptions> ModelOptions { get; }

  /// <summary>Configuration options for confusion matrix generation.</summary>
  [ConfigSection("Flowthru:Flows:Reporting:ConfusionMatrixOptions")]
  public partial IItem<CreateConfusionMatrixStep.Options> ConfusionMatrixOptions { get; }
}
