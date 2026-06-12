namespace Flowthru.Validation.Runtime;

/// <summary>
/// An extension's contribution to a <see cref="ServiceDependency"/>'s
/// resolved <see cref="ServiceProfile"/>. Each extension that owns a
/// constrained resource (the Python worker, an EFCore database scope, a
/// rate-limited endpoint) registers a contributor; the
/// <see cref="CompositeServiceProfileProvider"/> aggregates all of them by
/// conservative meet. Mirrors the <c>IStorageMediumProvider</c> /
/// <c>StorageMediumResolver</c> composition pattern.
/// </summary>
/// <remarks>
/// A contributor returns <c>null</c> for any dependency it doesn't
/// recognise — it speaks only for its own resources and stays silent on
/// everything else. Register via <c>AddSingleton&lt;IServiceProfileContributor&gt;(...)</c>
/// (multiple registrations compose); the host wires this inside its
/// extension setup (e.g. <c>UsePython</c>).
/// </remarks>
public interface IServiceProfileContributor
{
  /// <summary>
  /// This contributor's profile for <paramref name="dependency"/>, or
  /// <c>null</c> if it doesn't recognise the dependency.
  /// </summary>
  ServiceProfile? Contribute(ServiceDependency dependency);
}
