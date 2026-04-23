using Flowthru.Core.Data;
using SpaceflightsDistributed.Reporting.Flows.Reporting.Steps;

namespace SpaceflightsDistributed.Reporting.Data;

/// <summary>
/// Configuration catalog for the Reporting pipeline library.
/// Properties are bound from appsettings.json via the source-generated constructor.
/// </summary>
[FlowthruConfig]
public partial class ReportingFlowConfig
{
  /// <summary>Configuration options for confusion matrix generation.</summary>
  [ConfigSection("Flowthru:Flows:Reporting:ConfusionMatrixOptions")]
  public partial IItem<CreateConfusionMatrixStep.Options> ConfusionMatrixOptions { get; }
}
