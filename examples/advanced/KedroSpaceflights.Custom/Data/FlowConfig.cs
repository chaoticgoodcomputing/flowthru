using Flowthru.Core.Data;
using KedroSpaceflights.Custom.Flows.DataEvaluation.Steps;
using KedroSpaceflights.Custom.Flows.DataScience.Steps;

namespace KedroSpaceflights.Custom.Data;

/// <summary>
/// Configuration catalog for the KedroSpaceflights.Custom pipeline.
/// Properties are bound from appsettings.json via the source-generated constructor.
/// </summary>
[FlowthruConfig]
public partial class FlowConfig
{
  /// <summary>Configuration options for the train/test split step.</summary>
  [ConfigSection("Flowthru:Flows:DataScience:ModelParams")]
  public partial IItem<CreateTestTrainSplitStep.TestTrainSplitParams> ModelParams { get; }

  /// <summary>Configuration options for cross-validation.</summary>
  [ConfigSection("Flowthru:Flows:DataEvaluation:CrossValidationParams")]
  public partial IItem<CrossValidateModelStep.Params> CrossValidationParams { get; }
}
