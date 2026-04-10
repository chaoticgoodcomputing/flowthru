using Flowthru.Core.Data;
using Flowthru.Core.Flows;
using Flowthru.Core.Graph.Validation;

namespace Flowthru.Core.Registry;

/// <summary>
/// Implementation of IFlowRegistrar that builds a dictionary of flows.
/// </summary>
/// <typeparam name="TCatalog">The catalog type that flows will use</typeparam>
internal class FlowRegistrar<TCatalog> : IFlowRegistrar<TCatalog>
  where TCatalog : CatalogAbstract
{
  private readonly TCatalog _catalog;
  private readonly Dictionary<string, Func<Flow>> _factories = new();
  private readonly Dictionary<string, FlowRegistration> _metadata = new();
  private string? _lastRegisteredFlow;

  /// <summary>
  /// Initializes a new instance of FlowRegistrar.
  /// </summary>
  /// <param name="catalog">The catalog instance to pass to Flow factories</param>
  internal FlowRegistrar(TCatalog catalog)
  {
    _catalog = catalog;
  }

  /// <inheritdoc />
  public IFlowRegistrar<TCatalog> Register(string name, Func<TCatalog, Flow> flowFactory)
  {
    if (string.IsNullOrWhiteSpace(name))
    {
      throw new ArgumentException("Flow name cannot be null or empty", nameof(name));
    }

    if (_factories.ContainsKey(name))
    {
      throw new InvalidOperationException($"Flow '{name}' is already registered");
    }

    // Wrap factory to capture catalog
    _factories[name] = () => flowFactory(_catalog);

    // Initialize metadata
    _metadata[name] = new FlowRegistration { Name = name };
    _lastRegisteredFlow = name;

    return this;
  }

  /// <inheritdoc />
  public IFlowRegistrar<TCatalog> Register<TParams>(
    string name,
    Func<TCatalog, TParams, Flow> flowFactory,
    TParams parameters
  )
  {
    if (string.IsNullOrWhiteSpace(name))
    {
      throw new ArgumentException("Flow name cannot be null or empty", nameof(name));
    }

    if (_factories.ContainsKey(name))
    {
      throw new InvalidOperationException($"Flow '{name}' is already registered");
    }

    // Wrap factory to capture catalog and parameters
    _factories[name] = () => flowFactory(_catalog, parameters);

    // Initialize metadata
    _metadata[name] = new FlowRegistration { Name = name };
    _lastRegisteredFlow = name;

    return this;
  }

  /// <inheritdoc />
  public IFlowRegistrar<TCatalog> WithDescription(string description)
  {
    if (_lastRegisteredFlow == null)
    {
      throw new InvalidOperationException(
        "No flow has been registered yet. Call Register() first."
      );
    }

    if (!_metadata.ContainsKey(_lastRegisteredFlow))
    {
      throw new InvalidOperationException($"Flow '{_lastRegisteredFlow}' has not been registered");
    }

    _metadata[_lastRegisteredFlow].Description = description;
    return this;
  }

  /// <inheritdoc />
  public IFlowRegistrar<TCatalog> WithValidation(Action<ValidationOptions> configure)
  {
    if (_lastRegisteredFlow == null)
    {
      throw new InvalidOperationException(
        "No Flow has been registered yet. Call Register() first."
      );
    }

    if (!_metadata.ContainsKey(_lastRegisteredFlow))
    {
      throw new InvalidOperationException($"Flow '{_lastRegisteredFlow}' has not been registered");
    }

    configure(_metadata[_lastRegisteredFlow].ValidationOptions);
    return this;
  }

  /// <summary>
  /// Builds and returns all registered flows with their metadata applied.
  /// </summary>
  /// <returns>Dictionary of flow names to Flow instances</returns>
  internal Dictionary<string, Flow> Build()
  {
    var flows = new Dictionary<string, Flow>();

    foreach (var (name, factory) in _factories)
    {
      // Invoke factory to create flow
      var flow = factory();

      // Apply metadata
      flow.Name = name;

      if (_metadata.TryGetValue(name, out var metadata))
      {
        flow.Description = metadata.Description;
        flow.ValidationOptions = metadata.ValidationOptions;
      }

      flows[name] = flow;
    }

    return flows;
  }
}
