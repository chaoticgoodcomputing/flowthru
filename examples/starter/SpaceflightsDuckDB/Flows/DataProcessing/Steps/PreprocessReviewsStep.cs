using Flowthru.Step;
using Flowthru.Step.Testing;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using SpaceflightsDuckDB.Data._01_Raw.Schemas;
using SpaceflightsDuckDB.Data._02_Intermediate.Schemas;

namespace SpaceflightsDuckDB.Flows.DataProcessing.Steps;

/// <summary>
/// Preprocesses raw review data by parsing rating scores into strongly-typed values.
/// </summary>
/// <remarks>
/// Per-row parsing like this belongs in an ordinary C# step: each output row depends
/// on exactly one input row, so there is nothing for an engine-side SQL step to gain.
/// The typed Parquet output of this step is what the SQL join consumes.
/// </remarks>
[FlowthruStep]
public static class PreprocessReviewsStep
{
  /// <summary>
  /// Creates a preprocessing function that transforms raw review records into strongly-typed records.
  /// </summary>
  /// <returns>
  /// A function that converts <see cref="ReviewSchema"/> records to <see cref="PreprocessedReviewSchema"/> records.
  /// Records with non-numeric rating scores are filtered out.
  /// </returns>
  public static Func<IEnumerable<ReviewSchema>, IEnumerable<PreprocessedReviewSchema>> Create(
    ILogger logger)
  {
    return (input) =>
    {
      var rows = input.ToList();
      var processed = rows
        .Select(raw => Parse(raw))
        .Where(item => item != null)
        .Cast<PreprocessedReviewSchema>()
        .ToList();

      var dropped = rows.Count - processed.Count;
      if (dropped > 0)
      {
        logger.LogWarning(
          "Dropped {Dropped}/{Total} review rows with non-numeric rating scores",
          dropped, rows.Count
        );
      }
      else
      {
        logger.LogInformation("Preprocessed {Count} review rows", processed.Count);
      }

      return processed;
    };
  }

  /// <summary>
  /// Parses a raw review record into a preprocessed record with a strongly-typed score.
  /// </summary>
  /// <param name="raw">The raw review record to parse.</param>
  /// <returns>
  /// A <see cref="PreprocessedReviewSchema"/> if the score parses; otherwise, <c>null</c>.
  /// </returns>
  private static PreprocessedReviewSchema? Parse(ReviewSchema raw)
  {
    if (!decimal.TryParse(raw.ReviewScoresRating, out var score))
    {
      return null;
    }

    return new PreprocessedReviewSchema
    {
      ShuttleId = raw.ShuttleId,
      ReviewScoresRating = score,
    };
  }

#if FUNIT_ENABLED
  /// <summary>FUnit tests for <see cref="PreprocessReviewsStep"/>.</summary>
  public class Tests : FUnitContext
  {
    /// <summary>
    /// A review with a numeric score should parse into a typed record.
    /// </summary>
    [FUnitStepTest(typeof(PreprocessReviewsStep))]
    public void NumericScore_Parses()
    {
      // Arrange
      var reviews = Samples.Of(
        new ReviewSchema { ShuttleId = "S1", ReviewScoresRating = "91.0" });

      // Apply
      var result = Invoke(Create(NullLogger.Instance), reviews).ToList();

      // Assert
      Assert.That(result, Has.Count.EqualTo(1));
      Assert.That(result[0].ReviewScoresRating, Is.EqualTo(91.0m));
    }

    /// <summary>
    /// A review with a non-numeric score should be dropped.
    /// </summary>
    [FUnitStepTest(typeof(PreprocessReviewsStep))]
    public void NonNumericScore_RowDropped()
    {
      // Arrange
      var reviews = Samples.Of(
        new ReviewSchema { ShuttleId = "S1", ReviewScoresRating = "not-a-number" });

      // Apply
      var result = Invoke(Create(NullLogger.Instance), reviews).ToList();

      // Assert
      Assert.That(result, Is.Empty);
    }
  }
#endif
}
