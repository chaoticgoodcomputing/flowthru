namespace ML.Next.Tests.Samples.Samples.MulticlassClassification_Iris.Schemas;

/// <summary>
/// Sample Iris data instances for prediction testing.
/// Matches the samples from the official ML.NET example.
/// </summary>
public static class SampleIrisData {
  /// <summary>
  /// Sample 1: Iris Setosa (Label = 0)
  /// </summary>
  public static readonly IrisData Iris1 = new() {
    SepalLength = 5.1f,
    SepalWidth = 3.3f,
    PetalLength = 1.6f,
    PetalWidth = 0.2f,
  };

  /// <summary>
  /// Sample 2: Iris Virginica (Label = 2)
  /// </summary>
  public static readonly IrisData Iris2 = new() {
    SepalLength = 6.0f,
    SepalWidth = 3.4f,
    PetalLength = 6.1f,
    PetalWidth = 2.0f,
  };

  /// <summary>
  /// Sample 3: Iris Versicolor (Label = 1)
  /// </summary>
  public static readonly IrisData Iris3 = new() {
    SepalLength = 4.4f,
    SepalWidth = 3.1f,
    PetalLength = 2.5f,
    PetalWidth = 1.2f,
  };
}
