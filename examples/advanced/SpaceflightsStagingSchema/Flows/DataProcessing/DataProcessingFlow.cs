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
  public static BuiltFlow Create(RawCatalog raw, StagingCatalog staging)
  {
    return FlowBuilder.CreateFlow("DataProcessing", pipeline =>
    {
      pipeline.AddStep<
        IEnumerable<CompanySchema>,
        SeedingOptions,
        IEnumerable<PreprocessedCompanySchema>
      >(
        label: "PreprocessCompanies",
        transform: PreprocessCompaniesStep.Create(),
        inputs: (raw.Companies, raw.SeedingOptions),
        outputs: staging.Companies
      );

      pipeline.AddStep<
        IEnumerable<ShuttleSchema>,
        SeedingOptions,
        IEnumerable<PreprocessedShuttleSchema>
      >(
        label: "PreprocessShuttles",
        transform: PreprocessShuttlesStep.Create(),
        inputs: (raw.Shuttles, raw.SeedingOptions),
        outputs: staging.Shuttles
      );

      pipeline.AddStep<
        IEnumerable<ReviewSchema>,
        SeedingOptions,
        IEnumerable<PreprocessedReviewSchema>
      >(
        label: "PreprocessReviews",
        transform: PreprocessReviewsStep.Create(),
        inputs: (raw.Reviews, raw.SeedingOptions),
        outputs: staging.Reviews
      );
    });
  }
}
