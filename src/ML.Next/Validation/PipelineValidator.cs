using Microsoft.ML;
using LanguageExt;
using LanguageExt.Common;
using ML.Next.Core.Schema;
using static LanguageExt.Prelude;

namespace ML.Next.Validation;

/// <summary>
/// End-to-end pipeline validation with accumulated error reporting.
/// </summary>
public static class PipelineValidator
{
  /// <summary>
  /// Validate an entire ETL pipeline by checking:
  /// - Data loading succeeds
  /// - Schema requirements are met
  /// - All transformations are compatible
  /// </summary>
  /// <typeparam name="TSchemaIn">Input schema</typeparam>
  /// <typeparam name="TSchemaOut">Output schema</typeparam>
  /// <param name="dataLoader">Function to load data</param>
  /// <param name="transformer">Pipeline transformer</param>
  /// <returns>Success or accumulated errors</returns>
  public static Validation<Error, DataView<TSchemaOut>> ValidatePipeline<TSchemaIn, TSchemaOut>(
      Func<Fin<DataView<TSchemaIn>>> dataLoader,
      Transform.Transformer<TSchemaIn, TSchemaOut> transformer)
      where TSchemaIn : ISchemaDefinition
      where TSchemaOut : ISchemaDefinition
  {
    // Attempt to load data
    var dataResult = dataLoader();

    return dataResult.Match(
        Succ: data =>
        {
          // Attempt to transform
          var transformResult = transformer.Transform(data);

          return transformResult.Match(
                  Succ: output => Success<Error, DataView<TSchemaOut>>(output),
                  Fail: err => Fail<Error, DataView<TSchemaOut>>(LanguageExt.Seq.create(err))
              );
        },
        Fail: err => Fail<Error, DataView<TSchemaOut>>(LanguageExt.Seq.create(err))
    );
  }

  /// <summary>
  /// Validate that an estimator can be successfully fitted on the provided data.
  /// </summary>
  /// <typeparam name="TSchemaIn">Input schema</typeparam>
  /// <typeparam name="TSchemaOut">Output schema</typeparam>
  /// <param name="data">Training data</param>
  /// <param name="estimator">Estimator to fit</param>
  /// <returns>Success with fitted transformer or accumulated errors</returns>
  public static Validation<Error, Transform.Transformer<TSchemaIn, TSchemaOut>> ValidateEstimatorFit<TSchemaIn, TSchemaOut>(
      DataView<TSchemaIn> data,
      Transform.Estimator<TSchemaIn, TSchemaOut> estimator)
      where TSchemaIn : ISchemaDefinition
      where TSchemaOut : ISchemaDefinition
  {
    var fitResult = estimator.Fit(data);

    return fitResult.Match(
        Succ: transformer => Success<Error, Transform.Transformer<TSchemaIn, TSchemaOut>>(transformer),
        Fail: err => Fail<Error, Transform.Transformer<TSchemaIn, TSchemaOut>>(LanguageExt.Seq.create(err))
    );
  }
}
