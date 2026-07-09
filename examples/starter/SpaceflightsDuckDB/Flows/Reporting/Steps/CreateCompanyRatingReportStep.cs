using Flowthru.Step;
using Flowthru.Step.Testing;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using SpaceflightsDuckDB.Data._08_Reporting.Schemas;

namespace SpaceflightsDuckDB.Flows.Reporting.Steps;

/// <summary>
/// Formats the per-company summaries into a small top-rated-companies report.
/// </summary>
/// <remarks>
/// Everything here is narrow, per-row work — filtering, rounding, ranking a
/// handful of rows — so it stays an ordinary C# step. The wide aggregation that
/// produced the summaries already ran engine-side.
/// </remarks>
[FlowthruStep]
public static class CreateCompanyRatingReportStep
{
  /// <summary>Companies need at least this many rated shuttles to be ranked.</summary>
  private const int MinShuttleCount = 3;

  /// <summary>How many companies the report keeps.</summary>
  private const int ReportSize = 10;

  /// <summary>
  /// Creates a function that ranks companies by average review score and keeps the top entries.
  /// </summary>
  /// <returns>
  /// A function that produces <see cref="CompanyRatingReport"/> records for the
  /// top <see cref="ReportSize"/> companies with at least <see cref="MinShuttleCount"/>
  /// rated shuttles.
  /// </returns>
  public static Func<
    IEnumerable<CompanySummarySchema>,
    IEnumerable<CompanyRatingReport>
  > Create(ILogger logger)
  {
    return (input) =>
    {
      var summaries = input.ToList();
      var eligible = summaries
        .Where(s => s.ShuttleCount >= MinShuttleCount)
        .ToList();

      var report = eligible
        .OrderByDescending(s => s.AvgReviewScore)
        .ThenBy(s => s.CompanyId)
        .Take(ReportSize)
        .Select((s, index) => new CompanyRatingReport
        {
          Rank = index + 1,
          CompanyId = s.CompanyId,
          ShuttleCount = s.ShuttleCount,
          AvgReviewScore = Math.Round(s.AvgReviewScore, 1),
          AvgPrice = Math.Round(s.AvgPrice, 2),
        })
        .ToList();

      logger.LogInformation(
        "Ranked the top {Ranked} of {Eligible} companies with at least {Min} rated shuttles "
        + "({Total} companies summarized)",
        report.Count, eligible.Count, MinShuttleCount, summaries.Count
      );

      return report;
    };
  }

#if FUNIT_ENABLED
  /// <summary>FUnit tests for <see cref="CreateCompanyRatingReportStep"/>.</summary>
  public class Tests : FUnitContext
  {
    private static CompanySummarySchema Summary(
      string companyId, long shuttleCount, double avgScore) =>
      new()
      {
        CompanyId = companyId,
        ShuttleCount = shuttleCount,
        AvgPrice = 1500.0,
        AvgReviewScore = avgScore,
        TotalPassengerCapacity = shuttleCount * 4,
      };

    /// <summary>
    /// Companies below the minimum shuttle count should not be ranked.
    /// </summary>
    [FUnitStepTest(typeof(CreateCompanyRatingReportStep))]
    public void BelowMinimumShuttleCount_Excluded()
    {
      // Arrange
      var summaries = Samples.Of(
        Summary("C1", 5, 92.0),
        Summary("C2", 1, 99.0));

      // Apply
      var result = Invoke(Create(NullLogger.Instance), summaries).ToList();

      // Assert
      Assert.That(result, Has.Count.EqualTo(1));
      Assert.That(result[0].CompanyId, Is.EqualTo("C1"));
      Assert.That(result[0].Rank, Is.EqualTo(1));
    }

    /// <summary>
    /// Ranks should follow descending average review score.
    /// </summary>
    [FUnitStepTest(typeof(CreateCompanyRatingReportStep))]
    public void Ranks_FollowDescendingScore()
    {
      // Arrange
      var summaries = Samples.Of(
        Summary("C1", 4, 88.0),
        Summary("C2", 4, 95.0));

      // Apply
      var result = Invoke(Create(NullLogger.Instance), summaries).ToList();

      // Assert
      Assert.That(result.Select(r => (r.Rank, r.CompanyId)), Is.EqualTo(new[]
      {
        (1, "C2"), (2, "C1"),
      }));
    }
  }
#endif
}
