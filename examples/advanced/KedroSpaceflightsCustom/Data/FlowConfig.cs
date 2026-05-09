using KedroSpaceflightsCustom.Flows.DataEvaluation.Steps;
using KedroSpaceflightsCustom.Flows.DataScience.Steps;
using Microsoft.Extensions.Configuration;

namespace KedroSpaceflightsCustom.Data;

/// <summary>
/// Configuration catalog for the KedroSpaceflightsCustom pipeline.
/// Properties are bound from appsettings.json.
/// </summary>
public sealed class FlowConfig
{
  /// <summary>Configuration options for the train/test split step.</summary>
  public CreateTestTrainSplitStep.TestTrainSplitParams ModelParams { get; }

  /// <summary>Configuration options for cross-validation.</summary>
  public CrossValidateModelStep.Params CrossValidationParams { get; }

  public FlowConfig(IConfiguration configuration)
  {
    if (configuration is null) throw new ArgumentNullException(nameof(configuration));
    ModelParams =
      configuration.GetSection("Flowthru:Flows:DataScience:ModelParams").Get<CreateTestTrainSplitStep.TestTrainSplitParams>()
      ?? new CreateTestTrainSplitStep.TestTrainSplitParams();
    CrossValidationParams =
      configuration.GetSection("Flowthru:Flows:DataEvaluation:CrossValidationParams").Get<CrossValidateModelStep.Params>()
      ?? new CrossValidateModelStep.Params();
  }
}
