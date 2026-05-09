using Flowthru.Flow;
using SpaceflightsStagingSchema.Data;
using SpaceflightsStagingSchema.Data._02_Intermediate.Schemas;
using SpaceflightsStagingSchema.Flows.Promotion.Steps;

namespace SpaceflightsStagingSchema.Flows.Promotion;

/// <summary>
/// Promotes the three preprocessed source tables from the ephemeral staging
/// database into the FK-constrained production database.
/// </summary>
public static class PromotionFlow
{
  public static BuiltFlow Create(StagingCatalog staging, ProductionCatalog production)
  {
    return FlowBuilder.CreateFlow("Promotion", pipeline =>
    {
      pipeline.AddStep<IEnumerable<PreprocessedCompanySchema>, IEnumerable<PreprocessedCompanySchema>>(
        label: "PromoteCompanies",
        transform: PromoteCompaniesStep.Create(),
        input1: staging.Companies,
        output1: production.Companies
      );

      pipeline.AddStep<
        IEnumerable<PreprocessedShuttleSchema>,
        IEnumerable<PreprocessedCompanySchema>,
        IEnumerable<PreprocessedShuttleSchema>
      >(
        label: "PromoteShuttles",
        transform: PromoteShuttlesStep.Create(),
        input1: staging.Shuttles,
        input2: production.Companies,
        output1: production.Shuttles
      );

      pipeline.AddStep<
        IEnumerable<PreprocessedReviewSchema>,
        IEnumerable<PreprocessedShuttleSchema>,
        IEnumerable<PreprocessedReviewSchema>
      >(
        label: "PromoteReviews",
        transform: PromoteReviewsStep.Create(),
        input1: staging.Reviews,
        input2: production.Shuttles,
        output1: production.Reviews
      );
    });
  }
}
