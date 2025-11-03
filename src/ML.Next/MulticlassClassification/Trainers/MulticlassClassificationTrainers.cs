using System.Linq.Expressions;
using Microsoft.ML;
using ML.Next.Core.Columns;
using ML.Next.Core.Schema;
using ML.Next.Transforms;

namespace ML.Next.MulticlassClassification.Trainers;

/// <summary>
/// Type-safe multiclass classification trainers with compile-time column validation.
/// </summary>
public static class MulticlassClassificationTrainers
{
  /// <summary>
  /// SDCA Maximum Entropy multiclass classification trainer with compile-time column checking.
  /// </summary>
  /// <typeparam name="TSchemaIn">Input schema (must contain label and feature columns)</typeparam>
  /// <typeparam name="TSchemaOut">Output schema (must contain PredictedLabel and Score columns)</typeparam>
  /// <param name="context">MLContext</param>
  /// <param name="labelColumnSelector">Expression selecting the label column</param>
  /// <param name="featureColumnSelector">Expression selecting the feature column</param>
  /// <param name="exampleWeightColumnSelector">Optional weights column selector</param>
  /// <param name="l2Regularization">L2 regularization weight (default: 0.1)</param>
  /// <param name="l1Regularization">L1 regularization weight (default: null)</param>
  /// <param name="maximumNumberOfIterations">Maximum iterations (default: null for auto)</param>
  /// <param name="numberOfThreads">Number of threads (default: null for auto). Set to 1 for deterministic results.</param>
  /// <returns>Type-safe estimator for SDCA Maximum Entropy classification</returns>
  /// <example>
  /// <code>
  /// var trainer = MulticlassClassificationTrainers.SdcaMaximumEntropy&lt;IFeaturesSchema, IModelSchema&gt;(
  ///     mlContext,
  ///     labelColumnSelector: schema => schema.KeyColumn,
  ///     featureColumnSelector: schema => schema.Features,
  ///     numberOfThreads: 1  // Single-threaded for determinism
  /// );
  /// </code>
  /// </example>
  public static Estimator<TSchemaIn, TSchemaOut> SdcaMaximumEntropy<TSchemaIn, TSchemaOut>(
    MLContext context,
    Expression<Func<TSchemaIn, object>> labelColumnSelector,
    Expression<Func<TSchemaIn, object>> featureColumnSelector,
    Expression<Func<TSchemaIn, object>>? exampleWeightColumnSelector = null,
    float? l2Regularization = null,
    float? l1Regularization = null,
    int? maximumNumberOfIterations = null,
    int? numberOfThreads = null
  )
    where TSchemaIn : ISchemaDefinition
    where TSchemaOut : ISchemaDefinition
  {
    var labelColName = ColumnExpressionExtractor.ExtractColumnName(labelColumnSelector);
    var featureColName = ColumnExpressionExtractor.ExtractColumnName(featureColumnSelector);
    var weightsColName =
      exampleWeightColumnSelector != null
        ? ColumnExpressionExtractor.ExtractColumnName(exampleWeightColumnSelector)
        : null;

    var options = new Microsoft.ML.Trainers.SdcaMaximumEntropyMulticlassTrainer.Options
    {
      LabelColumnName = labelColName,
      FeatureColumnName = featureColName,
      ExampleWeightColumnName = weightsColName,
    };

    if (l2Regularization.HasValue)
    {
      options.L2Regularization = l2Regularization.Value;
    }

    if (l1Regularization.HasValue)
    {
      options.L1Regularization = l1Regularization.Value;
    }

    if (maximumNumberOfIterations.HasValue)
    {
      options.MaximumNumberOfIterations = maximumNumberOfIterations.Value;
    }

    if (numberOfThreads.HasValue)
    {
      options.NumberOfThreads = numberOfThreads.Value;
    }

    var trainer = context.MulticlassClassification.Trainers.SdcaMaximumEntropy(options);

    return Estimator<TSchemaIn, TSchemaOut>.From(trainer);
  }

  /// <summary>
  /// SDCA Non-Calibrated multiclass classification trainer with compile-time column checking.
  /// </summary>
  /// <typeparam name="TSchemaIn">Input schema (must contain label and feature columns)</typeparam>
  /// <typeparam name="TSchemaOut">Output schema (must contain PredictedLabel and Score columns)</typeparam>
  /// <param name="context">MLContext</param>
  /// <param name="labelColumnSelector">Expression selecting the label column</param>
  /// <param name="featureColumnSelector">Expression selecting the feature column</param>
  /// <param name="exampleWeightColumnSelector">Optional weights column selector</param>
  /// <param name="l2Regularization">L2 regularization weight (default: 0.1)</param>
  /// <param name="l1Regularization">L1 regularization weight (default: null)</param>
  /// <param name="loss">Loss function (default: null for Log)</param>
  /// <returns>Type-safe estimator for SDCA multiclass classification</returns>
  public static Estimator<TSchemaIn, TSchemaOut> SdcaNonCalibrated<TSchemaIn, TSchemaOut>(
    MLContext context,
    Expression<Func<TSchemaIn, object>> labelColumnSelector,
    Expression<Func<TSchemaIn, object>> featureColumnSelector,
    Expression<Func<TSchemaIn, object>>? exampleWeightColumnSelector = null,
    float? l2Regularization = null,
    float? l1Regularization = null,
    Microsoft.ML.Trainers.ISupportSdcaClassificationLoss? loss = null
  )
    where TSchemaIn : ISchemaDefinition
    where TSchemaOut : ISchemaDefinition
  {
    var labelColName = ColumnExpressionExtractor.ExtractColumnName(labelColumnSelector);
    var featureColName = ColumnExpressionExtractor.ExtractColumnName(featureColumnSelector);
    var weightsColName =
      exampleWeightColumnSelector != null
        ? ColumnExpressionExtractor.ExtractColumnName(exampleWeightColumnSelector)
        : null;

    var options = new Microsoft.ML.Trainers.SdcaNonCalibratedMulticlassTrainer.Options
    {
      LabelColumnName = labelColName,
      FeatureColumnName = featureColName,
      ExampleWeightColumnName = weightsColName,
    };

    if (l2Regularization.HasValue)
    {
      options.L2Regularization = l2Regularization.Value;
    }

    if (l1Regularization.HasValue)
    {
      options.L1Regularization = l1Regularization.Value;
    }

    if (loss != null)
    {
      options.Loss = loss;
    }

    var trainer = context.MulticlassClassification.Trainers.SdcaNonCalibrated(options);

    return Estimator<TSchemaIn, TSchemaOut>.From(trainer);
  }
}
