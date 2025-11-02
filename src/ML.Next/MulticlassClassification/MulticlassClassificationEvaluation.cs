using System.Linq.Expressions;
using Microsoft.ML;
using Microsoft.ML.Data;
using ML.Next.Core.Columns;
using ML.Next.Core.Schema;

namespace ML.Next.MulticlassClassification;

/// <summary>
/// Type-safe multiclass classification evaluation APIs with compile-time column validation.
/// </summary>
public static class MulticlassClassificationEvaluation
{
  /// <summary>
  /// Evaluate multiclass classification model with compile-time column checking.
  /// </summary>
  /// <typeparam name="TSchema">The schema type containing label and score columns</typeparam>
  /// <param name="context">MLContext</param>
  /// <param name="predictions">Predictions data with schema tracking</param>
  /// <param name="labelColumnSelector">Expression selecting the label column</param>
  /// <param name="scoreColumnSelector">Expression selecting the score column (probability distributions)</param>
  /// <param name="predictedLabelColumnSelector">Optional expression selecting the predicted label column</param>
  /// <param name="topKPredictionCount">Number of top predictions to evaluate (default: 0 for all)</param>
  /// <returns>Multiclass classification metrics</returns>
  /// <example>
  /// <code>
  /// var metrics = MulticlassClassificationEvaluation.Evaluate(
  ///     mlContext,
  ///     predictions,
  ///     labelColumnSelector: schema => schema.Label,
  ///     scoreColumnSelector: schema => schema.Score
  /// );
  /// </code>
  /// </example>
  public static MulticlassClassificationMetrics Evaluate<TSchema>(
    MLContext context,
    DataView<TSchema> predictions,
    Expression<Func<TSchema, object>> labelColumnSelector,
    Expression<Func<TSchema, object>> scoreColumnSelector,
    Expression<Func<TSchema, object>>? predictedLabelColumnSelector = null,
    int topKPredictionCount = 0
  )
    where TSchema : ISchemaDefinition
  {
    var labelColName = ColumnExpressionExtractor.ExtractColumnName(labelColumnSelector);
    var scoreColName = ColumnExpressionExtractor.ExtractColumnName(scoreColumnSelector);
    var predictedLabelColName =
      predictedLabelColumnSelector != null
        ? ColumnExpressionExtractor.ExtractColumnName(predictedLabelColumnSelector)
        : null;

    return context.MulticlassClassification.Evaluate(
      predictions.Underlying,
      labelColumnName: labelColName,
      scoreColumnName: scoreColName,
      predictedLabelColumnName: predictedLabelColName,
      topKPredictionCount: topKPredictionCount
    );
  }
}
