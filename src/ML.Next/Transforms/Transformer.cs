using LanguageExt;
using LanguageExt.Common;
using Microsoft.ML;
using ML.Next.Core.Schema;

namespace ML.Next.Transforms;

/// <summary>
/// Type-safe transformer that tracks input and output schemas at compile-time.
/// </summary>
/// <typeparam name="TSchemaIn">The input schema type</typeparam>
/// <typeparam name="TSchemaOut">The output schema type</typeparam>
/// <remarks>
/// This wrapper around ML.NET's ITransformer provides compile-time guarantees
/// about schema transformations. The type system ensures that transformers
/// can only be composed when their schemas are compatible.
/// </remarks>
public readonly record struct Transformer<TSchemaIn, TSchemaOut>
  where TSchemaIn : ISchemaDefinition
  where TSchemaOut : ISchemaDefinition
{
  /// <summary>
  /// The underlying ML.NET transformer.
  /// </summary>
  internal ITransformer Underlying { get; init; }

  /// <summary>
  /// Creates a typed transformer from an ML.NET ITransformer.
  /// </summary>
  public static Transformer<TSchemaIn, TSchemaOut> From(ITransformer transformer) =>
    new() { Underlying = transformer };

  /// <summary>
  /// Apply transformation with compile-time schema tracking.
  /// </summary>
  /// <param name="input">Input data view with known schema</param>
  /// <returns>Transformed data view with new schema, or error</returns>
  public Fin<DataView<TSchemaOut>> Transform(DataView<TSchemaIn> input)
  {
    try
    {
      var output = Underlying.Transform(input.Underlying);
      return Fin<DataView<TSchemaOut>>.Succ(DataView<TSchemaOut>.From(output));
    }
    catch (Exception ex)
    {
      return Fin<DataView<TSchemaOut>>.Fail(Error.New(ex));
    }
  }

  /// <summary>
  /// Get output schema for validation before transformation.
  /// </summary>
  /// <param name="inputSchema">The input schema</param>
  /// <returns>The predicted output schema, or error</returns>
  public Fin<DataViewSchema> GetOutputSchema(DataViewSchema inputSchema)
  {
    try
    {
      return Fin<DataViewSchema>.Succ(Underlying.GetOutputSchema(inputSchema));
    }
    catch (Exception ex)
    {
      return Fin<DataViewSchema>.Fail(Error.New(ex));
    }
  }

  /// <summary>
  /// Converts to an Option, None if underlying is null.
  /// </summary>
  public Option<Transformer<TSchemaIn, TSchemaOut>> ToOption() =>
    Underlying == null
      ? Option<Transformer<TSchemaIn, TSchemaOut>>.None
      : Option<Transformer<TSchemaIn, TSchemaOut>>.Some(this);
}
