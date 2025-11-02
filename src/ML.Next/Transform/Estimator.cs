using Microsoft.ML;
using LanguageExt;
using LanguageExt.Common;
using ML.Next.Core.Schema;

namespace ML.Next.Transform;

/// <summary>
/// Type-safe estimator that learns from data and produces a transformer.
/// </summary>
/// <typeparam name="TSchemaIn">The input schema</typeparam>
/// <typeparam name="TSchemaOut">The output schema after fitting</typeparam>
public readonly record struct Estimator<TSchemaIn, TSchemaOut>
    where TSchemaIn : ISchemaDefinition
    where TSchemaOut : ISchemaDefinition
{
  /// <summary>
  /// The underlying ML.NET estimator.
  /// </summary>
  internal IEstimator<ITransformer> Underlying { get; init; }

  /// <summary>
  /// Creates a typed estimator from an ML.NET IEstimator.
  /// </summary>
  public static Estimator<TSchemaIn, TSchemaOut> From(IEstimator<ITransformer> estimator) =>
      new() { Underlying = estimator };

  /// <summary>
  /// Fit the estimator to training data, producing a transformer.
  /// </summary>
  /// <param name="data">Training data with known schema</param>
  /// <returns>A fitted transformer, or error</returns>
  public Fin<Transformer<TSchemaIn, TSchemaOut>> Fit(DataView<TSchemaIn> data)
  {
    try
    {
      var transformer = Underlying.Fit(data.Underlying);
      return Fin<Transformer<TSchemaIn, TSchemaOut>>.Succ(
          Transformer<TSchemaIn, TSchemaOut>.From(transformer));
    }
    catch (Exception ex)
    {
      return Fin<Transformer<TSchemaIn, TSchemaOut>>.Fail(Error.New(ex));
    }
  }

  /// <summary>
  /// Appends another estimator to this one, creating a pipeline.
  /// </summary>
  public Estimator<TSchemaIn, TSchemaFinal> Append<TSchemaFinal>(
      Estimator<TSchemaOut, TSchemaFinal> next)
      where TSchemaFinal : ISchemaDefinition
  {
    var composed = Underlying.Append(next.Underlying);
    return Estimator<TSchemaIn, TSchemaFinal>.From(composed);
  }
}
