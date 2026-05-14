using Flowthru.Step;
using KedroIris.Data._01_Raw.Schemas;
using KedroIris.Data._04_Feature.Schemas;
using KedroIris.Data._05_ModelInput.Schemas;

namespace KedroIris.Flows.DataEngineering.Steps;

/// <summary>
/// Splits the classical Iris dataset into training and test sets with one-hot encoding.
/// Each split is separated into features (X) and labels (Y).
/// </summary>
[FlowthruStep]
public static class SplitAndEncodeStep
{
  /// <summary>
  /// Configuration options for data splitting.
  /// </summary>
  public record Options
  {
    /// <summary>Proportion of data to use for testing (e.g., 0.2 for 20%).</summary>
    public double TestDataRatio { get; init; } = 0.2;
  }

  /// <summary>
  /// Canonical Func-returning Create — the transform receives the
  /// raw iris rows and the configuration-bound <see cref="Options"/>
  /// as a tuple input. Options come from the catalog like any other
  /// fingerprintable input (Phase 5/8 of the smart-caching RFC); a
  /// change to <c>Flowthru:Flows:DataEngineering:SplitOptions</c> in
  /// <c>appsettings.json</c> invalidates this step's cached output
  /// automatically.
  /// </summary>
  public static Func<
    (IEnumerable<IrisRawSchema>, Options),
    (
      IEnumerable<IrisFeatureSchema> Features,
      IEnumerable<FeatureVectorSchema> TrainX,
      IEnumerable<TargetLabelSchema> TrainY,
      IEnumerable<FeatureVectorSchema> TestX,
      IEnumerable<TargetLabelSchema> TestY
    )
  > Create() => input =>
  {
    var (rawData, options) = input;

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
    var testCount = (int)(totalCount * options.TestDataRatio);
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

    return (encoded, trainX, trainY, testX, testY);
  };
}
