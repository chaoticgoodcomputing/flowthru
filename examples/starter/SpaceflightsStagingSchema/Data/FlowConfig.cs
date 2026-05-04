using Flowthru.Core.Data;
using SpaceflightsStagingSchema.Flows.DataScience.Steps;
using SpaceflightsStagingSchema.Flows.Reporting.Steps;

namespace SpaceflightsStagingSchema.Data;

/// <summary>
/// Configuration catalog for the SpaceflightsStagingSchema pipeline.
/// Properties are bound from appsettings.json via the source-generated constructor.
/// </summary>
[FlowthruConfig]
public partial class FlowConfig
{
  [ConfigSection("Flowthru:Flows:DataScience:ModelOptions")]
  public partial IItem<SplitDataStep.ModelOptions> ModelOptions { get; }

  [ConfigSection("Flowthru:Flows:Reporting:ConfusionMatrixOptions")]
  public partial IItem<CreateConfusionMatrixStep.Options> ConfusionMatrixOptions { get; }
}
