using Microsoft.ML.Data;

namespace Flowthru.Tests.ML.Next.Samples.Samples.Clustering_Iris.Schemas;

/// <summary>
/// Prediction output from K-Means clustering.
/// </summary>
public class IrisPrediction {
  [ColumnName("PredictedLabel")]
  public uint SelectedClusterId;

  [ColumnName("Score")]
  public float[] Distance = Array.Empty<float>();
}
