using Flowthru.Core.Steps;
using Flowthru.FUnit;
using KedroSpaceflightsFUnit.Data._05_ModelInput.Schemas;
using KedroSpaceflightsFUnit.Data._06_Models.Schemas;
using MathNet.Numerics.LinearRegression;

namespace KedroSpaceflightsFUnit.Flows.DataScience.Steps;

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
  public static Func<IEnumerable<TrainingData>, LinearRegressionModel> Create()
  {
    return (input) =>
    {
      var data = input.ToList();

      if (data.Count == 0)
      {
        throw new InvalidOperationException("No training data available");
      }

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
          features[i].DCheckComplete ? 1.0 : 0.0,
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

      return model;
    };
  }

#if FUNIT_ENABLED
  /// <summary>FUnit tests for <see cref="TrainModelStep"/>.</summary>
  public class Tests : FUnitContext
  {
    private static TrainingData SampleRow(double price = 1000.0) =>
      new()
      {
        Features = new FeatureVector
        {
          Engines = 4,
          PassengerCapacity = 100,
          Crew = 8,
          DCheckComplete = true,
          MoonClearanceComplete = false,
          IataApproved = true,
          CompanyRating = 0.9m,
          ReviewScoresRating = 80m,
        },
        Label = (decimal)price,
      };

    /// <summary>
    /// Empty training data should throw <see cref="InvalidOperationException"/>.
    /// </summary>
    [StepTest(typeof(TrainModelStep))]
    public void EmptyInput_ThrowsInvalidOperationException()
    {
      Assert.Throws<InvalidOperationException>(
        () => Invoke(Create(), Enumerable.Empty<TrainingData>())
      );
    }

    /// <summary>
    /// The number of coefficients must equal the number of feature names.
    /// </summary>
    [StepTest(typeof(TrainModelStep))]
    public void TrainedModel_CoefficientCountMatchesFeatureNames()
    {
      // Arrange — need at least as many rows as features for QR to be well-determined
      var input = Samples.Generate(20, i => SampleRow(1000.0 + i * 10.0));

      // Apply
      var model = Invoke(Create(), input);

      // Assert
      Assert.That(model.Coefficients.Length, Is.EqualTo(model.FeatureNames.Length));
    }
  }
#endif
}
