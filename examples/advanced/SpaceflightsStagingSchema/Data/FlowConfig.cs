using Flowthru.Core.Data;
using SpaceflightsStagingSchema.Flows.DataProcessing;
using SpaceflightsStagingSchema.Flows.DataScience.Steps;
using SpaceflightsStagingSchema.Flows.Reporting.Steps;

namespace SpaceflightsStagingSchema.Data;

/// <summary>
/// Configuration catalog for the SpaceflightsStagingSchema pipeline.
/// Properties are bound from appsettings.json via the source-generated constructor.
/// </summary>
[FlowthruConfig]
public partial class FlowConfig
{
  /// <summary>
  /// Synthetic-data seeding knobs. Set the per-source counts to non-zero
  /// values to scale the pipeline up to bulk-test volumes.
  /// </summary>
  [ConfigSection("Flowthru:Flows:DataProcessing:Seeding")]
  public partial IItem<SeedingOptions> SeedingOptions { get; }

  [ConfigSection("Flowthru:Flows:DataScience:ModelOptions")]
  public partial IItem<SplitDataStep.ModelOptions> ModelOptions { get; }

  [ConfigSection("Flowthru:Flows:Reporting:ConfusionMatrixOptions")]
  public partial IItem<CreateConfusionMatrixStep.Options> ConfusionMatrixOptions { get; }
}
