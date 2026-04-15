using Flowthru.Core.Steps;
using KedroSpaceflightsSpark.Data._05_ModelInput.Schemas;
using KedroSpaceflightsSpark.Data._06_Models.Schemas;
using MathNet.Numerics.LinearRegression;

namespace KedroSpaceflightsSpark.Flows.DataScience.Steps;

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
      var labels = data.Select(d => d.Label).ToArray();

      var featureMatrix = new double[features.Count][];
      for (int i = 0; i < features.Count; i++)
      {
        featureMatrix[i] =
        [
          features[i].Engines,
          features[i].PassengerCapacity,
          features[i].Crew,
          features[i].DCheckComplete ? 1.0 : 0.0,
          features[i].IataApproved ? 1.0 : 0.0,
          features[i].CompanyRating,
          features[i].ReviewScoresRating,
        ];
      }

      var coefficients = MultipleRegression.QR(featureMatrix, labels, intercept: true);

      return new LinearRegressionModel
      {
        Intercept = coefficients[0],
        Coefficients = coefficients.Skip(1).ToArray(),
        FeatureNames =
        [
          "engines",
          "passenger_capacity",
          "crew",
          "d_check_complete",
          "iata_approved",
          "company_rating",
          "review_scores_rating",
        ],
      };
    };
  }
}
