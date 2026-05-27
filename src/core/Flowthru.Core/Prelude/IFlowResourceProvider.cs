namespace Flowthru.Prelude;

/// <summary>
/// Declares a flow-scoped <see cref="IFlowResource"/> that the engine
/// acquires before pre-flight and releases (LIFO) after post-run — the
/// same bracket timing as <see cref="Flowthru.Data.Catalog.CatalogAbstract.Resource"/>,
/// but available to any DI-registered service, not just catalogs.
/// </summary>
/// <remarks>
/// Register the implementing service in the DI container (typically as a
/// singleton alongside other extension infrastructure). The engine
/// discovers all <see cref="IFlowResourceProvider"/> registrations during
/// flow execution and includes their resources in the unified
/// acquire/release loop.
/// </remarks>
public interface IFlowResourceProvider
{
  /// <summary>
  /// The flow-scoped resource to bracket around execution, or <c>null</c>
  /// when no resource is needed for this run.
  /// </summary>
  IFlowResource? FlowResource { get; }
}
