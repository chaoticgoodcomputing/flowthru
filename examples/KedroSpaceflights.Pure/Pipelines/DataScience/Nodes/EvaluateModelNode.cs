using KedroSpaceflights.Pure.Data._03_Primary.Schemas;
using KedroSpaceflights.Pure.Data._04_Models.Schemas;

namespace KedroSpaceflights.Pure.Pipelines.DataScience.Nodes;

public static class EvaluateModelNode
{
  public static Func<(LinearRegressionModel, IEnumerable<TestData>), Task<ModelMetrics>> Create()
  {
    return async (input) =>
    {
      var (model, testData) = input;
      var data = testData.ToList();

      if (data.Count == 0)
      {
        Console.WriteLine("No test data available for evaluation");
        return await Task.FromResult(
          new ModelMetrics
          {
            R2Score = 0,
            MeanAbsoluteError = 0,
            MaxError = 0,
          }
        );
      }

      // Make predictions
      var predictions = data.Select(d => Predict(model, d.Features)).ToList();
      var actuals = data.Select(d => (double)d.Label).ToList();

      // Calculate metrics
      var r2 = CalculateR2(actuals, predictions);
      var mae = CalculateMae(actuals, predictions);
      var maxError = CalculateMaxError(actuals, predictions);

      Console.WriteLine($"Model Evaluation Results:");
      Console.WriteLine($"  R² Score: {r2:F3}");
      Console.WriteLine($"  Mean Absolute Error: {mae:F2}");
      Console.WriteLine($"  Max Error: {maxError:F2}");

      return await Task.FromResult(
        new ModelMetrics
        {
          R2Score = (decimal)r2,
          MeanAbsoluteError = (decimal)mae,
          MaxError = (decimal)maxError,
        }
      );
    };
  }

  private static double Predict(LinearRegressionModel model, FeatureVector features)
  {
    double prediction = model.Intercept;

    // Note: Excluding moon_clearance_complete to match training features
    var featureValues = new double[]
    {
      (double)features.Engines,
      (double)features.PassengerCapacity,
      (double)features.Crew,
      features.DCheckComplete ? 1.0 : 0.0,
      features.IataApproved ? 1.0 : 0.0,
      (double)features.CompanyRating,
      (double)features.ReviewScoresRating,
    };

    for (int i = 0; i < model.Coefficients.Length; i++)
    {
      prediction += model.Coefficients[i] * featureValues[i];
    }

    return prediction;
  }

  private static double CalculateR2(List<double> actuals, List<double> predictions)
  {
    var mean = actuals.Average();
    var ssTotal = actuals.Sum(y => Math.Pow(y - mean, 2));
    var ssResidual = actuals.Zip(predictions, (a, p) => Math.Pow(a - p, 2)).Sum();
    return 1 - (ssResidual / ssTotal);
  }

  private static double CalculateMae(List<double> actuals, List<double> predictions)
  {
    return actuals.Zip(predictions, (a, p) => Math.Abs(a - p)).Average();
  }

  private static double CalculateMaxError(List<double> actuals, List<double> predictions)
  {
    return actuals.Zip(predictions, (a, p) => Math.Abs(a - p)).Max();
  }
}
