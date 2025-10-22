using Flowthru.Data;
using Flowthru.Pipelines;

namespace Flowthru.Registry;

/// <summary>
/// Implementation of IPipelineRegistrar that builds a dictionary of pipelines.
/// </summary>
/// <typeparam name="TCatalog">The catalog type that pipelines will use</typeparam>
internal class PipelineRegistrar<TCatalog> : IPipelineRegistrar<TCatalog>
  where TCatalog : DataCatalogBase {
  private readonly TCatalog _catalog;
  private readonly Dictionary<string, Func<Pipeline>> _factories = new();
  private readonly Dictionary<string, PipelineMetadata> _metadata = new();
  private string? _lastRegisteredPipeline;

  /// <summary>
  /// Initializes a new instance of PipelineRegistrar.
  /// </summary>
  /// <param name="catalog">The catalog instance to pass to pipeline factories</param>
  internal PipelineRegistrar(TCatalog catalog) {
    _catalog = catalog;
  }

  /// <inheritdoc />
  public IPipelineRegistrar<TCatalog> Register(
    string name,
    Func<TCatalog, Pipeline> pipelineFactory) {
    if (string.IsNullOrWhiteSpace(name)) {
      throw new ArgumentException("Pipeline name cannot be null or empty", nameof(name));
    }

    if (_factories.ContainsKey(name)) {
      throw new InvalidOperationException($"Pipeline '{name}' is already registered");
    }

    // Wrap factory to capture catalog
    _factories[name] = () => pipelineFactory(_catalog);

    // Initialize metadata
    _metadata[name] = new PipelineMetadata { Name = name };
    _lastRegisteredPipeline = name;

    return this;
  }

  /// <inheritdoc />
  public IPipelineRegistrar<TCatalog> Register<TParams>(
    string name,
    Func<TCatalog, TParams, Pipeline> pipelineFactory,
    TParams parameters) {
    if (string.IsNullOrWhiteSpace(name)) {
      throw new ArgumentException("Pipeline name cannot be null or empty", nameof(name));
    }

    if (_factories.ContainsKey(name)) {
      throw new InvalidOperationException($"Pipeline '{name}' is already registered");
    }

    // Wrap factory to capture catalog and parameters
    _factories[name] = () => pipelineFactory(_catalog, parameters);

    // Initialize metadata
    _metadata[name] = new PipelineMetadata { Name = name };
    _lastRegisteredPipeline = name;

    return this;
  }

  /// <inheritdoc />
  public IPipelineRegistrar<TCatalog> WithDescription(string description) {
    if (_lastRegisteredPipeline == null) {
      throw new InvalidOperationException("No pipeline has been registered yet. Call Register() first.");
    }

    if (!_metadata.ContainsKey(_lastRegisteredPipeline)) {
      throw new InvalidOperationException($"Pipeline '{_lastRegisteredPipeline}' has not been registered");
    }

    _metadata[_lastRegisteredPipeline].Description = description;
    return this;
  }

  /// <inheritdoc />
  public IPipelineRegistrar<TCatalog> WithTags(params string[] tags) {
    if (_lastRegisteredPipeline == null) {
      throw new InvalidOperationException("No pipeline has been registered yet. Call Register() first.");
    }

    if (!_metadata.ContainsKey(_lastRegisteredPipeline)) {
      throw new InvalidOperationException($"Pipeline '{_lastRegisteredPipeline}' has not been registered");
    }

    _metadata[_lastRegisteredPipeline].Tags = tags.ToList().AsReadOnly();
    return this;
  }

  /// <summary>
  /// Builds and returns all registered pipelines with their metadata applied.
  /// </summary>
  /// <returns>Dictionary of pipeline names to pipeline instances</returns>
  internal Dictionary<string, Pipeline> Build() {
    var pipelines = new Dictionary<string, Pipeline>();

    foreach (var (name, factory) in _factories) {
      // Invoke factory to create pipeline
      var pipeline = factory();

      // Apply metadata
      pipeline.Name = name;

      if (_metadata.TryGetValue(name, out var metadata)) {
        pipeline.Description = metadata.Description;
        pipeline.Tags = metadata.Tags;
      }

      pipelines[name] = pipeline;
    }

    return pipelines;
  }
}
