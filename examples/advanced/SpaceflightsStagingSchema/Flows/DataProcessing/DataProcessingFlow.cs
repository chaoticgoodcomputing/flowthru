using Flowthru.Core.Flows;
using SpaceflightsStagingSchema.Data;
using SpaceflightsStagingSchema.Flows.DataProcessing.Steps;

namespace SpaceflightsStagingSchema.Flows.DataProcessing;

/// <summary>
/// Reads raw filesystem inputs, parses each independently, optionally appends
/// synthetic rows for bulk-test scaling (per <see cref="SeedingOptions"/>),
/// and writes the combined typed form to the ephemeral staging schema. Three
/// preprocess steps run with no inter-dependency — staging has no FK
/// constraints, so load order doesn't matter. Production is not touched by
/// this flow.
/// </summary>
public static class DataProcessingFlow
{
  public static Flow Create(RawCatalog raw, StagingCatalog staging, FlowConfig config)
  {
    return FlowBuilder.CreateFlow(pipeline =>
    {
      pipeline.AddStep(
        label: "PreprocessCompanies",
        description: "Parses raw company records into typed records and appends synthetic rows.",
        transform: PreprocessCompaniesStep.Create(),
        input: (raw.Companies, config.SeedingOptions),
        output: staging.Companies
      );

      pipeline.AddStep(
        label: "PreprocessShuttles",
        description: "Parses raw shuttle records into typed records and appends synthetic rows.",
        transform: PreprocessShuttlesStep.Create(),
        input: (raw.Shuttles, config.SeedingOptions),
        output: staging.Shuttles
      );

      pipeline.AddStep(
        label: "PreprocessReviews",
        description: "Parses raw review records into typed records and appends synthetic rows.",
        transform: PreprocessReviewsStep.Create(),
        input: (raw.Reviews, config.SeedingOptions),
        output: staging.Reviews
      );
    });
  }
}
