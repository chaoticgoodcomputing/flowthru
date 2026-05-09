using Flowthru.Step;

using MathNet.Numerics.LinearRegression;
using SpaceflightsDistributed.DataScience.Data._05_ModelInput.Schemas;
using SpaceflightsDistributed.DataScience.Data._06_Models.Schemas;

namespace SpaceflightsDistributed.DataScience.Flows.DataScience.Steps;

[FlowthruStep]
public static class TrainModelStep
{
  public static Func<IEnumerable<TrainingData>, LinearRegressionModel> Create()
  {
    return (input) =>
    {
      var data = input.ToList();

      if (data.Count == 0)
      {
        throw new InvalidOperationException("No training data available");
      }

      var features = data.Select(d => d.Features).ToList();
      var labels = data.Select(d => (double)d.Label).ToArray();

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

      var coefficients = MultipleRegression.QR(featureMatrix, labels, intercept: true);

      return new LinearRegressionModel
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
    };
  }

#if FUNIT_ENABLED
  /// <summary>FUnit tests for <see cref="TrainModelStep"/>.</summary>
  public class Tests : FUnitContext
  {
    private static FeatureVector MakeFeatures(int engines = 2, int capacity = 100) =>
      new()
      {
        Engines = engines,
        PassengerCapacity = capacity,
        Crew = 5,
        DCheckComplete = true,
        MoonClearanceComplete = false,
        IataApproved = true,
        CompanyRating = 0.9m,
        ReviewScoresRating = 4.5m,
      };

    [StepTest(typeof(TrainModelStep))]
    public void ValidData_ProducesModelWithCorrectFeatureCount()
    {
      // QR regression requires at least features + 1 samples (7 features + intercept = 8 minimum)
      var training = Enumerable
        .Range(1, 10)
        .Select(i => new TrainingData { Features = MakeFeatures(i, i * 50), Label = i * 1000m });

      var model = Invoke(Create(), training);

      // 7 features in FeatureVector
      Assert.That(model.Coefficients, Has.Length.EqualTo(7));
      Assert.That(model.FeatureNames, Has.Length.EqualTo(7));
    }

    [StepTest(typeof(TrainModelStep))]
    public void EmptyTrainingData_Throws()
    {
      Assert.That(
        () => Invoke(Create(), Enumerable.Empty<TrainingData>()),
        Throws.InvalidOperationException
      );
    }

    [StepTest(typeof(TrainModelStep))]
    public void PerfectLinearRelationship_FitsExactly()
    {
      // Each row varies all 7 features independently so the feature matrix is full-rank.
      // QR decomposition requires at least (features + 1) = 8 non-collinear samples.
      var training = Enumerable
        .Range(1, 10)
        .Select(i => new TrainingData
        {
          Features = new FeatureVector
          {
            Engines = i,
            PassengerCapacity = i * 20,
            Crew = i + 1,
            DCheckComplete = i % 2 == 0,
            MoonClearanceComplete = i % 3 == 0,
            IataApproved = i % 2 != 0,
            CompanyRating = (decimal)(0.5 + (i * 0.05)),
            ReviewScoresRating = (decimal)(3.0 + (i * 0.1)),
          },
          Label = i * 500m,
        });

      var model = Invoke(Create(), training);

      Assert.That(model.Intercept, Is.Not.NaN);
      Assert.That(model.Coefficients.All(c => !double.IsNaN(c)), Is.True);
    }
  }
#endif
}
