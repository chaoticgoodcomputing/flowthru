using System.ComponentModel.DataAnnotations;
using Flowthru.Nodes;
using Flowthru.Tests.KedroSpaceflights.Data.Schemas.Processed;

namespace Flowthru.Tests.KedroSpaceflights.Pipelines.DataProcessing.Nodes;

/// <summary>
/// Combines preprocessed shuttles, companies, and reviews into a single model input table.
/// Performs inner joins on validated, non-nullable data.
/// All null filtering is handled upstream in preprocessing nodes.
/// 
/// Stateless node with implicit parameterless constructor,
/// compatible with type reference instantiation for distributed/parallel execution.
/// </summary>
public class CreateModelInputTableNode
    : NodeBase<
      (IEnumerable<ShuttleSchema> Shuttles,
       IEnumerable<CompanySchema> Companies,
       IEnumerable<ReviewSchema> Reviews),
      IEnumerable<ModelInputSchema>,
      NoParams> {
  protected override Task<IEnumerable<ModelInputSchema>> Transform(
      (IEnumerable<ShuttleSchema> Shuttles,
       IEnumerable<CompanySchema> Companies,
       IEnumerable<ReviewSchema> Reviews) input) {
    // Perform inner joins using LINQ: reviews → shuttles → companies
    // This is more memory-efficient than creating lookup dictionaries
    var modelInput = input.Reviews
        .Join(
            input.Shuttles,
            review => review.ShuttleId,
            shuttle => shuttle.Id,
            (review, shuttle) => new { Review = review, Shuttle = shuttle })
        .Join(
            input.Companies,
            rs => rs.Shuttle.CompanyId,
            company => company.Id,
            (rs, company) => new ModelInputSchema {
              // Shuttle columns
              ShuttleLocation = rs.Shuttle.ShuttleLocation,
              ShuttleType = rs.Shuttle.ShuttleType,
              EngineType = rs.Shuttle.EngineType,
              EngineVendor = rs.Shuttle.EngineVendor,
              Engines = rs.Shuttle.Engines,
              PassengerCapacity = rs.Shuttle.PassengerCapacity,
              CancellationPolicy = rs.Shuttle.CancellationPolicy,
              Crew = rs.Shuttle.Crew,
              DCheckComplete = rs.Shuttle.DCheckComplete,
              MoonClearanceComplete = rs.Shuttle.MoonClearanceComplete,
              Price = rs.Shuttle.Price,
              CompanyId = rs.Shuttle.CompanyId,
              ShuttleId = rs.Shuttle.Id,

              // Review columns
              ReviewScoresRating = rs.Review.ReviewScoresRating,
              ReviewScoresComfort = rs.Review.ReviewScoresComfort,
              ReviewScoresAmenities = rs.Review.ReviewScoresAmenities,
              ReviewScoresTrip = rs.Review.ReviewScoresTrip,
              ReviewScoresCrew = rs.Review.ReviewScoresCrew,
              ReviewScoresLocation = rs.Review.ReviewScoresLocation,
              ReviewScoresPrice = rs.Review.ReviewScoresPrice,
              NumberOfReviews = rs.Review.NumberOfReviews,
              ReviewsPerMonth = rs.Review.ReviewsPerMonth,

              // Company columns
              Id = company.Id,
              CompanyRating = company.CompanyRating,
              CompanyLocation = company.CompanyLocation,
              TotalFleetCount = company.TotalFleetCount,
              IataApproved = company.IataApproved
            });

    return Task.FromResult(modelInput);
  }
}
