using Flowthru.Core.Data;
using KedroIrisFUnit.Flows.DataEngineering.Steps;
using KedroIrisFUnit.Flows.DataScience.Steps;

namespace KedroIrisFUnit.Data;

/// <summary>
/// Configuration catalog for the Iris classification pipeline.
/// Properties are bound from appsettings.json via the source-generated constructor.
/// </summary>
[FlowthruConfig]
public partial class FlowConfig
{
  /// <summary>Configuration options for data splitting and one-hot encoding.</summary>
  [ConfigSection("Flowthru:Flows:DataEngineering")]
  public partial IItem<SplitAndEncodeStep.Options> SplitOptions { get; }

  /// <summary>Configuration options for logistic regression model training.</summary>
  [ConfigSection("Flowthru:Flows:DataScience")]
  public partial IItem<TrainModelStep.Options> TrainOptions { get; }
}
