using Flowthru.Step;
using Flowthru.Step.Testing;
using IrisFUnit.Data._05_ModelInput.Schemas;
using IrisFUnit.Data._06_Models.Schemas;
using IrisFUnit.Data._07_ModelOutput.Schemas;

namespace IrisFUnit.Flows.DataScience.Steps;

/// <summary>
/// Makes predictions using a trained multi-class logistic regression model.
/// </summary>
[FlowthruStep]
public static class PredictStep
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
  > Create() =>
    input =>
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

  /// <summary>
  /// Sigmoid activation function: 1 / (1 + exp(-z)).
  /// </summary>
  private static double Sigmoid(double z)
  {
    return 1.0 / (1.0 + Math.Exp(-z));
  }

#if FUNIT_ENABLED
  /// <summary>
  /// FUnit tests for <see cref="PredictStep"/>.
  /// </summary>
  public class Tests : FUnitContext
  {
    private static ModelWeightsSchema ZeroModel() =>
      new ModelWeightsSchema
      {
        // All-zero weights: sigmoid(0) = 0.5 for every class,
        // so argmax always returns class index 0.
        Weights = new double[(4 + 1) * 3],
        NumFeatures = 4,
        NumClasses = 3,
      };

    /// <summary>
    /// The step must emit exactly one <see cref="PredictionSchema"/> per input
    /// feature row — output length must equal input length.
    /// </summary>
    #region docs:step-funit-test
    [FUnitStepTest(typeof(PredictStep))]
    public void ReturnsOnePredictionPerInputRow()
    {
      // Arrange
      var testX = Samples.Generate(
        5,
        i => new FeatureVectorSchema
        {
          SepalLength = 5.0 + i,
          SepalWidth = 3.0,
          PetalLength = 1.5,
          PetalWidth = 0.3,
        }
      );

      // Apply
      var predictions = Invoke(Create(), (ZeroModel(), testX)).ToList();

      // Assert
      Assert.That(predictions, Has.Count.EqualTo(5));
    }
    #endregion

    /// <summary>
    /// The predicted class index must always fall in [0, numClasses-1].
    /// With a zero-weight model, sigmoid(0) = 0.5 for every class and argmax
    /// returns class 0 — still a valid, in-range index.
    /// </summary>
    [FUnitStepTest(typeof(PredictStep))]
    public void PredictedClass_IsAlwaysWithinValidRange()
    {
      // Arrange
      var testX = Samples.Of(
        new FeatureVectorSchema
        {
          SepalLength = 6.3,
          SepalWidth = 3.3,
          PetalLength = 6.0,
          PetalWidth = 2.5,
        }
      );

      // Apply
      var predictions = Invoke(Create(), (ZeroModel(), testX)).ToList();

      // Assert — class indices must be in [0, 2] for a 3-class model
      Assert.That(predictions[0].PredictedClass, Is.InRange(0, 2));
    }
  }
#endif
}
