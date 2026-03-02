using KedroIris.Data._05_ModelInput.Schemas;
using KedroIris.Data._06_Models.Schemas;

namespace KedroIris.Pipelines.DataScience.Nodes;

/// <summary>
/// Trains a simple multi-class logistic regression model using gradient descent.
/// </summary>
public static class TrainModelNode
{
  /// <summary>
  /// Creates a training function for multi-class logistic regression.
  /// </summary>
  /// <param name="numIterations">Number of training iterations.</param>
  /// <param name="learningRate">Learning rate for gradient descent.</param>
  /// <returns>
  /// A function that trains on feature vectors and labels to produce model weights.
  /// </returns>
  public static Func<
    (IEnumerable<FeatureVectorSchema> TrainX, IEnumerable<TargetLabelSchema> TrainY),
    ModelWeightsSchema
  > Create(int numIterations, double learningRate)
  {
    return (input) =>
    {
      var (trainX, trainY) = input;

      // Convert to arrays for matrix operations
      var xList = trainX.ToList();
      var yList = trainY.ToList();

      var numSamples = xList.Count;
      var numFeatures = 4; // sepal_length, sepal_width, petal_length, petal_width
      var numClasses = 3; // setosa, versicolor, virginica

      // Build feature matrix X with bias term (num_samples x (num_features + 1))
      var X = new double[numSamples, numFeatures + 1];
      for (int i = 0; i < numSamples; i++)
      {
        X[i, 0] = 1.0; // bias
        X[i, 1] = xList[i].SepalLength;
        X[i, 2] = xList[i].SepalWidth;
        X[i, 3] = xList[i].PetalLength;
        X[i, 4] = xList[i].PetalWidth;
      }

      // Build label matrix Y (num_samples x num_classes)
      var Y = new double[numSamples, numClasses];
      for (int i = 0; i < numSamples; i++)
      {
        Y[i, 0] = yList[i].Setosa;
        Y[i, 1] = yList[i].Versicolor;
        Y[i, 2] = yList[i].Virginica;
      }

      // Train one model for each class
      var weights = new List<double[]>();
      for (int classIdx = 0; classIdx < numClasses; classIdx++)
      {
        // Initialize weights for this class
        var theta = new double[numFeatures + 1];

        // Get target labels for this class
        var y = new double[numSamples];
        for (int i = 0; i < numSamples; i++)
        {
          y[i] = Y[i, classIdx];
        }

        // Gradient descent
        for (int iter = 0; iter < numIterations; iter++)
        {
          // z = X * theta
          var z = new double[numSamples];
          for (int i = 0; i < numSamples; i++)
          {
            z[i] = 0;
            for (int j = 0; j < numFeatures + 1; j++)
            {
              z[i] += X[i, j] * theta[j];
            }
          }

          // h = sigmoid(z)
          var h = z.Select(Sigmoid).ToArray();

          // gradient = X^T * (h - y) / numSamples
          var gradient = new double[numFeatures + 1];
          for (int j = 0; j < numFeatures + 1; j++)
          {
            gradient[j] = 0;
            for (int i = 0; i < numSamples; i++)
            {
              gradient[j] += X[i, j] * (h[i] - y[i]);
            }
            gradient[j] /= numSamples;
          }

          // theta -= learningRate * gradient
          for (int j = 0; j < numFeatures + 1; j++)
          {
            theta[j] -= learningRate * gradient[j];
          }
        }

        weights.Add(theta);
      }

      // Flatten weights matrix into a single array (column-major order)
      // Shape: (numFeatures + 1, numClasses)
      var flatWeights = new double[(numFeatures + 1) * numClasses];
      for (int col = 0; col < numClasses; col++)
      {
        for (int row = 0; row < numFeatures + 1; row++)
        {
          flatWeights[row * numClasses + col] = weights[col][row];
        }
      }

      return new ModelWeightsSchema
      {
        Weights = flatWeights,
        NumFeatures = numFeatures,
        NumClasses = numClasses,
      };
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
