using ML.Next.Core.Schema;
using ML.Next.Core.Columns;

namespace ML.Next.Tests.Samples.Samples.Clustering_Iris.Schemas;

/// <summary>
/// Phantom type schemas for type-safe pipeline composition.
/// These interfaces never get instantiated - they exist purely for compile-time checking.
/// </summary>
public static class IrisClusteringSchemas {
  /// <summary>
  /// Raw schema: columns as loaded from iris-full.txt
  /// </summary>
  public interface IRawSchema : ISchemaDefinition {
    ColumnSpec<float> Label { get; }
    ColumnSpec<float> SepalLength { get; }
    ColumnSpec<float> SepalWidth { get; }
    ColumnSpec<float> PetalLength { get; }
    ColumnSpec<float> PetalWidth { get; }
  }

  /// <summary>
  /// After feature concatenation (all measurements combined)
  /// </summary>
  public interface IFeaturesSchema : IRawSchema {
    ColumnSpec<float[]> Features { get; }
  }

  /// <summary>
  /// After K-Means clustering training/prediction
  /// </summary>
  public interface IClusteredSchema : IFeaturesSchema {
    ColumnSpec<uint> PredictedLabel { get; }
    ColumnSpec<float[]> Score { get; }  // Distances to cluster centroids
  }
}
