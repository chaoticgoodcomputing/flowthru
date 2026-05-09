using Microsoft.Extensions.Configuration;
using SpaceflightsDistributed.Reporting.Flows.Reporting.Steps;

namespace SpaceflightsDistributed.Reporting.Data;

/// <summary>
/// Configuration catalog for the Reporting pipeline library.
/// </summary>
public sealed class ReportingFlowConfig
{
  public CreateConfusionMatrixStep.Options ConfusionMatrixOptions { get; }

  public ReportingFlowConfig(IConfiguration configuration)
  {
    if (configuration is null) throw new ArgumentNullException(nameof(configuration));
    ConfusionMatrixOptions =
      configuration.GetSection("Flowthru:Flows:Reporting:ConfusionMatrixOptions").Get<CreateConfusionMatrixStep.Options>()
      ?? new CreateConfusionMatrixStep.Options();
  }
}
