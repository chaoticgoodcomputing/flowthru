using Flowthru.Core.Abstractions;

namespace KedroSpaceflightsGQL.Data._08_Reporting.Schemas;

/// <summary>
/// Summary report for the top-rated company's shuttle fleet.
/// Produced by the Analytics flow to demonstrate parameterized GQL catalog items.
/// </summary>
[FlowthruSchema]
public partial record TopRatedCompanyReport
{
  /// <summary>
  /// Identifier of the top-rated company.
  /// </summary>
  public required string CompanyId { get; init; }

  /// <summary>
  /// Number of shuttles operated by this company.
  /// </summary>
  public required int ShuttleCount { get; init; }

  /// <summary>
  /// Average trip price across the company's fleet.
  /// </summary>
  public required decimal AveragePrice { get; init; }

  /// <summary>
  /// Total passenger capacity across the company's entire fleet.
  /// </summary>
  public required int TotalPassengerCapacity { get; init; }
}
