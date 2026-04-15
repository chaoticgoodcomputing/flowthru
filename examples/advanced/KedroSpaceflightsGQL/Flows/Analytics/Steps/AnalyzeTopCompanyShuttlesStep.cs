using Flowthru.Core.Steps;
using KedroSpaceflightsGQL.Data._08_Reporting.Schemas;
using KedroSpaceflightsGQL.Infra.GqlClient;

namespace KedroSpaceflightsGQL.Flows.Analytics.Steps;

/// <summary>
/// Computes a fleet summary for the top-rated company from its filtered shuttle data.
/// </summary>
/// <remarks>
/// This step is a pure transform. It receives a plain <c>IEnumerable</c> of shuttle records
/// and produces a report. It has no awareness that the shuttle data was fetched via a
/// parameterized GQL query — the catalog layer handled all of that. The DAG ordering
/// (this step runs after <c>FindTopRatedCompany</c>) is enforced automatically by the
/// dependency analyzer inspecting <c>TopRatedCompanyShuttles</c>'s adapter dependencies.
/// </remarks>
[FlowthruStep]
public static class AnalyzeTopCompanyShuttlesStep
{
  /// <summary>
  /// Creates the transform that summarizes a company's shuttle fleet.
  /// </summary>
  public static Func<
    IEnumerable<IGetShuttlesByCompanyId_Shuttles>,
    TopRatedCompanyReport
  > Create() =>
    shuttles =>
    {
      var list = shuttles.ToList();

      var companyId = list.FirstOrDefault()?.CompanyId ?? "unknown";
      var shuttleCount = list.Count;
      var averagePrice = shuttleCount > 0 ? list.Average(s => s.Price) : 0m;
      var totalCapacity = list.Sum(s => s.PassengerCapacity);

      return new TopRatedCompanyReport
      {
        CompanyId = companyId,
        ShuttleCount = shuttleCount,
        AveragePrice = averagePrice,
        TotalPassengerCapacity = totalCapacity,
      };
    };
}
