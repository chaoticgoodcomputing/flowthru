using Microsoft.ML.Data;

namespace Flowthru.Tests.ML.Ext.Samples.Clustering_Iris.DataStructures;

/// <summary>
/// Clustering prediction result.
/// </summary>
public class IrisPrediction {
  [ColumnName("PredictedLabel")]
  public uint SelectedClusterId;

  [ColumnName("Score")]
  public float[] Distance = Array.Empty<float>();
}
