using LanguageExt;
using ML.Next.Core.Schema;

namespace ML.Next.Transform;

/// <summary>
/// Fluent builder for constructing transformation pipelines with compile-time schema tracking.
/// </summary>
/// <typeparam name="TSchema">The current schema in the pipeline</typeparam>
public class PipelineBuilder<TSchema>
    where TSchema : ISchemaDefinition
{
  private readonly DataView<TSchema> _view;
  private readonly Lst<object> _transformers = Lst<object>.Empty;

  internal PipelineBuilder(DataView<TSchema> view)
  {
    _view = view;
  }

  /// <summary>
  /// Gets the current data view.
  /// </summary>
  public DataView<TSchema> View => _view;

  /// <summary>
  /// Apply a transformer to the pipeline, changing the schema type.
  /// </summary>
  /// <typeparam name="TSchemaOut">The new schema after transformation</typeparam>
  /// <param name="transformer">The transformer to apply</param>
  /// <returns>A new pipeline builder with the updated schema</returns>
  public PipelineBuilder<TSchemaOut> Then<TSchemaOut>(
      Transformer<TSchema, TSchemaOut> transformer)
      where TSchemaOut : ISchemaDefinition
  {
    var result = transformer.Transform(_view);

    return result.Match(
        Succ: view => new PipelineBuilder<TSchemaOut>(view),
        Fail: error => throw new InvalidOperationException(
            $"Transformation failed: {error.Message}", error.ToException()));
  }

  /// <summary>
  /// Builds and returns the final transformed data view.
  /// </summary>
  public DataView<TSchema> Build() => _view;
}
