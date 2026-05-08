using Microsoft.Extensions.Configuration;
using SpaceflightsEFCore.Flows.DataScience.Steps;
using SpaceflightsEFCore.Flows.Reporting.Steps;

namespace SpaceflightsEFCore.Data;

/// <summary>
/// Configuration catalog for the SpaceflightsEFCore pipeline.
/// A plain reference type registered as a DI singleton via
/// <c>RegisterCatalog</c>; flow factories declare it as a parameter
/// alongside <see cref="Catalog"/> and the framework resolves both
/// from the host service provider.
/// </summary>
/// <remarks>
/// Per §2.6 / Phase 4: catalogs are DI-resolvable values. A
/// configuration record is a "catalog" in the same sense that a
/// data catalog is — it captures services (the
/// <see cref="IConfiguration"/>) and exposes typed option records to
/// flows. Replaces the legacy <c>[FlowthruConfig]</c> source-gen +
/// <c>[ConfigSection]</c> partial-property pattern with explicit
/// constructor binding.
/// </remarks>
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
