using Flowthru.Core.Steps;
using Flowthru.FUnit;
using SpaceflightsDistributed.DataScience.Data._05_ModelInput.Schemas;
using SpaceflightsDistributed.DataScience.Data._06_Models.Schemas;
using SpaceflightsDistributed.DataScience.Data._07_ModelOutput.Schemas;

namespace SpaceflightsDistributed.DataScience.Flows.DataScience.Steps;

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
          new ModelMetrics
          {
            R2Score = 0,
            MeanAbsoluteError = 0,
            MaxError = 0,
          },
          Enumerable.Empty<ModelPredictions>()
        );
      }

      var predictions = data.Select(d => Predict(model, d.Features)).ToList();
      var actuals = data.Select(d => (double)d.Label).ToList();

      var r2 = CalculateR2(actuals, predictions);
      var mae = CalculateMae(actuals, predictions);
      var maxError = CalculateMaxError(actuals, predictions);

      var predictionPairs = actuals
        .Zip(
          predictions,
          (actual, predicted) => new ModelPredictions { Actual = actual, Predicted = predicted }
        )
        .ToList();

      return (
        new ModelMetrics
        {
          R2Score = (decimal)r2,
          MeanAbsoluteError = (decimal)mae,
          MaxError = (decimal)maxError,
        },
        predictionPairs
      );
    };
  }

  private static double Predict(LinearRegressionModel model, FeatureVector features)
  {
    double prediction = model.Intercept;
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

  private static double CalculateMae(List<double> actuals, List<double> predictions) =>
    actuals.Zip(predictions, (a, p) => Math.Abs(a - p)).Average();

  private static double CalculateMaxError(List<double> actuals, List<double> predictions) =>
    actuals.Zip(predictions, (a, p) => Math.Abs(a - p)).Max();

#if FUNIT_ENABLED
  /// <summary>FUnit tests for <see cref="EvaluateModelStep"/>.</summary>
  public class Tests : FunitContext
  {
    private static LinearRegressionModel IdentityModel() =>
      new()
      {
        // Predicts exactly the PassengerCapacity feature (coefficient[1] = 1, rest = 0)
        Intercept = 0,
        Coefficients = new double[] { 0, 1, 0, 0, 0, 0, 0 },
        FeatureNames = new[]
        {
          "engines",
          "passenger_capacity",
          "crew",
          "d_check",
          "iata",
          "rating",
          "review",
        },
      };

    private static TestData MakeTestRow(int capacity, decimal label) =>
      new()
      {
        Features = new FeatureVector
        {
          Engines = 2,
          PassengerCapacity = capacity,
          Crew = 5,
          DCheckComplete = true,
          MoonClearanceComplete = false,
          IataApproved = true,
          CompanyRating = 0.9m,
          ReviewScoresRating = 4.5m,
        },
        Label = label,
      };

    [StepTest(typeof(EvaluateModelStep))]
    public void PerfectPredictions_R2IsOne()
    {
      // When predicted == actual for every row, R2 should be 1.0
      var model = IdentityModel();
      var testData = Samples.Of(
        MakeTestRow(100, 100m),
        MakeTestRow(200, 200m),
        MakeTestRow(300, 300m)
      );

      var (metrics, _) = Invoke(Create(), (model, testData));

      Assert.That((double)metrics.R2Score, Is.EqualTo(1.0).Within(1e-9));
      Assert.That((double)metrics.MeanAbsoluteError, Is.EqualTo(0.0).Within(1e-9));
      Assert.That((double)metrics.MaxError, Is.EqualTo(0.0).Within(1e-9));
    }

    [StepTest(typeof(EvaluateModelStep))]
    public void PredictionsReturned_CountMatchesInput()
    {
      var model = IdentityModel();
      var testData = Samples.Of(MakeTestRow(100, 100m), MakeTestRow(200, 150m));

      var (_, predictions) = Invoke(Create(), (model, testData));

      Assert.That(predictions.Count(), Is.EqualTo(2));
    }

    [StepTest(typeof(EvaluateModelStep))]
    public void EmptyTestData_ReturnsZeroMetrics()
    {
      var model = IdentityModel();

      var (metrics, predictions) = Invoke(Create(), (model, Enumerable.Empty<TestData>()));

      Assert.That(metrics.R2Score, Is.EqualTo(0));
      Assert.That(predictions, Is.Empty);
    }
  }
#endif
}
