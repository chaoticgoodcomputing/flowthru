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

    // Create dictionaries for efficient lookup
    var companyDict = companies.ToDictionary(c => c.Id);

    // Diagnostic: Check review shuttle IDs (before filtering)
    var allShuttlesDict = shuttles.ToDictionary(s => s.Id);
    var reviewCount = reviews.Count();
    var validReviewShuttleIds = reviews
        .Where(r => !string.IsNullOrWhiteSpace(r.ShuttleId))
        .Where(r => allShuttlesDict.ContainsKey(r.ShuttleId))
        .Count();
    if (validReviewShuttleIds == 0 && reviewCount > 0)
    {
      // Sample some IDs to see what's wrong
      var sampleReviewIds = reviews.Take(5).Select(r => r.ShuttleId).ToList();
      var sampleShuttleIds = shuttles.Take(5).Select(s => s.Id).ToList(); Console.WriteLine($"=== Sample shuttle ids: {string.Join(", ", sampleShuttleIds)} ===");
    }

    // Join reviews with shuttles and companies
    // Apply pandas-style dropna() to filter out rows with ANY null values
    var validShuttles = shuttles.DropNa().ToDictionary(s => s.Id);
    var validReviews = reviews.DropNa();
    var validCompanies = companies.DropNa();
    var validCompanyDict = validCompanies.ToDictionary(c => c.Id);

    var modelInput = validReviews
        .Where(review => validShuttles.ContainsKey(review.ShuttleId))
        .Select(review =>
        {
          var shuttle = validShuttles[review.ShuttleId];

          // Skip if company doesn't exist or is invalid
          if (!validCompanyDict.ContainsKey(shuttle.CompanyId))
            return null;

          var company = validCompanyDict[shuttle.CompanyId];

          return new ModelInputSchema
          {
            // Shuttle columns (matching Kedro's column order)
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
            ReviewScoresRating = ParseDecimal(review.ReviewScoresRating),
            ReviewScoresComfort = ParseDecimal(review.ReviewScoresComfort),
            ReviewScoresAmenities = ParseDecimal(review.ReviewScoresAmenities),
            ReviewScoresTrip = ParseDecimal(review.ReviewScoresTrip),
            ReviewScoresCrew = ParseDecimal(review.ReviewScoresCrew),
            ReviewScoresLocation = ParseDecimal(review.ReviewScoresLocation),
            ReviewScoresPrice = ParseDecimal(review.ReviewScoresPrice),
            NumberOfReviews = ParseInt(review.NumberOfReviews),
            ReviewsPerMonth = ParseDecimal(review.ReviewsPerMonth),

            // Company columns
            Id = company.Id,
            CompanyRating = company.CompanyRating,
            CompanyLocation = company.CompanyLocation,
            TotalFleetCount = company.TotalFleetCount,
            IataApproved = company.IataApproved
          };
        })
        .Where(row => row != null)
        .Select(row => row!)
        // Final dropna() on the merged result (matching Kedro's behavior)
        .DropNa();

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
