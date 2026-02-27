using KedroIris.Data._05_ModelInput.Schemas;
using KedroIris.Data._07_ModelOutput.Schemas;
using KedroIris.Data._08_Reporting.Schemas;

namespace KedroIris.Pipelines.DataScience.Nodes;

/// <summary>
/// Evaluates model predictions against true labels and computes metrics.
/// </summary>
public static class EvaluateModelNode
{
  /// <summary>
  /// Creates an evaluation function that compares predictions with actual labels.
  /// </summary>
  /// <returns>
  /// A function that takes predictions and true labels to produce evaluation metrics.
  /// </returns>
  public static Func<
    (IEnumerable<PredictionSchema> Predictions, IEnumerable<TargetLabelSchema> TestY),
    Task<MetricsSchema>
  > Create()
  {
    return async (input) =>
    {
      var (predictions, testY) = input;

      var predList = predictions.ToList();
      var yList = testY.ToList();

      // Extract true class indices from one-hot encoded labels
      var trueClasses = yList
        .Select(label =>
        {
          if (label.Setosa == 1.0)
            return 0;
          if (label.Versicolor == 1.0)
            return 1;
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

      return await Task.FromResult(
        new MetricsSchema
        {
          Accuracy = accuracy,
          NumCorrect = numCorrect,
          NumTotal = numTotal,
        }
      );
    };
  }
}
