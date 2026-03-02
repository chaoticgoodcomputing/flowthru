using KedroIris.Data._05_ModelInput.Schemas;
using KedroIris.Data._06_Models.Schemas;
using KedroIris.Data._07_ModelOutput.Schemas;

namespace KedroIris.Pipelines.DataScience.Nodes;

/// <summary>
/// Makes predictions using a trained multi-class logistic regression model.
/// </summary>
public static class PredictNode
{
  /// <summary>
  /// Creates a prediction function that applies the trained model to test data.
  /// </summary>
  /// <returns>
  /// A function that takes a model and test features to produce class predictions.
  /// </returns>
  public static Func<
    (ModelWeightsSchema Model, IEnumerable<FeatureVectorSchema> TestX),
    IEnumerable<PredictionSchema>
  > Create()
  {
    return (input) =>
    {
      var (model, testX) = input;

      var xList = testX.ToList();
      var numSamples = xList.Count;
      var numFeatures = model.NumFeatures;
      var numClasses = model.NumClasses;

      // Build feature matrix X with bias term
      var X = new double[numSamples, numFeatures + 1];
      for (int i = 0; i < numSamples; i++)
      {
        X[i, 0] = 1.0; // bias
        X[i, 1] = xList[i].SepalLength;
        X[i, 2] = xList[i].SepalWidth;
        X[i, 3] = xList[i].PetalLength;
        X[i, 4] = xList[i].PetalWidth;
      }

      // Reshape flat weights into matrix (numFeatures + 1, numClasses)
      var weights = new double[numFeatures + 1, numClasses];
      for (int row = 0; row < numFeatures + 1; row++)
      {
        for (int col = 0; col < numClasses; col++)
        {
          weights[row, col] = model.Weights[row * numClasses + col];
        }
      }

      // Compute predictions for each sample
      var predictions = new List<PredictionSchema>();
      for (int i = 0; i < numSamples; i++)
      {
        // Compute probabilities for each class: sigmoid(X * weights)
        var probs = new double[numClasses];
        for (int classIdx = 0; classIdx < numClasses; classIdx++)
        {
          double z = 0;
          for (int j = 0; j < numFeatures + 1; j++)
          {
            z += X[i, j] * weights[j, classIdx];
          }
          probs[classIdx] = Sigmoid(z);
        }

        // Predict the class with highest probability
        var predictedClass = Array.IndexOf(probs, probs.Max());

        predictions.Add(new PredictionSchema { PredictedClass = predictedClass });
      }

      return predictions;
    };
  }

  /// <summary>
  /// Sigmoid activation function: 1 / (1 + exp(-z)).
  /// </summary>
  private static double Sigmoid(double z)
  {
    return 1.0 / (1.0 + Math.Exp(-z));
  }
}
