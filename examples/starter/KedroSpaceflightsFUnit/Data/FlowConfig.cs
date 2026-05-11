using KedroSpaceflightsFUnit.Flows.DataScience.Steps;
using KedroSpaceflightsFUnit.Flows.Reporting.Steps;
using Microsoft.Extensions.Configuration;

namespace KedroSpaceflightsFUnit.Data;

/// <summary>
/// Configuration catalog for the Spaceflights pipeline. A plain reference type
/// registered as a DI singleton via <c>RegisterCatalog</c>.
/// </summary>
public sealed class FlowConfig
{
  public SplitDataStep.ModelOptions ModelOptions { get; }
  public CreateConfusionMatrixStep.Options ConfusionMatrixOptions { get; }

  public FlowConfig(IConfiguration configuration)
  {
    if (configuration is null) throw new ArgumentNullException(nameof(configuration));
    ModelOptions =
      configuration.GetSection("Flowthru:Flows:DataScience:ModelOptions").Get<SplitDataStep.ModelOptions>()
      ?? new SplitDataStep.ModelOptions();
    ConfusionMatrixOptions =
      configuration.GetSection("Flowthru:Flows:Reporting:ConfusionMatrixOptions").Get<CreateConfusionMatrixStep.Options>()
      ?? new CreateConfusionMatrixStep.Options();
  }
}
