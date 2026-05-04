using Flowthru.Core.Flows;
using SpaceflightsStagingSchema.Data;
using SpaceflightsStagingSchema.Flows.DataProcessing.Steps;

namespace SpaceflightsStagingSchema.Flows.DataProcessing;

/// <summary>
/// Reads raw filesystem inputs, parses each independently, and writes the typed
/// forms to the ephemeral staging database. Three preprocess steps run with no
/// inter-dependency — staging has no FK constraints, so load order doesn't
/// matter. Production is not touched by this flow.
/// </summary>
public static class DataProcessingFlow
{
  public static Flow Create(RawCatalog raw, StagingCatalog staging)
  {
    return FlowBuilder.CreateFlow(pipeline =>
    {
      pipeline.AddStep(
        label: "PreprocessCompanies",
        description: "Parses raw company records into typed records.",
        transform: PreprocessCompaniesStep.Create(),
        input: raw.Companies,
        output: staging.Companies
      );

      pipeline.AddStep(
        label: "PreprocessShuttles",
        description: "Parses raw shuttle records into typed records.",
        transform: PreprocessShuttlesStep.Create(),
        input: raw.Shuttles,
        output: staging.Shuttles
      );

      pipeline.AddStep(
        label: "PreprocessReviews",
        description: "Parses raw review records into typed records (decimal score).",
        transform: PreprocessReviewsStep.Create(),
        input: raw.Reviews,
        output: staging.Reviews
      );
    });
  }
}
