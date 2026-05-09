using KedroSpaceflightsGQL.Flows.DataScience.Steps;
using KedroSpaceflightsGQL.Flows.Reporting.Steps;
using Microsoft.Extensions.Configuration;

namespace KedroSpaceflightsGQL.Data;

/// <summary>
/// Configuration catalog for the KedroSpaceflightsGQL pipeline. A plain reference type
/// registered as a DI singleton via <c>RegisterCatalog</c>; flow factories declare it as
/// a parameter alongside <see cref="Catalog"/> and the framework resolves both from the
/// host service provider.
/// </summary>
public sealed class FlowConfig
{
  /// <summary>Configuration options for data splitting and model training.</summary>
  public SplitDataStep.ModelOptions ModelOptions { get; }

  /// <summary>Configuration options for confusion matrix generation.</summary>
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
