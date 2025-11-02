using LanguageExt;
using LanguageExt.Common;
using Microsoft.ML;
using ML.Next.Core.Schema;
using ML.Next.Transforms;
using static LanguageExt.Prelude;

namespace ML.Next.Model;

/// <summary>
/// Type-safe prediction engine wrapper that ensures input/output types match the model's schema.
/// </summary>
/// <typeparam name="TInput">Input data class (must match model's input schema)</typeparam>
/// <typeparam name="TOutput">Output data class (must match model's output schema)</typeparam>
public sealed class PredictionEngine<TInput, TOutput>
  where TInput : class
  where TOutput : class, new()
{
  private readonly Microsoft.ML.PredictionEngine<TInput, TOutput> _engine;

  private PredictionEngine(Microsoft.ML.PredictionEngine<TInput, TOutput> engine)
  {
    _engine = engine;
  }

  /// <summary>
  /// Create a prediction engine from a trained transformer with schema tracking.
  /// </summary>
  /// <typeparam name="TSchemaIn">Input schema type</typeparam>
  /// <typeparam name="TSchemaOut">Output schema type</typeparam>
  /// <param name="context">MLContext</param>
  /// <param name="transformer">Trained transformer</param>
  /// <returns>Prediction engine or error</returns>
  public static Fin<PredictionEngine<TInput, TOutput>> Create<TSchemaIn, TSchemaOut>(
    MLContext context,
    Transforms.Transformer<TSchemaIn, TSchemaOut> transformer
  )
    where TSchemaIn : ISchemaDefinition
    where TSchemaOut : ISchemaDefinition
  {
    try
    {
      var engine = context.Model.CreatePredictionEngine<TInput, TOutput>(transformer.Underlying);
      return Fin<PredictionEngine<TInput, TOutput>>.Succ(
        new PredictionEngine<TInput, TOutput>(engine)
      );
    }
    catch (Exception ex)
    {
      return Fin<PredictionEngine<TInput, TOutput>>.Fail(
        Error.New($"Failed to create prediction engine: {ex.Message}", ex)
      );
    }
  }

  /// <summary>
  /// Make a prediction on a single input.
  /// </summary>
  /// <param name="input">Input data</param>
  /// <returns>Prediction result or error</returns>
  public Fin<TOutput> Predict(TInput input)
  {
    try
    {
      var result = _engine.Predict(input);
      return Fin<TOutput>.Succ(result);
    }
    catch (Exception ex)
    {
      return Fin<TOutput>.Fail(Error.New($"Prediction failed: {ex.Message}", ex));
    }
  }
}
