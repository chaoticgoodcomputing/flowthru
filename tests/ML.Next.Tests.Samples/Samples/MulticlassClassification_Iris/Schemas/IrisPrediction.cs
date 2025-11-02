using Microsoft.ML.Data;

namespace ML.Next.Tests.Samples.Samples.MulticlassClassification_Iris.Schemas;

/// <summary>
/// Prediction output for Iris multiclass classification.
/// Contains probability scores for each of the 3 species classes.
/// </summary>
public class IrisPrediction {
  [ColumnName("Score")]
  public float[] Score = null!;

  [ColumnName("PredictedLabel")]
  public float PredictedLabel;
}
