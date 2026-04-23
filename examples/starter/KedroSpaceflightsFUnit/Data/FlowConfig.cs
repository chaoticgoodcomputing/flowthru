using Flowthru.Core.Data;
using KedroSpaceflightsFUnit.Flows.DataScience.Steps;
using KedroSpaceflightsFUnit.Flows.Reporting.Steps;

namespace KedroSpaceflightsFUnit.Data;

/// <summary>
/// Configuration catalog for the Spaceflights pipeline.
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
