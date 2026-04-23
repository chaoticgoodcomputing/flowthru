using Flowthru.Core.Data;
using KedroIris.Flows.DataEngineering.Steps;
using KedroIris.Flows.DataScience.Steps;

namespace KedroIris.Data;

/// <summary>
/// Configuration catalog for the Iris classification pipeline.
/// Properties are bound from appsettings.json via the source-generated constructor.
/// </summary>
[FlowthruConfig]
public partial class FlowConfig
{
  /// <summary>Configuration options for data splitting and one-hot encoding.</summary>
  [ConfigSection("Flowthru:Flows:DataEngineering")]
  public IItem<SplitAndEncodeStep.Options> SplitOptions { get; }

  /// <summary>Configuration options for logistic regression model training.</summary>
  [ConfigSection("Flowthru:Flows:DataScience")]
  public IItem<TrainModelStep.Options> TrainOptions { get; }
}
