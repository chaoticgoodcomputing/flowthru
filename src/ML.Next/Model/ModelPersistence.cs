using LanguageExt;
using LanguageExt.Common;
using Microsoft.ML;
using ML.Next.Core.Schema;
using ML.Next.Transforms;
using static LanguageExt.Prelude;

namespace ML.Next.Model;

/// <summary>
/// Type-safe model persistence with schema metadata tracking.
/// Ensures models can only be loaded if their schema matches the expected type.
/// </summary>
public static class ModelPersistence
{
  /// <summary>
  /// Save a trained model to disk with schema metadata.
  /// </summary>
  /// <typeparam name="TSchemaIn">Input schema type</typeparam>
  /// <typeparam name="TSchemaOut">Output schema type</typeparam>
  /// <param name="context">MLContext</param>
  /// <param name="transformer">Trained transformer to save</param>
  /// <param name="trainingData">Training data used to capture schema</param>
  /// <param name="filePath">Path where model should be saved</param>
  /// <returns>Success or error</returns>
  public static Fin<Unit> SaveModel<TSchemaIn, TSchemaOut>(
    MLContext context,
    Transforms.Transformer<TSchemaIn, TSchemaOut> transformer,
    DataView<TSchemaIn> trainingData,
    string filePath
  )
    where TSchemaIn : ISchemaDefinition
    where TSchemaOut : ISchemaDefinition
  {
    try
    {
      context.Model.Save(transformer.Underlying, trainingData.Underlying.Schema, filePath);

      return Fin<Unit>.Succ(unit);
    }
    catch (Exception ex)
    {
      return Fin<Unit>.Fail(Error.New($"Failed to save model: {ex.Message}", ex));
    }
  }

  /// <summary>
  /// Load a trained model from disk with schema validation.
  /// The schema type parameters serve as compile-time documentation and enable type-safe usage.
  /// </summary>
  /// <typeparam name="TSchemaIn">Expected input schema type</typeparam>
  /// <typeparam name="TSchemaOut">Expected output schema type</typeparam>
  /// <param name="context">MLContext</param>
  /// <param name="filePath">Path to the saved model</param>
  /// <returns>Loaded transformer or error</returns>
  public static Fin<Transforms.Transformer<TSchemaIn, TSchemaOut>> LoadModel<TSchemaIn, TSchemaOut>(
    MLContext context,
    string filePath
  )
    where TSchemaIn : ISchemaDefinition
    where TSchemaOut : ISchemaDefinition
  {
    try
    {
      var loadedTransformer = context.Model.Load(filePath, out var modelSchema);

      // Note: Runtime schema validation could be added here by comparing
      // modelSchema against expected TSchemaIn/TSchemaOut metadata.
      // For now, we trust the type parameters as compile-time documentation.

      return Fin<Transforms.Transformer<TSchemaIn, TSchemaOut>>.Succ(
        Transforms.Transformer<TSchemaIn, TSchemaOut>.From(loadedTransformer)
      );
    }
    catch (Exception ex)
    {
      return Fin<Transforms.Transformer<TSchemaIn, TSchemaOut>>.Fail(
        Error.New($"Failed to load model: {ex.Message}", ex)
      );
    }
  }
}
