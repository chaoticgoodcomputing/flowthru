using Flowthru.Core.Steps;
using Flowthru.Misc.DataFrames;
using KedroSpaceflightsSpark.Data._02_Intermediate.Schemas;
using KedroSpaceflightsSpark.Data._03_Primary.Schemas;

namespace KedroSpaceflightsSpark.Flows.DataProcessing.Steps;

/// <summary>
/// Joins the three preprocessed TypedFrames using Spark distributed joins to produce
/// the unified model input table.
///
/// Join order:
///   1. Shuttles ⋈ ParsedReviews   (on shuttle.Id = review.ShuttleId)
///   2. Result   ⋈ Companies        (on shuttleReview.CompanyId = company.Id)
///
/// The result is a TypedFrame<ModelInputTableSchema> stored in a memory catalog item.
/// The execution plan remains deferred — no Spark action is triggered here.
/// Downstream steps (SplitDataStep, RankShuttlesByPriceStep) can continue to apply
/// Spark operations before any materialization occurs.
/// </summary>
[FlowthruStep]
public static class CreateModelInputTableStep
{
  public static Func<
    (
      TypedFrame<PreprocessedShuttleSchema>,
      TypedFrame<PreprocessedCompanySchema>,
      TypedFrame<ParsedReviewSchema>
    ),
    TypedFrame<ModelInputTableSchema>
  > Create()
  {
    return (input) =>
    {
      var (shuttles, companies, reviews) = input;

      // Step 1: Join shuttles with parsed reviews on shuttle.Id = review.ShuttleId
      var shuttlesWithReviews = shuttles.Join(
        reviews,
        s => s.Id,
        r => r.ShuttleId,
        (s, r) =>
          new ShuttleReviewSchema
          {
            ShuttleId = s.Id,
            ShuttleType = s.ShuttleType,
            CompanyId = s.CompanyId,
            Engines = s.Engines,
            PassengerCapacity = s.PassengerCapacity,
            Crew = s.Crew,
            Price = s.Price,
            DCheckComplete = s.DCheckComplete,
            MoonClearanceComplete = s.MoonClearanceComplete,
            ReviewScoresRating = r.ReviewScoresRating,
          }
      );

      // Step 2: Join with companies on shuttleReview.CompanyId = company.Id
      return shuttlesWithReviews.Join(
        companies,
        sr => sr.CompanyId,
        c => c.Id,
        (sr, c) =>
          new ModelInputTableSchema
          {
            ShuttleId = sr.ShuttleId,
            ShuttleType = sr.ShuttleType,
            CompanyId = sr.CompanyId,
            Engines = sr.Engines,
            PassengerCapacity = sr.PassengerCapacity,
            Crew = sr.Crew,
            DCheckComplete = sr.DCheckComplete,
            MoonClearanceComplete = sr.MoonClearanceComplete,
            Price = sr.Price,
            IataApproved = c.IataApproved,
            CompanyRating = c.CompanyRating,
            ReviewScoresRating = sr.ReviewScoresRating,
          }
      );
    };
  }
}
