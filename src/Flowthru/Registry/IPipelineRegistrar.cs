using Flowthru.Data;
using Flowthru.Pipelines;

namespace Flowthru.Registry;

/// <summary>
/// Fluent interface for registering pipelines in a type-safe manner.
/// </summary>
/// <typeparam name="TCatalog">The catalog type that pipelines will use</typeparam>
/// <remarks>
/// <para>
/// This interface provides compile-time type safety by tying pipeline factories
/// to a specific catalog type. The registrar validates that all registered
/// pipelines accept the correct catalog type.
/// </para>
/// <para>
/// <strong>Usage:</strong>
/// <code>
/// protected override void RegisterPipelines(IPipelineRegistrar&lt;MyCatalog&gt; registrar)
/// {
///     // Pipeline without parameters
///     registrar.Register("processing", ProcessingPipeline.Create);
///     
///     // Pipeline with parameters
///     registrar.Register("training", TrainPipeline.Create, new TrainOptions());
///     
///     // Add metadata
///     registrar.WithDescription("processing", "Cleans and transforms raw data");
/// }
/// </code>
/// </para>
/// </remarks>
public interface IPipelineRegistrar<TCatalog> where TCatalog : DataCatalogBase {
  /// <summary>
  /// Registers a pipeline with a parameterless factory function.
  /// </summary>
  /// <param name="name">Unique pipeline name</param>
  /// <param name="pipelineFactory">Factory function that creates the pipeline from catalog</param>
  /// <returns>This registrar for method chaining</returns>
  /// <remarks>
  /// Use this overload when the pipeline doesn't require parameters.
  /// </remarks>
  IPipelineRegistrar<TCatalog> Register(
    string name,
    Func<TCatalog, Pipeline> pipelineFactory);

  /// <summary>
  /// Registers a pipeline with a parameterized factory function.
  /// </summary>
  /// <typeparam name="TParams">The type of parameters the pipeline requires</typeparam>
  /// <param name="name">Unique pipeline name</param>
  /// <param name="pipelineFactory">Factory function that creates the pipeline from catalog and parameters</param>
  /// <param name="parameters">Parameter instance to pass to the pipeline</param>
  /// <returns>This registrar for method chaining</returns>
  /// <remarks>
  /// <para>
  /// Use this overload when the pipeline requires configuration parameters.
  /// Parameters are strongly typed and checked at compile time.
  /// </para>
  /// <para>
  /// The factory signature must match: <c>Func&lt;TCatalog, TParams, Pipeline&gt;</c>
  /// </para>
  /// </remarks>
  IPipelineRegistrar<TCatalog> Register<TParams>(
    string name,
    Func<TCatalog, TParams, Pipeline> pipelineFactory,
    TParams parameters);

  /// <summary>
  /// Adds a description to the most recently registered pipeline.
  /// </summary>
  /// <param name="description">Human-readable description of what the pipeline does</param>
  /// <returns>This registrar for method chaining</returns>
  /// <remarks>
  /// Use this overload when fluently chaining after Register().
  /// </remarks>
  IPipelineRegistrar<TCatalog> WithDescription(string description);

  /// <summary>
  /// Adds tags to the most recently registered pipeline.
  /// </summary>
  /// <param name="tags">Tags for categorizing the pipeline</param>
  /// <returns>This registrar for method chaining</returns>
  /// <remarks>
  /// Use this overload when fluently chaining after Register().
  /// Tags can be used for filtering and organizing pipelines (e.g., "etl", "ml", "reporting").
  /// </remarks>
  IPipelineRegistrar<TCatalog> WithTags(params string[] tags);
}
