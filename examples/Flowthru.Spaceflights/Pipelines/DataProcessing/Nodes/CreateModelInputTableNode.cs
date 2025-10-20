using System.ComponentModel.DataAnnotations;
using Flowthru.Meta.Extensions;
using Flowthru.Nodes;
using Flowthru.Spaceflights.Data.Schemas.Raw;
using Flowthru.Spaceflights.Data.Schemas.Processed;

namespace Flowthru.Spaceflights.Pipelines.DataProcessing.Nodes;

/// <summary>
/// Combines preprocessed shuttles, companies, and raw reviews into a single model input table.
/// Performs inner joins and drops rows with missing data.
/// 
/// Stateless node with implicit parameterless constructor,
/// compatible with type reference instantiation for distributed/parallel execution.
/// </summary>
public class CreateModelInputTableNode
    : NodeBase<CreateModelInputTableInputs, ModelInputSchema>
{
  protected override Task<IEnumerable<ModelInputSchema>> Transform(
      IEnumerable<CreateModelInputTableInputs> inputs)
  {
    // Extract the singleton input containing all catalog data
    var input = inputs.Single();
    var shuttles = input.Shuttles;
    var companies = input.Companies;
    var reviews = input.Reviews;

    // Filter shuttles: keep only rows with all required non-null fields
    var validShuttles = shuttles
        .Where(s => s.Engines.HasValue
                 && s.PassengerCapacity.HasValue
                 && s.Crew.HasValue
                 && !string.IsNullOrWhiteSpace(s.Id)
                 && !string.IsNullOrWhiteSpace(s.CompanyId)
                 && !string.IsNullOrWhiteSpace(s.ShuttleLocation)
                 && !string.IsNullOrWhiteSpace(s.ShuttleType)
                 && !string.IsNullOrWhiteSpace(s.EngineType)
                 && !string.IsNullOrWhiteSpace(s.EngineVendor)
                 && !string.IsNullOrWhiteSpace(s.CancellationPolicy))
        .ToDictionary(s => s.Id);

    // Filter companies: keep only rows with all required non-null fields
    var validCompanies = companies
        .Where(c => c.CompanyRating.HasValue
                 && c.TotalFleetCount.HasValue
                 && !string.IsNullOrWhiteSpace(c.Id)
                 && !string.IsNullOrWhiteSpace(c.CompanyLocation))
        .ToDictionary(s => s.Id);

    // Filter reviews and parse string fields to decimals/ints
    var validReviews = reviews
        .Where(r => !string.IsNullOrWhiteSpace(r.ShuttleId))
        .Select(r => new
        {
          ShuttleId = r.ShuttleId,
          ReviewScoresRating = ParseDecimal(r.ReviewScoresRating),
          ReviewScoresComfort = ParseDecimal(r.ReviewScoresComfort),
          ReviewScoresAmenities = ParseDecimal(r.ReviewScoresAmenities),
          ReviewScoresTrip = ParseDecimal(r.ReviewScoresTrip),
          ReviewScoresCrew = ParseDecimal(r.ReviewScoresCrew),
          ReviewScoresLocation = ParseDecimal(r.ReviewScoresLocation),
          ReviewScoresPrice = ParseDecimal(r.ReviewScoresPrice),
          NumberOfReviews = ParseInt(r.NumberOfReviews),
          ReviewsPerMonth = ParseDecimal(r.ReviewsPerMonth)
        })
        .Where(r => r.ReviewScoresRating.HasValue
                 && r.ReviewScoresComfort.HasValue
                 && r.ReviewScoresAmenities.HasValue
                 && r.ReviewScoresTrip.HasValue
                 && r.ReviewScoresCrew.HasValue
                 && r.ReviewScoresLocation.HasValue
                 && r.ReviewScoresPrice.HasValue
                 && r.NumberOfReviews.HasValue
                 && r.ReviewsPerMonth.HasValue);

    // Join reviews with shuttles and companies, creating ModelInputSchema with non-nullable values
    var modelInput = validReviews
        .Where(review => validShuttles.ContainsKey(review.ShuttleId))
        .Select(review =>
        {
          var shuttle = validShuttles[review.ShuttleId];

          // Skip if company doesn't exist
          if (!validCompanies.ContainsKey(shuttle.CompanyId))
            return null;

          var company = validCompanies[shuttle.CompanyId];

          return new ModelInputSchema
          {
            // Shuttle columns (matching Kedro's column order)
            ShuttleLocation = shuttle.ShuttleLocation!,
            ShuttleType = shuttle.ShuttleType!,
            EngineType = shuttle.EngineType!,
            EngineVendor = shuttle.EngineVendor!,
            Engines = shuttle.Engines!.Value,
            PassengerCapacity = shuttle.PassengerCapacity!.Value,
            CancellationPolicy = shuttle.CancellationPolicy!,
            Crew = shuttle.Crew!.Value,
            DCheckComplete = shuttle.DCheckComplete,
            MoonClearanceComplete = shuttle.MoonClearanceComplete,
            Price = shuttle.Price,
            CompanyId = shuttle.CompanyId!,
            ShuttleId = shuttle.Id,

            // Review columns (already validated as non-null)
            ReviewScoresRating = review.ReviewScoresRating!.Value,
            ReviewScoresComfort = review.ReviewScoresComfort!.Value,
            ReviewScoresAmenities = review.ReviewScoresAmenities!.Value,
            ReviewScoresTrip = review.ReviewScoresTrip!.Value,
            ReviewScoresCrew = review.ReviewScoresCrew!.Value,
            ReviewScoresLocation = review.ReviewScoresLocation!.Value,
            ReviewScoresPrice = review.ReviewScoresPrice!.Value,
            NumberOfReviews = review.NumberOfReviews!.Value,
            ReviewsPerMonth = review.ReviewsPerMonth!.Value,

            // Company columns
            Id = company.Id,
            CompanyRating = company.CompanyRating!.Value,
            CompanyLocation = company.CompanyLocation!,
            TotalFleetCount = company.TotalFleetCount!.Value,
            IataApproved = company.IataApproved
          };
        })
        .Where(row => row != null)
        .Select(row => row!);

    return Task.FromResult(modelInput);
  }

  /// <summary>
  /// Parses decimal from string, returns null if empty/invalid
  /// </summary>
  private static decimal? ParseDecimal(string? value)
  {
    if (string.IsNullOrWhiteSpace(value))
      return null;

    if (decimal.TryParse(value, out var result))
      return result;

    return null;
  }

  /// <summary>
  /// Parses integer from string, returns null if empty/invalid
  /// </summary>
  private static int? ParseInt(string? value)
  {
    if (string.IsNullOrWhiteSpace(value))
      return null;

    if (int.TryParse(value, out var result))
      return result;

    return null;
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
/// </summary>
/// <remarks>
/// Properties will be mapped to catalog entries at pipeline registration time
/// using CatalogMap&lt;T&gt; to maintain separation of concerns:
/// - Schema layer: Pure data shape definitions
/// - Catalog layer: Data storage/naming bindings
/// </remarks>
public record CreateModelInputTableInputs
{
  /// <summary>
  /// Preprocessed shuttle data
  /// </summary>
  [Required]
  public IEnumerable<ShuttleSchema> Shuttles { get; init; } = null!;

  /// <summary>
  /// Preprocessed company data
  /// </summary>
  [Required]
  public IEnumerable<CompanySchema> Companies { get; init; } = null!;

  /// <summary>
  /// Raw review data
  /// </summary>
  [Required]
  public IEnumerable<ReviewRawSchema> Reviews { get; init; } = null!;
}

#endregion
