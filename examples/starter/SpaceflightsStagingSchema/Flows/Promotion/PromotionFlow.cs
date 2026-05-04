using Flowthru.Core.Flows;
using SpaceflightsStagingSchema.Data;
using SpaceflightsStagingSchema.Flows.Promotion.Steps;

namespace SpaceflightsStagingSchema.Flows.Promotion;

/// <summary>
/// Promotes the three preprocessed source tables from the ephemeral staging
/// database into the FK-constrained production database. The DAG order matters
/// because production enforces referential integrity:
/// <list type="number">
///   <item>Companies (no FK dependency)</item>
///   <item>Shuttles (FK on CompanyId → Companies.Id)</item>
///   <item>Reviews (FK on ShuttleId → Shuttles.Id)</item>
/// </list>
/// Each step is identity-shaped; the work is done by the cross-catalog write
/// going through production's constraint enforcement.
/// </summary>
public static class PromotionFlow
{
  public static Flow Create(StagingCatalog staging, ProductionCatalog production)
  {
    return FlowBuilder.CreateFlow(pipeline =>
    {
      pipeline.AddStep(
        label: "PromoteCompanies",
        description: "Copies preprocessed companies from staging into production (PK enforced).",
        transform: PromoteCompaniesStep.Create(),
        input: staging.Companies,
        output: production.Companies
      );

      pipeline.AddStep(
        label: "PromoteShuttles",
        description: "Copies preprocessed shuttles from staging into production. Depends on PromoteCompanies for FK CompanyId integrity.",
        transform: PromoteShuttlesStep.Create(),
        // production.Companies appears as a second input solely so the DAG
        // enforces Companies-before-Shuttles ordering for FK integrity. The
        // step body ignores that input.
        input: (staging.Shuttles, production.Companies),
        output: production.Shuttles
      );

      pipeline.AddStep(
        label: "PromoteReviews",
        description: "Copies preprocessed reviews from staging into production. Depends on PromoteShuttles for FK ShuttleId integrity.",
        transform: PromoteReviewsStep.Create(),
        input: (staging.Reviews, production.Shuttles),
        output: production.Reviews
      );
    });
  }
}
