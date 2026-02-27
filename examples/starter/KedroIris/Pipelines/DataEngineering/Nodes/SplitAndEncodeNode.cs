using KedroIris.Data._01_Raw.Schemas;
using KedroIris.Data._04_Feature.Schemas;
using KedroIris.Data._05_ModelInput.Schemas;

namespace KedroIris.Pipelines.DataEngineering.Nodes;

/// <summary>
/// Splits the classical Iris dataset into training and test sets with one-hot encoding.
/// Each split is separated into features (X) and labels (Y).
/// </summary>
public static class SplitAndEncodeNode
{
  /// <summary>
  /// Creates a data splitting function that encodes species labels and partitions the dataset.
  /// </summary>
  /// <param name="testDataRatio">Proportion of data to use for testing (e.g., 0.2 for 20%).</param>
  /// <returns>
  /// A function that transforms raw iris data into feature-encoded data and four separate
  /// training/test splits (train_x, train_y, test_x, test_y).
  /// </returns>
  public static Func<
    IEnumerable<IrisRawSchema>,
    Task<(
      IEnumerable<IrisFeatureSchema> Features,
      IEnumerable<FeatureVectorSchema> TrainX,
      IEnumerable<TargetLabelSchema> TrainY,
      IEnumerable<FeatureVectorSchema> TestX,
      IEnumerable<TargetLabelSchema> TestY
    )>
  > Create(double testDataRatio)
  {
    return async (rawData) =>
    {
      // One-hot encode species labels
      var encoded = rawData
        .Select(row => new IrisFeatureSchema
        {
          SepalLength = row.SepalLength,
          SepalWidth = row.SepalWidth,
          PetalLength = row.PetalLength,
          PetalWidth = row.PetalWidth,
          Setosa = row.Species == "setosa" ? 1.0 : 0.0,
          Versicolor = row.Species == "versicolor" ? 1.0 : 0.0,
          Virginica = row.Species == "virginica" ? 1.0 : 0.0,
        })
        .ToList();

      // Shuffle data for random train/test split
      var random = new Random(42); // Fixed seed for reproducibility
      var shuffled = encoded.OrderBy(_ => random.Next()).ToList();

      // Split into train and test sets
      var totalCount = shuffled.Count;
      var testCount = (int)(totalCount * testDataRatio);
      var testData = shuffled.Take(testCount).ToList();
      var trainData = shuffled.Skip(testCount).ToList();

      // Separate features (X) from labels (Y)
      var trainX = trainData.Select(row => new FeatureVectorSchema
      {
        SepalLength = row.SepalLength,
        SepalWidth = row.SepalWidth,
        PetalLength = row.PetalLength,
        PetalWidth = row.PetalWidth,
      });

      var trainY = trainData.Select(row => new TargetLabelSchema
      {
        Setosa = row.Setosa,
        Versicolor = row.Versicolor,
        Virginica = row.Virginica,
      });

      var testX = testData.Select(row => new FeatureVectorSchema
      {
        SepalLength = row.SepalLength,
        SepalWidth = row.SepalWidth,
        PetalLength = row.PetalLength,
        PetalWidth = row.PetalWidth,
      });

      var testY = testData.Select(row => new TargetLabelSchema
      {
        Setosa = row.Setosa,
        Versicolor = row.Versicolor,
        Virginica = row.Virginica,
      });

      return await Task.FromResult((encoded, trainX, trainY, testX, testY));
    };
  }
}
