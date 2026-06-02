namespace Flowthru.Validation.Runtime;

/// <summary>
/// Language-agnostic identity of a runtime service a step depends on.
/// Used as the element type of <see cref="IStepNode.ServiceDependencies"/>
/// and the carrier through which the pre-flight inspector pipeline
/// resolves and probes services. Core ships the <see cref="CSharp"/>
/// variant; extensions add their own variants via the open
/// <see cref="External"/> case (see <see cref="IExtensionServiceDependency"/>).
/// </summary>
/// <remarks>
/// <para>
/// Following the §2.5 pattern: closed sum at Core (private constructor —
/// no derived case can be added outside this file) plus a single
/// <see cref="External"/> variant carrying an
/// <see cref="IExtensionServiceDependency"/>. The closed cases get exhaustive
/// pattern matching; the extension case routes to a registered
/// dispatcher by <see cref="IExtensionServiceDependency.Category"/>.
/// </para>
/// </remarks>
public abstract record ServiceDependency
{
  private ServiceDependency() { }

  /// <summary>Stable identity used as the DAG-node key for this service.</summary>
  public abstract string DagId { get; }

  /// <summary>Short, human-readable name for diagnostics rendering.</summary>
  public abstract string DisplayName { get; }

  /// <summary>
  /// A C# service identified by an interface or class type. The
  /// <see cref="ServiceType"/> is the lookup key used against the host's
  /// <see cref="System.IServiceProvider"/> when resolving the service at
  /// runtime.
  /// </summary>
  public sealed record CSharp(Type ServiceType) : ServiceDependency
  {
    /// <inheritdoc/>
    public override string DagId => ServiceType.FullName ?? ServiceType.Name;

    /// <inheritdoc/>
    public override string DisplayName => ServiceType.Name;
  }

  /// <summary>
  /// An extension-defined service reference. The wrapped
  /// <see cref="IExtensionServiceDependency"/> carries the extension's
  /// rendering and routing data; Core's dispatcher resolution uses
  /// <see cref="IExtensionServiceDependency.Category"/> to find the matching
  /// <see cref="IServiceDependencyDispatcher"/> registered with the host.
  /// </summary>
  public sealed record External(IExtensionServiceDependency Cause) : ServiceDependency
  {
    /// <inheritdoc/>
    public override string DagId => Cause.DagId;

    /// <inheritdoc/>
    public override string DisplayName => Cause.DisplayName;
  }

  /// <summary>
  /// A C# service identified by an interface or class type, classified
  /// as <em>observation-only</em>: its calls don't affect the step's
  /// output values, only the IO timeline. DI resolution is identical
  /// to <see cref="CSharp"/> — the <see cref="ServiceType"/> is the
  /// lookup key used against the host's
  /// <see cref="System.IServiceProvider"/> at runtime — but the cache
  /// planner skips refs of this variant when deciding step
  /// cacheability. The canonical (and currently only) recognised case
  /// is <c>Microsoft.Extensions.Logging.ILogger</c>; the
  /// <c>[FlowthruStep]</c> source generator emits this variant when a
  /// <c>Create()</c> parameter's fully-qualified type matches a
  /// hardcoded set.
  /// </summary>
  /// <remarks>
  /// <para>
  /// This variant exists because the smart-caching
  /// planner's invariant is "any service dep makes a step
  /// uncacheable" (and cascades downstream) — true for domain
  /// services whose state can change the step's output (e.g.,
  /// <c>IRemoteTimeService</c>) but wrong for pure observation
  /// surfaces. Without this carve-out, declaring a logger
  /// uncacheabilises every Flow in the workspace.
  /// </para>
  /// </remarks>
  public sealed record ObservationOnly(Type ServiceType) : ServiceDependency
  {
    /// <inheritdoc/>
    public override string DagId => ServiceType.FullName ?? ServiceType.Name;

    /// <inheritdoc/>
    public override string DisplayName => ServiceType.Name;
  }

  /// <summary>Build a <see cref="CSharp"/> ref from a generic type parameter.</summary>
  public static ServiceDependency Of<TService>() where TService : class =>
    new CSharp(typeof(TService));

  /// <summary>Build a <see cref="CSharp"/> ref from a runtime <see cref="Type"/>.</summary>
  public static ServiceDependency Of(Type serviceType) => new CSharp(serviceType);
}

/// <summary>
/// Open extension point for extension-defined service references. An
/// extension implements this interface to surface its domain-specific
/// service identities through Core's standard dispatcher pipeline.
/// </summary>
/// <remarks>
/// <para>
/// Implementations live in
/// <c>Flowthru.Validation.Runtime.&lt;ExtensionName&gt;</c> sub-namespaces
/// per the namespace convention. The <see cref="Category"/> discriminator
/// lets Core's dispatcher resolution route lookups without coupling to
/// the concrete extension type.
/// </para>
/// </remarks>
public interface IExtensionServiceDependency
{
  /// <summary>Stable identity for graph keying — must be unique across all extensions.</summary>
  string DagId { get; }

  /// <summary>Short name used in diagnostic output.</summary>
  string DisplayName { get; }

  /// <summary>
  /// Category discriminator used by Core to find the registered
  /// <see cref="IServiceDependencyDispatcher"/> capable of resolving this kind
  /// of reference (e.g., <c>"python"</c> for Python services).
  /// </summary>
  string Category { get; }
}
