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

    // Create lookup dictionaries for join operations
    // All data is already validated and non-nullable from preprocessing nodes
    var shuttles = input.Shuttles.ToDictionary(s => s.Id);
    var companies = input.Companies.ToDictionary(c => c.Id);

    // Perform inner joins: reviews → shuttles → companies
    var modelInput = input.Reviews
        .Where(review => shuttles.ContainsKey(review.ShuttleId))
        .Select(review => {
          var shuttle = shuttles[review.ShuttleId];

          // Skip if company doesn't exist (inner join semantics)
          if (!companies.ContainsKey(shuttle.CompanyId)) {
            return null;
          }

          var company = companies[shuttle.CompanyId];

          // All fields are non-nullable - direct assignment with no .Value calls
          return new ModelInputSchema {
            // Shuttle columns
            ShuttleLocation = shuttle.ShuttleLocation,
            ShuttleType = shuttle.ShuttleType,
            EngineType = shuttle.EngineType,
            EngineVendor = shuttle.EngineVendor,
            Engines = shuttle.Engines,
            PassengerCapacity = shuttle.PassengerCapacity,
            CancellationPolicy = shuttle.CancellationPolicy,
            Crew = shuttle.Crew,
            DCheckComplete = shuttle.DCheckComplete,
            MoonClearanceComplete = shuttle.MoonClearanceComplete,
            Price = shuttle.Price,
            CompanyId = shuttle.CompanyId,
            ShuttleId = shuttle.Id,

            // Review columns
            ReviewScoresRating = review.ReviewScoresRating,
            ReviewScoresComfort = review.ReviewScoresComfort,
            ReviewScoresAmenities = review.ReviewScoresAmenities,
            ReviewScoresTrip = review.ReviewScoresTrip,
            ReviewScoresCrew = review.ReviewScoresCrew,
            ReviewScoresLocation = review.ReviewScoresLocation,
            ReviewScoresPrice = review.ReviewScoresPrice,
            NumberOfReviews = review.NumberOfReviews,
            ReviewsPerMonth = review.ReviewsPerMonth,

            // Company columns
            Id = company.Id,
            CompanyRating = company.CompanyRating,
            CompanyLocation = company.CompanyLocation,
            TotalFleetCount = company.TotalFleetCount,
            IataApproved = company.IataApproved
          };
        })
        .Where(row => row != null)
        .Select(row => row!);

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
