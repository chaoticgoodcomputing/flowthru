using System.Linq.Expressions;
using Microsoft.ML;
using Microsoft.ML.Data;
using ML.Next.Core.Schema;
using ML.Next.Core.Columns;

namespace ML.Next.Load;

/// <summary>
/// Type-safe clustering evaluation APIs with compile-time column validation.
/// </summary>
public static class ClusteringEvaluation
{
  /// <summary>
  /// Evaluate clustering model with compile-time column checking.
  /// </summary>
  /// <typeparam name="TSchema">The schema type containing score and feature columns</typeparam>
  /// <param name="context">MLContext</param>
  /// <param name="predictions">Predictions data with schema tracking</param>
  /// <param name="scoreColumn">Expression selecting the score column (distances to centroids)</param>
  /// <param name="featureColumn">Expression selecting the feature column</param>
  /// <param name="labelColumn">Optional expression selecting the label column</param>
  /// <returns>Clustering metrics</returns>
  /// <example>
  /// <code>
  /// var metrics = ClusteringEvaluation.Evaluate(
  ///     mlContext,
  ///     predictions,
  ///     schema => schema.Score,
  ///     schema => schema.Features
  /// );
  /// </code>
  /// </example>
  public static ClusteringMetrics Evaluate<TSchema>(
      MLContext context,
      DataView<TSchema> predictions,
      Expression<Func<TSchema, object>> scoreColumn,
      Expression<Func<TSchema, object>> featureColumn,
      Expression<Func<TSchema, object>>? labelColumn = null)
      where TSchema : ISchemaDefinition
  {
    var scoreColName = ColumnExpressionExtractor.ExtractColumnName(scoreColumn);
    var featureColName = ColumnExpressionExtractor.ExtractColumnName(featureColumn);
    var labelColName = labelColumn != null
        ? ColumnExpressionExtractor.ExtractColumnName(labelColumn)
        : null;

    return context.Clustering.Evaluate(
        predictions.Underlying,
        scoreColumnName: scoreColName,
        featureColumnName: featureColName,
        labelColumnName: labelColName);
  }
}
