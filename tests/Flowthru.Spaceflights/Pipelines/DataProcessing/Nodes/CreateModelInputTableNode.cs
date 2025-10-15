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
    : Node<ShuttleSchema, CompanySchema, ReviewRawSchema, ModelInputSchema>
{
  protected override Task<IEnumerable<ModelInputSchema>> TransformInternal(
      IEnumerable<ShuttleSchema> shuttles,
      IEnumerable<CompanySchema> companies,
      IEnumerable<ReviewRawSchema> reviews)
  {
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

          // Check if company exists
          if (!companyDict.ContainsKey(shuttle.CompanyId))
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
