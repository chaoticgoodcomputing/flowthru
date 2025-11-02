using Flowthru.ML.Next.Core.Schema;
using Flowthru.ML.Next.Core.Columns;

namespace Flowthru.Tests.ML.Next.Samples.Samples.Clustering_Iris.Schemas;

/// <summary>
/// Phantom type schemas for type-safe pipeline composition.
/// These interfaces never get instantiated - they exist purely for compile-time checking.
/// </summary>
public static class IrisClusteringSchemas {
  /// <summary>
  /// Raw schema: columns as loaded from iris-full.txt
  /// </summary>
  public interface IRawSchema : ISchemaDefinition {
    ColumnName<float> Label { get; }
    ColumnName<float> SepalLength { get; }
    ColumnName<float> SepalWidth { get; }
    ColumnName<float> PetalLength { get; }
    ColumnName<float> PetalWidth { get; }
  }

  /// <summary>
  /// After feature concatenation (all measurements combined)
  /// </summary>
  public interface IFeaturesSchema : IRawSchema {
    ColumnName<float[]> Features { get; }
  }

  /// <summary>
  /// After K-Means clustering training/prediction
  /// </summary>
  public interface IClusteredSchema : IFeaturesSchema {
    ColumnName<uint> PredictedLabel { get; }
    ColumnName<float[]> Score { get; }  // Distances to cluster centroids
  }
}
