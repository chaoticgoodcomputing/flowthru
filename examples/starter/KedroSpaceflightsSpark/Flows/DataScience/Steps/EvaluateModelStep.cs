using Flowthru.Core.Steps;
using KedroSpaceflightsSpark.Data._05_ModelInput.Schemas;
using KedroSpaceflightsSpark.Data._06_Models.Schemas;
using KedroSpaceflightsSpark.Data._07_ModelOutput.Schemas;

namespace KedroSpaceflightsSpark.Flows.DataScience.Steps;

[FlowthruStep]
public static class EvaluateModelStep
{
  public static Func<
    (LinearRegressionModel, IEnumerable<TestData>),
    (ModelMetrics, IEnumerable<ModelPredictions>)
  > Create()
  {
    return (input) =>
    {
      var (model, testData) = input;
      var data = testData.ToList();

      if (data.Count == 0)
      {
        return (
          new ModelMetrics { R2Score = 0, MeanAbsoluteError = 0, MaxError = 0 },
          Enumerable.Empty<ModelPredictions>()
        );
      }

      var predictions = data.Select(d => Predict(model, d.Features)).ToList();
      var actuals = data.Select(d => d.Label).ToList();

      var predictionPairs = actuals
        .Zip(predictions, (actual, predicted) => new ModelPredictions { Actual = actual, Predicted = predicted })
        .ToList();

      return (
        new ModelMetrics
        {
          R2Score = (decimal)CalculateR2(actuals, predictions),
          MeanAbsoluteError = (decimal)CalculateMae(actuals, predictions),
          MaxError = (decimal)CalculateMaxError(actuals, predictions),
        },
        predictionPairs
      );
    };
  }

  private static double Predict(LinearRegressionModel model, FeatureVector features)
  {
    double prediction = model.Intercept;
    double[] featureValues =
    [
      features.Engines,
      features.PassengerCapacity,
      features.Crew,
      features.DCheckComplete ? 1.0 : 0.0,
      features.IataApproved ? 1.0 : 0.0,
      features.CompanyRating,
      features.ReviewScoresRating,
    ];
    for (int i = 0; i < model.Coefficients.Length; i++)
      prediction += model.Coefficients[i] * featureValues[i];
    return prediction;
  }

  private static double CalculateR2(List<double> actuals, List<double> predictions)
  {
    var mean = actuals.Average();
    var ssTotal = actuals.Sum(y => Math.Pow(y - mean, 2));
    var ssResidual = actuals.Zip(predictions, (a, p) => Math.Pow(a - p, 2)).Sum();
    return 1 - (ssResidual / ssTotal);
  }

  private static double CalculateMae(List<double> actuals, List<double> predictions) =>
    actuals.Zip(predictions, (a, p) => Math.Abs(a - p)).Average();

  private static double CalculateMaxError(List<double> actuals, List<double> predictions) =>
    actuals.Zip(predictions, (a, p) => Math.Abs(a - p)).Max();
}
