using Flowthru.Core.Steps;
using KedroSpaceflightsGQL.Infra.GqlClient;

namespace KedroSpaceflightsGQL.Flows.Analytics.Steps;

/// <summary>
/// Identifies the company with the highest rating across all companies.
/// Produces the company ID string that parameterizes the downstream shuttle query.
/// </summary>
/// <remarks>
/// This step is a pure transform: it has no awareness that its output will be consumed
/// by a parameterized catalog entry. The catalog layer handles that wiring entirely.
/// </remarks>
[FlowthruStep]
public static class FindTopRatedCompanyStep
{
  /// <summary>
  /// Creates the transform that selects the highest-rated company's ID.
  /// </summary>
  /// <remarks>
  /// The <c>bool</c> first input is the <c>GqlDatabaseSeeded</c> gate; it is consumed
  /// only to express the DAG dependency on Ingest and is otherwise unused.
  /// </remarks>
  public static Func<(bool, IEnumerable<IGetCompanies_Companies>), string> Create() =>
    input =>
    {
      var (_, companies) = input;
      var top = companies.MaxBy(c => c.CompanyRating);

      if (top is null)
      {
        throw new InvalidOperationException(
          "Cannot determine top-rated company: the companies collection is empty."
        );
      }

      return top.Id;
    };
}
