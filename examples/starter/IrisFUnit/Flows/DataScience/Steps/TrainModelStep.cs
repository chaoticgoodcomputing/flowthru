using System.Diagnostics;
using Flowthru.Step;
using Flowthru.Step.Testing;
using IrisFUnit.Data._05_ModelInput.Schemas;
using IrisFUnit.Data._06_Models.Schemas;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace IrisFUnit.Flows.DataScience.Steps;

/// <summary>
/// Trains a simple multi-class logistic regression model using
/// gradient descent.
/// </summary>
[FlowthruStep]
public static class TrainModelStep
{
  /// <summary>Configuration options for <see cref="TrainModelStep"/>.</summary>
  public record Options
  {
    /// <summary>Number of training iterations for gradient descent.</summary>
    public int NumTrainIter { get; init; } = 10000;

    /// <summary>Learning rate for gradient descent optimization.</summary>
    public double LearningRate { get; init; } = 0.01;
  }

  /// <summary>
  /// Canonical Func-returning Create — the transform receives the
  /// training features, training labels, and the configuration-bound
  /// <see cref="Options"/> as a tuple input. Phase 5/8: a change to
  /// <c>Flowthru:Flows:DataScience:TrainModelOptions</c> in
  /// <c>appsettings.json</c> invalidates this step's cached output.
  /// </summary>
  public static Func<
    (
      IEnumerable<FeatureVectorSchema>,
      IEnumerable<TargetLabelSchema>,
      Options
    ),
    ModelWeightsSchema
  > Create(ILogger logger) =>
    input =>
    {
      var (trainX, trainY, options) = input;
      var numIterations = options.NumTrainIter;
      var learningRate = options.LearningRate;

      var xList = trainX.ToList();
      var yList = trainY.ToList();

      var numSamples = xList.Count;
      var numFeatures = 4; // sepal_length, sepal_width, petal_length, petal_width
      var numClasses = 3; // setosa, versicolor, virginica

      logger.LogInformation(
        "Training one-vs-rest logistic regression on {Samples} samples "
        + "× {Features} features × {Classes} classes ({Iters} iters, lr={LR})",
        numSamples, numFeatures, numClasses, numIterations, learningRate
      );
      var stopwatch = Stopwatch.StartNew();

      // Build feature matrix X with bias term (numSamples × (numFeatures + 1)).
      var X = new double[numSamples, numFeatures + 1];
      for (var i = 0; i < numSamples; i++)
      {
        X[i, 0] = 1.0; // bias
        X[i, 1] = xList[i].SepalLength;
        X[i, 2] = xList[i].SepalWidth;
        X[i, 3] = xList[i].PetalLength;
        X[i, 4] = xList[i].PetalWidth;
      }

      // Build label matrix Y (numSamples × numClasses).
      var Y = new double[numSamples, numClasses];
      for (var i = 0; i < numSamples; i++)
      {
        Y[i, 0] = yList[i].Setosa;
        Y[i, 1] = yList[i].Versicolor;
        Y[i, 2] = yList[i].Virginica;
      }

      // Train one model per class.
      var weights = new List<double[]>();
      for (var classIdx = 0; classIdx < numClasses; classIdx++)
      {
        var theta = new double[numFeatures + 1];
        var y = new double[numSamples];
        for (var i = 0; i < numSamples; i++)
        {
          y[i] = Y[i, classIdx];
        }

        for (var iter = 0; iter < numIterations; iter++)
        {
          var z = new double[numSamples];
          for (var i = 0; i < numSamples; i++)
          {
            z[i] = 0;
            for (var j = 0; j < numFeatures + 1; j++) z[i] += X[i, j] * theta[j];
          }
          var h = z.Select(Sigmoid).ToArray();

          var gradient = new double[numFeatures + 1];
          for (var j = 0; j < numFeatures + 1; j++)
          {
            gradient[j] = 0;
            for (var i = 0; i < numSamples; i++) gradient[j] += X[i, j] * (h[i] - y[i]);
            gradient[j] /= numSamples;
          }
          for (var j = 0; j < numFeatures + 1; j++) theta[j] -= learningRate * gradient[j];
        }
        weights.Add(theta);
      }

      // Flatten the weights matrix in row-major-by-feature order so a flat
      // double[] survives JSON round-tripping.
      var flatWeights = new double[(numFeatures + 1) * numClasses];
      for (var col = 0; col < numClasses; col++)
      {
        for (var row = 0; row < numFeatures + 1; row++)
        {
          flatWeights[row * numClasses + col] = weights[col][row];
        }
      }

      stopwatch.Stop();
      logger.LogInformation(
        "Training completed in {Elapsed:F0} ms", stopwatch.Elapsed.TotalMilliseconds
      );

      return new ModelWeightsSchema
      {
        Weights = flatWeights,
        NumFeatures = numFeatures,
        NumClasses = numClasses,
      };
    };

  private static double Sigmoid(double z) => 1.0 / (1.0 + Math.Exp(-z));

#if FUNIT_ENABLED
  public class Tests : FUnitContext
  {
    private static IEnumerable<FeatureVectorSchema> SampleFeatures(int count) =>
      Enumerable.Range(0, count).Select(i => new FeatureVectorSchema
      {
        SepalLength = 5.0 + i * 0.1,
        SepalWidth = 3.0,
        PetalLength = 1.5 + i * 0.05,
        PetalWidth = 0.3,
      });

    private static IEnumerable<TargetLabelSchema> SampleLabels(int count) =>
      Enumerable.Range(0, count).Select(i => new TargetLabelSchema
      {
        Setosa = i % 3 == 0 ? 1.0 : 0.0,
        Versicolor = i % 3 == 1 ? 1.0 : 0.0,
        Virginica = i % 3 == 2 ? 1.0 : 0.0,
      });

    [FUnitStepTest(typeof(TrainModelStep))]
    public void ReturnsWeightsWithCorrectShape()
    {
      var trainX = SampleFeatures(12);
      var trainY = SampleLabels(12);

      var model = Invoke(
        Create(NullLogger.Instance),
        (trainX, trainY, new Options { NumTrainIter = 100, LearningRate = 0.01 })
      );

      Assert.That(model.NumFeatures, Is.EqualTo(4));
      Assert.That(model.NumClasses, Is.EqualTo(3));
      Assert.That(model.Weights.Length, Is.EqualTo((4 + 1) * 3));
    }

    [FUnitStepTest(typeof(TrainModelStep))]
    public void WithMinimalIterations_DoesNotThrow()
    {
      var trainX = SampleFeatures(6);
      var trainY = SampleLabels(6);

      Assert.DoesNotThrow(() =>
        Invoke(Create(NullLogger.Instance), (trainX, trainY, new Options { NumTrainIter = 1, LearningRate = 0.001 }))
      );
    }
  }
#endif
}
