namespace Flowthru.Validation.Runtime;

/// <summary>
/// Resolves a <see cref="ServiceRef"/> to its <see cref="ServiceProfile"/>.
/// The resolution seam mirrors <c>IServiceRefDispatcher</c>: Core ships a
/// permissive default, and extensions / host registrations contribute
/// capacity declarations by composing over it. Resolution happens at
/// pre-flight, before any step is dispatched.
/// </summary>
/// <remarks>
/// A future composite implementation will take the conservative meet of
/// layered sources (medium capability, deployment config, explicit
/// registration override) keyed by the resource identity. For now the
/// default returns <see cref="ServiceProfile.Unbounded"/> for every
/// reference, so the scheduler's conflict gating is a no-op until a
/// resource declares a capacity.
/// </remarks>
public interface IServiceProfileProvider
{
  /// <summary>Resolve the profile for <paramref name="dependency"/>.</summary>
  ServiceProfile Resolve(ServiceRef dependency);
}

/// <summary>
/// The permissive default <see cref="IServiceProfileProvider"/> — every
/// service is unbounded and cache-affecting. Registered by
/// <c>AddFlowthru</c> via <c>TryAddSingleton</c> so a host (or extension)
/// can register a composing provider ahead of it.
/// </summary>
public sealed class DefaultServiceProfileProvider : IServiceProfileProvider
{
  /// <inheritdoc/>
  public ServiceProfile Resolve(ServiceRef dependency) => ServiceProfile.Unbounded;
}
