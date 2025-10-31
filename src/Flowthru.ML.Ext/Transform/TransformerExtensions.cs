using Flowthru.ML.Ext.Core.Schema;

namespace Flowthru.ML.Ext.Transform;

/// <summary>
/// Extension methods for transformer composition and pipeline building.
/// </summary>
public static class TransformerExtensions {
  /// <summary>
  /// Compose transformers with compile-time schema type tracking.
  /// The type system proves that the middle schemas are compatible!
  /// </summary>
  /// <typeparam name="TSchemaIn">Initial input schema</typeparam>
  /// <typeparam name="TSchemaMiddle">Intermediate schema (output of first, input of second)</typeparam>
  /// <typeparam name="TSchemaOut">Final output schema</typeparam>
  /// <param name="first">First transformer in the chain</param>
  /// <param name="second">Second transformer in the chain</param>
  /// <returns>A composed transformer from TSchemaIn to TSchemaOut</returns>
  public static Transformer<TSchemaIn, TSchemaOut> Append<TSchemaIn, TSchemaMiddle, TSchemaOut>(
      this Transformer<TSchemaIn, TSchemaMiddle> first,
      Transformer<TSchemaMiddle, TSchemaOut> second)
      where TSchemaIn : ISchemaDefinition
      where TSchemaMiddle : ISchemaDefinition
      where TSchemaOut : ISchemaDefinition {
    // The type system guarantees TSchemaMiddle compatibility!
    // Chain transformers by applying second to first's output
    var composed = new Microsoft.ML.Data.TransformerChain<Microsoft.ML.ITransformer>(
        first.Underlying, second.Underlying);

    return Transformer<TSchemaIn, TSchemaOut>.From(composed);
  }

  /// <summary>
  /// Creates a pipeline builder for fluent transformer composition.
  /// </summary>
  /// <typeparam name="TSchema">The current schema</typeparam>
  /// <param name="view">The data view to start the pipeline from</param>
  /// <returns>A pipeline builder for fluent API</returns>
  public static PipelineBuilder<TSchema> Pipeline<TSchema>(
      this DataView<TSchema> view)
      where TSchema : ISchemaDefinition =>
      new(view);
}
