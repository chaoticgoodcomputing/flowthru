using Flowthru.Flow;
using SpaceflightsStagingSchema.Data;
using SpaceflightsStagingSchema.Data._01_Raw.Schemas;
using SpaceflightsStagingSchema.Data._02_Intermediate.Schemas;
using SpaceflightsStagingSchema.Flows.DataProcessing.Steps;

namespace SpaceflightsStagingSchema.Flows.DataProcessing;

/// <summary>
/// Reads raw filesystem inputs, parses each independently, optionally appends
/// synthetic rows for bulk-test scaling (per <see cref="SeedingOptions"/>),
/// and writes the combined typed form to the ephemeral staging schema.
/// </summary>
public static class DataProcessingFlow
{
  public static BuiltFlow Create(RawCatalog raw, StagingCatalog staging, FlowConfig config)
  {
    return FlowBuilder.CreateFlow("DataProcessing", pipeline =>
    {
      pipeline.AddStep<IEnumerable<CompanySchema>, IEnumerable<PreprocessedCompanySchema>>(
        label: "PreprocessCompanies",
        transform: PreprocessCompaniesStep.Create(config.SeedingOptions),
        input1: raw.Companies,
        output1: staging.Companies
      );

      pipeline.AddStep<IEnumerable<ShuttleSchema>, IEnumerable<PreprocessedShuttleSchema>>(
        label: "PreprocessShuttles",
        transform: PreprocessShuttlesStep.Create(config.SeedingOptions),
        input1: raw.Shuttles,
        output1: staging.Shuttles
      );

      pipeline.AddStep<IEnumerable<ReviewSchema>, IEnumerable<PreprocessedReviewSchema>>(
        label: "PreprocessReviews",
        transform: PreprocessReviewsStep.Create(config.SeedingOptions),
        input1: raw.Reviews,
        output1: staging.Reviews
      );
    });
  }
}
