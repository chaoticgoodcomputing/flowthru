using Microsoft.Extensions.Configuration;
using SpaceflightsStagingSchema.Flows.DataProcessing;
using SpaceflightsStagingSchema.Flows.DataScience.Steps;
using SpaceflightsStagingSchema.Flows.Reporting.Steps;

namespace SpaceflightsStagingSchema.Data;

/// <summary>
/// Configuration catalog for the SpaceflightsStagingSchema pipeline.
/// </summary>
public sealed class FlowConfig
{
  /// <summary>
  /// Synthetic-data seeding knobs. Set the per-source counts to non-zero
  /// values to scale the pipeline up to bulk-test volumes.
  /// </summary>
  public SeedingOptions SeedingOptions { get; }

  public SplitDataStep.ModelOptions ModelOptions { get; }

  public CreateConfusionMatrixStep.Options ConfusionMatrixOptions { get; }

  public FlowConfig(IConfiguration configuration)
  {
    if (configuration is null) throw new ArgumentNullException(nameof(configuration));
    SeedingOptions =
      configuration.GetSection("Flowthru:Flows:DataProcessing:Seeding").Get<SeedingOptions>()
      ?? new SeedingOptions();
    ModelOptions =
      configuration.GetSection("Flowthru:Flows:DataScience:ModelOptions").Get<SplitDataStep.ModelOptions>()
      ?? new SplitDataStep.ModelOptions();
    ConfusionMatrixOptions =
      configuration.GetSection("Flowthru:Flows:Reporting:ConfusionMatrixOptions").Get<CreateConfusionMatrixStep.Options>()
      ?? new CreateConfusionMatrixStep.Options();
  }
}
