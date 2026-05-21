using Flowthru.Step;
using Flowthru.Step.Testing;
using IrisFUnit.Data._05_ModelInput.Schemas;
using IrisFUnit.Data._07_ModelOutput.Schemas;
using IrisFUnit.Data._08_Reporting.Schemas;

namespace IrisFUnit.Flows.DataScience.Steps;

/// <summary>
/// Evaluates model predictions against true labels and computes metrics.
/// </summary>
[FlowthruStep]
public static class EvaluateModelStep
{
  /// <summary>
  /// Creates an evaluation function that compares predictions with actual labels.
  /// </summary>
  /// <returns>
  /// A function that takes predictions and true labels to produce evaluation metrics.
  /// </returns>
  public static Func<
    (IEnumerable<PredictionSchema> Predictions, IEnumerable<TargetLabelSchema> TestY),
    MetricsSchema
  > Create() =>
    input =>
    {
      var (predictions, testY) = input;

      var predList = predictions.ToList();
      var yList = testY.ToList();

      // Extract true class indices from one-hot encoded labels
      var trueClasses = yList
        .Select(label =>
        {
          if (label.Setosa == 1.0)
          {
            return 0;
          }

          if (label.Versicolor == 1.0)
          {
            return 1;
          }

          return 2; // virginica
        })
        .ToList();

      // Compare predictions with true labels
      int numCorrect = 0;
      for (int i = 0; i < predList.Count; i++)
      {
        if (predList[i].PredictedClass == trueClasses[i])
        {
          numCorrect++;
        }
      }

      var numTotal = predList.Count;
      var accuracy = (double)numCorrect / numTotal;

      // Log accuracy to console
      Console.WriteLine($"Model accuracy on test set: {accuracy:P2} ({numCorrect}/{numTotal})");

      return new MetricsSchema
      {
        Accuracy = accuracy,
        NumCorrect = numCorrect,
        NumTotal = numTotal,
      };
    };

#if FUNIT_ENABLED
  /// <summary>FUnit tests for <see cref="EvaluateModelStep"/>.</summary>
  public class Tests : FUnitContext
  {
    private static TargetLabelSchema Setosa =>
      new()
      {
        Setosa = 1.0,
        Versicolor = 0.0,
        Virginica = 0.0,
      };
    private static TargetLabelSchema Versicolor =>
      new()
      {
        Setosa = 0.0,
        Versicolor = 1.0,
        Virginica = 0.0,
      };
    private static TargetLabelSchema Virginica =>
      new()
      {
        Setosa = 0.0,
        Versicolor = 0.0,
        Virginica = 1.0,
      };

    /// <summary>
    /// When every predicted class index matches its true label, accuracy
    /// should be 1.0 (100%) and NumCorrect should equal NumTotal.
    /// </summary>
    [FUnitStepTest(typeof(EvaluateModelStep))]
    public void AllCorrect_ReturnsAccuracyOfOne()
    {
      // Arrange
      var predictions = new[]
      {
        new PredictionSchema { PredictedClass = 0 }, // setosa   → correct
        new PredictionSchema { PredictedClass = 1 }, // versicolor → correct
        new PredictionSchema { PredictedClass = 2 }, // virginica  → correct
      };
      var labels = new[] { Setosa, Versicolor, Virginica };

      // Apply
      var result = Invoke(Create(), (predictions, labels));

      // Assert
      Assert.That(result.Accuracy, Is.EqualTo(1.0));
      Assert.That(result.NumCorrect, Is.EqualTo(3));
      Assert.That(result.NumTotal, Is.EqualTo(3));
    }

    /// <summary>
    /// When every predicted class index is wrong, accuracy should be 0.0
    /// and NumCorrect should be zero regardless of NumTotal.
    /// </summary>
    [FUnitStepTest(typeof(EvaluateModelStep))]
    public void NoneCorrect_ReturnsAccuracyOfZero()
    {
      // Arrange
      var predictions = new[]
      {
        new PredictionSchema { PredictedClass = 1 }, // expects setosa (0)    → wrong
        new PredictionSchema { PredictedClass = 2 }, // expects versicolor (1) → wrong
        new PredictionSchema { PredictedClass = 0 }, // expects virginica (2)  → wrong
      };
      var labels = new[] { Setosa, Versicolor, Virginica };

      // Apply
      var result = Invoke(Create(), (predictions, labels));

      // Assert
      Assert.That(result.Accuracy, Is.EqualTo(0.0));
      Assert.That(result.NumCorrect, Is.EqualTo(0));
    }

    /// <summary>
    /// When exactly half the predictions are correct, accuracy should be 0.5
    /// (NumCorrect / NumTotal = 1 / 2).
    /// </summary>
    [FUnitStepTest(typeof(EvaluateModelStep))]
    public void HalfCorrect_ReturnsAccuracyOfPointFive()
    {
      // Arrange
      var predictions = new[]
      {
        new PredictionSchema { PredictedClass = 0 }, // expects setosa (0)    → correct
        new PredictionSchema { PredictedClass = 2 }, // expects versicolor (1) → wrong
      };
      var labels = new[] { Setosa, Versicolor };

      // Apply
      var result = Invoke(Create(), (predictions, labels));

      // Assert
      Assert.That(result.Accuracy, Is.EqualTo(0.5));
      Assert.That(result.NumCorrect, Is.EqualTo(1));
      Assert.That(result.NumTotal, Is.EqualTo(2));
    }
  }
#endif
}
