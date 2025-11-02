using Microsoft.ML.Data;

namespace ML.Next.Tests.Samples.Samples.MulticlassClassification_Iris.Schemas;

/// <summary>
/// Iris flower measurement data with species label for multiclass classification.
/// </summary>
public class IrisData {
  [LoadColumn(0)]
  public float Label;

  [LoadColumn(1)]
  public float SepalLength;

  [LoadColumn(2)]
  public float SepalWidth;

  [LoadColumn(3)]
  public float PetalLength;

  [LoadColumn(4)]
  public float PetalWidth;
}
