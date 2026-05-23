using System.Diagnostics;
using Flowthru.Step;
using MathNet.Numerics.LinearRegression;
using Microsoft.Extensions.Logging;
using Spaceflights.Data._02_Intermediate.Schemas;
using Spaceflights.Data._05_ModelInput.Schemas;
using Spaceflights.Data._06_Models.Schemas;

namespace Spaceflights.Flows.DataScience.Steps;

/// <summary>
/// Trains a linear regression model to predict prices based on shuttle and company features.
/// </summary>
[FlowthruStep]
public static class TrainModelStep
{
  /// <summary>
  /// Creates a model training function that fits a linear regression model.
  /// Uses QR decomposition for numerical stability.
  /// </summary>
  /// <returns>
  /// A function that trains a <see cref="LinearRegressionModel"/> from training data.
  /// </returns>
  /// <remarks>
  /// Uses <see href="https://numerics.mathdotnet.com/">Math.NET Numerics</see> for regression computation.
  /// Excludes moon_clearance_complete feature due to zero variance in training data.
  /// </remarks>
  /// <exception cref="InvalidOperationException">Thrown when no training data is available.</exception>
  public static Func<IEnumerable<TrainingData>, LinearRegressionModel> Create(ILogger logger)
  {
    return (input) =>
    {
      var data = input.ToList();

      if (data.Count == 0)
      {
        throw new InvalidOperationException("No training data available");
      }

      logger.LogInformation(
        "Training linear regression on {Samples} samples × 7 features (QR decomposition)",
        data.Count
      );
      var stopwatch = Stopwatch.StartNew();

      // Extract features and labels
      var features = data.Select(d => d.Features).ToList();
      var labels = data.Select(d => (double)d.Label).ToArray();

      // Convert features to jagged array form (one row per observation)
      // Note: Excluding moon_clearance_complete due to zero variance (all values are the same)
      var featureMatrix = new double[features.Count][];
      for (int i = 0; i < features.Count; i++)
      {
        featureMatrix[i] = new double[]
        {
          (double)features[i].Engines,
          (double)features[i].PassengerCapacity,
          (double)features[i].Crew,
          features[i].DCheckComplete == CheckStatus.Complete ? 1.0 : 0.0,
          features[i].IataApproved ? 1.0 : 0.0,
          (double)features[i].CompanyRating,
          (double)features[i].ReviewScoresRating,
        };
      }

      // Use Math.NET's MultipleRegression with QR decomposition (more stable than normal equation)
      var coefficients = MultipleRegression.QR(featureMatrix, labels, intercept: true);

      var model = new LinearRegressionModel
      {
        Intercept = coefficients[0],
        Coefficients = coefficients.Skip(1).ToArray(),
        FeatureNames = new[]
        {
          "engines",
          "passenger_capacity",
          "crew",
          "d_check_complete",
          "iata_approved",
          "company_rating",
          "review_scores_rating",
        },
      };

      stopwatch.Stop();
      logger.LogInformation(
        "Training completed in {Elapsed:F0} ms (intercept={Intercept:F2})",
        stopwatch.Elapsed.TotalMilliseconds, model.Intercept
      );

      return model;
    };
  }
}
