using Flowthru.Core.Data;
using KedroSpaceflightsCustom.Flows.DataEvaluation.Steps;
using KedroSpaceflightsCustom.Flows.DataScience.Steps;

namespace KedroSpaceflightsCustom.Data;

/// <summary>
/// Configuration catalog for the KedroSpaceflightsCustom pipeline.
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
