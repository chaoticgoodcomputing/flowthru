using Flowthru.Data;
using Flowthru.Pipelines;

namespace Flowthru.Registry;

/// <summary>
/// Base class for pipeline registries that provides type-safe pipeline registration.
/// </summary>
/// <typeparam name="TCatalog">The catalog type that pipelines in this registry will use</typeparam>
/// <remarks>
/// <para>
/// Inherit from this class to create a pipeline registry for your project.
/// The generic type parameter ensures that all registered pipelines use
/// the same catalog type, providing compile-time type safety.
/// </para>
/// <para>
/// <strong>Usage:</strong>
/// <code>
/// public class MyPipelineRegistry : PipelineRegistry&lt;MyCatalog&gt;
/// {
///     protected override void RegisterPipelines(IPipelineRegistrar&lt;MyCatalog&gt; registrar)
///     {
///         registrar.Register("data_processing", DataProcessingPipeline.Create);
///         registrar.Register("data_science", DataSciencePipeline.Create, new ModelOptions());
///     }
/// }
/// </code>
/// </para>
/// <para>
/// <strong>Design Philosophy:</strong>
/// By making the registry generic and typed to a specific catalog, we ensure that:
/// - Pipeline factories receive the correct catalog type (no casting needed)
/// - Refactoring tools can track catalog usage across pipelines
/// - IntelliSense shows correct catalog properties in pipeline definitions
/// </para>
/// </remarks>
public abstract class PipelineRegistry<TCatalog> where TCatalog : DataCatalogBase
{
  /// <summary>
  /// Override this method to register all pipelines in your project.
  /// </summary>
  /// <param name="registrar">The registrar for adding pipelines</param>
  /// <remarks>
  /// <para>
  /// Use the registrar's fluent API to register pipelines, add descriptions, and set tags.
  /// </para>
  /// <para>
  /// Example:
  /// <code>
  /// protected override void RegisterPipelines(IPipelineRegistrar&lt;MyCatalog&gt; registrar)
  /// {
  ///     registrar
  ///         .Register("etl", EtlPipeline.Create)
  ///         .WithDescription("Extract, transform, and load data")
  ///         .WithTags("data-processing", "etl");
  ///         
  ///     registrar
  ///         .Register("train", TrainPipeline.Create, new TrainOptions { Epochs = 100 })
  ///         .WithDescription("Train ML model")
  ///         .WithTags("ml", "training");
  /// }
  /// </code>
  /// </para>
  /// </remarks>
  protected abstract void RegisterPipelines(IPipelineRegistrar<TCatalog> registrar);

  /// <summary>
  /// Builds and returns all registered pipelines for the given catalog.
  /// </summary>
  /// <param name="catalog">The catalog instance to use for pipeline construction</param>
  /// <returns>Dictionary of pipeline names to pipeline instances</returns>
  /// <remarks>
  /// This method is called internally by FlowthruApplication. Users typically
  /// don't call this directly.
  /// </remarks>
  internal Dictionary<string, Pipeline> GetPipelines(TCatalog catalog)
  {
    var registrar = new PipelineRegistrar<TCatalog>(catalog);
    RegisterPipelines(registrar);
    return registrar.Build();
  }
}
