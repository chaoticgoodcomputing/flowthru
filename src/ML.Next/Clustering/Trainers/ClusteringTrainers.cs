using System.Linq.Expressions;
using Microsoft.ML;
using ML.Next.Core.Schema;
using ML.Next.Core.Columns;
using ML.Next.Transforms;

namespace ML.Next.Clustering.Trainers;

/// <summary>
/// Type-safe clustering trainers with compile-time column validation.
/// </summary>
public static class ClusteringTrainers
{
  /// <summary>
  /// K-Means clustering trainer with compile-time column checking.
  /// </summary>
  /// <typeparam name="TSchemaIn">Input schema (must contain feature column)</typeparam>
  /// <typeparam name="TSchemaOut">Output schema (must contain PredictedLabel and Score columns)</typeparam>
  /// <param name="context">MLContext</param>
  /// <param name="featureColumn">Expression selecting the feature column</param>
  /// <param name="numberOfClusters">Number of clusters to find (default: 5)</param>
  /// <param name="exampleWeightColumn">Optional weights column selector</param>
  /// <param name="numberOfThreads">Degree of lock-free parallelism (default: automatic). Set to 1 for deterministic results.</param>
  /// <returns>Type-safe estimator for K-Means clustering</returns>
  /// <example>
  /// <code>
  /// var clusteringEstimator = ClusteringTrainers.KMeans&lt;IFeaturesSchema, IClusteredSchema&gt;(
  ///     mlContext,
  ///     schema => schema.Features,
  ///     numberOfClusters: 3,
  ///     numberOfThreads: 1  // Single-threaded for determinism
  /// );
  /// </code>
  /// </example>
  public static Estimator<TSchemaIn, TSchemaOut> KMeans<TSchemaIn, TSchemaOut>(
      MLContext context,
      Expression<Func<TSchemaIn, object>> featureColumn,
      int numberOfClusters = 5,
      Expression<Func<TSchemaIn, object>>? exampleWeightColumn = null,
      int? numberOfThreads = null)
      where TSchemaIn : ISchemaDefinition
      where TSchemaOut : ISchemaDefinition
  {
    var featureColName = ColumnExpressionExtractor.ExtractColumnName(featureColumn);
    var weightsColName = exampleWeightColumn != null
        ? ColumnExpressionExtractor.ExtractColumnName(exampleWeightColumn)
        : null;

    var trainer = context.Clustering.Trainers.KMeans(
        new Microsoft.ML.Trainers.KMeansTrainer.Options
        {
          FeatureColumnName = featureColName,
          NumberOfClusters = numberOfClusters,
          ExampleWeightColumnName = weightsColName,
          NumberOfThreads = numberOfThreads
        });

    return Estimator<TSchemaIn, TSchemaOut>.From(trainer);
  }
}
