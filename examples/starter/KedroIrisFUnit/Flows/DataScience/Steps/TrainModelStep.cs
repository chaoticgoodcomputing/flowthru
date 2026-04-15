using Flowthru.Core.Steps;
using Flowthru.FUnit;
using KedroIrisFUnit.Data._05_ModelInput.Schemas;
using KedroIrisFUnit.Data._06_Models.Schemas;

namespace KedroIrisFUnit.Flows.DataScience.Steps;

/// <summary>
/// Trains a simple multi-class logistic regression model using gradient descent.
/// </summary>
[FlowthruStep]
public static class TrainModelStep
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

#if FUNIT_ENABLED
    /// <summary>
    /// FUnit tests for <see cref="TrainModelStep"/>.
    /// </summary>
    public class Tests : FunitContext
    {
        private static IEnumerable<FeatureVectorSchema> SampleFeatures(int count) =>
          Enumerable
            .Range(0, count)
            .Select(i => new FeatureVectorSchema
            {
                SepalLength = 5.0 + i * 0.1,
                SepalWidth = 3.0,
                PetalLength = 1.5 + i * 0.05,
                PetalWidth = 0.3,
            });

        private static IEnumerable<TargetLabelSchema> SampleLabels(int count) =>
          Enumerable
            .Range(0, count)
            .Select(i => new TargetLabelSchema
            {
                Setosa = i % 3 == 0 ? 1.0 : 0.0,
                Versicolor = i % 3 == 1 ? 1.0 : 0.0,
                Virginica = i % 3 == 2 ? 1.0 : 0.0,
            });

        /// <summary>
        /// After training, the returned <see cref="ModelWeightsSchema"/> must describe
        /// a weight matrix of shape (NumFeatures + 1) × NumClasses — the +1 accounts
        /// for the bias term prepended to each feature row.
        /// </summary>
        [StepTest(typeof(TrainModelStep))]
        public void ReturnsWeightsWithCorrectShape()
        {
            // Arrange — 12 samples cycling across 3 classes, 4 features each
            var trainX = SampleFeatures(12);
            var trainY = SampleLabels(12);

            // Apply
            var model = Invoke(Create(numIterations: 100, learningRate: 0.01), (trainX, trainY));

            // Assert — weight matrix shape: (NumFeatures + 1) × NumClasses = 5 × 3 = 15
            Assert.That(model.NumFeatures, Is.EqualTo(4));
            Assert.That(model.NumClasses, Is.EqualTo(3));
            Assert.That(model.Weights.Length, Is.EqualTo((4 + 1) * 3));
        }

        /// <summary>
        /// The step must complete without throwing even when training for only a
        /// single iteration — this guards against index-out-of-bounds or divide-by-zero
        /// issues in the gradient descent loop at minimal iteration counts.
        /// </summary>
        [StepTest(typeof(TrainModelStep))]
        public void WithMinimalIterations_DoesNotThrow()
        {
            // Arrange — smallest viable training set (6 samples, 2 per class)
            var trainX = SampleFeatures(6);
            var trainY = SampleLabels(6);

            // Apply + Assert
            Assert.DoesNotThrow(
              () => Invoke(Create(numIterations: 1, learningRate: 0.001), (trainX, trainY))
            );
        }
    }
#endif
}
