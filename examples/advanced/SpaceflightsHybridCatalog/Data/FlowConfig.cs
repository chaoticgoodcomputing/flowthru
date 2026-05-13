using Microsoft.Extensions.Configuration;
using SpaceflightsHybridCatalog.Flows.DataScience.Steps;
using SpaceflightsHybridCatalog.Flows.Reporting.Steps;

namespace SpaceflightsHybridCatalog.Data;

/// <summary>
/// Configuration catalog for the SpaceflightsHybridCatalog pipeline. Bound to
/// <see cref="IConfiguration"/> at DI construction time so flow factories pull
/// typed option records as ordinary catalog properties.
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
