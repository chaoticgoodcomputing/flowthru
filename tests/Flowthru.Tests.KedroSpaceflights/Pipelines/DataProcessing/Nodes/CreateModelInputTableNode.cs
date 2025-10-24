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
    : NodeBase<CreateModelInputTableInputs, ModelInputSchema> {
  protected override Task<IEnumerable<ModelInputSchema>> Transform(
      IEnumerable<CreateModelInputTableInputs> inputs) {
    // Extract the singleton input containing all preprocessed catalog data
    var input = inputs.Single();

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

#region Node Artifacts (Colocated)

// Following FlowThru's artifact colocation policy:
// Node-specific types (input/output schemas) are defined with the node that uses them.
// This mirrors the React Props pattern where component-specific types live with the component.
// Pure domain schemas (catalog entry types) remain in Data/Schemas/.

/// <summary>
/// Multi-input schema for CreateModelInputTableNode.
/// Bundles three catalog entries (shuttles, companies, reviews) for join operation.
/// All inputs are preprocessed and validated - no null handling required.
/// </summary>
/// <remarks>
/// Properties will be mapped to catalog entries at pipeline registration time
/// using CatalogMap&lt;T&gt; to maintain separation of concerns:
/// - Schema layer: Pure data shape definitions
/// - Catalog layer: Data storage/naming bindings
/// </remarks>
public record CreateModelInputTableInputs {
  /// <summary>
  /// Preprocessed shuttle data (validated, non-nullable fields)
  /// </summary>
  [Required]
  public IEnumerable<ShuttleSchema> Shuttles { get; init; } = null!;

  /// <summary>
  /// Preprocessed company data (validated, non-nullable fields)
  /// </summary>
  [Required]
  public IEnumerable<CompanySchema> Companies { get; init; } = null!;

  /// <summary>
  /// Preprocessed review data (validated, non-nullable fields)
  /// </summary>
  [Required]
  public IEnumerable<ReviewSchema> Reviews { get; init; } = null!;
}

#endregion
