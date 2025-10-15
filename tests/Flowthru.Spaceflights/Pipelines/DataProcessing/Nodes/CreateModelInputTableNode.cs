using System.ComponentModel.DataAnnotations;
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
    var shuttleDict = shuttles.ToDictionary(s => s.Id);
    var companyDict = companies.ToDictionary(c => c.Id);

    // Join reviews with shuttles and companies
    var modelInput = reviews
        .Where(review => !string.IsNullOrWhiteSpace(review.ShuttleId))
        .Where(review => shuttleDict.ContainsKey(review.ShuttleId))
        .Select(review =>
        {
          var shuttle = shuttleDict[review.ShuttleId];

          // Check if company exists (handle null CompanyIds gracefully)
          if (string.IsNullOrWhiteSpace(shuttle.CompanyId) || !companyDict.ContainsKey(shuttle.CompanyId))
            return null;

          var company = companyDict[shuttle.CompanyId];

          return new ModelInputSchema
          {
            ShuttleId = shuttle.Id,
            CompanyId = company.Id,
            CompanyLocation = company.CompanyLocation,
            ShuttleType = shuttle.ShuttleType,
            Engines = shuttle.Engines,
            PassengerCapacity = shuttle.PassengerCapacity,
            Crew = shuttle.Crew,
            DCheckComplete = shuttle.DCheckComplete,
            MoonClearanceComplete = shuttle.MoonClearanceComplete,
            IataApproved = company.IataApproved,
            CompanyRating = company.CompanyRating,
            ReviewScoresRating = ParseDecimal(review.ReviewScoresRating),
            Price = shuttle.Price
          };
        })
        .Where(row => row != null)
        .Select(row => row!)
        // Drop rows with missing critical data
        .Where(row => row.ReviewScoresRating.HasValue)
        .Where(row => row.Engines.HasValue)
        .Where(row => row.PassengerCapacity.HasValue)
        .Where(row => row.Crew.HasValue);

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
