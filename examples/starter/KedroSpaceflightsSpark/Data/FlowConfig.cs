using Flowthru.Core.Data;
using KedroSpaceflightsSpark.Flows.DataScience.Steps;
using KedroSpaceflightsSpark.Flows.Reporting.Steps;

namespace KedroSpaceflightsSpark.Data;

/// <summary>
/// Configuration catalog for the Spaceflights Spark pipeline.
/// Properties are bound from appsettings.json via the source-generated constructor.
/// </summary>
[FlowthruConfig]
public partial class FlowConfig
{
  /// <summary>Configuration options for data splitting and model training.</summary>
  [ConfigSection("Flowthru:Flows:DataScience:ModelOptions")]
  public IItem<SplitDataStep.ModelOptions> ModelOptions { get; }

  /// <summary>Configuration options for confusion matrix generation.</summary>
  [ConfigSection("Flowthru:Flows:Reporting:ConfusionMatrixOptions")]
  public IItem<CreateConfusionMatrixStep.Options> ConfusionMatrixOptions { get; }
}
