namespace Flowthru.Validation.Runtime;

/// <summary>
/// Language-agnostic identity of a runtime service a step depends on.
/// Used as the element type of <see cref="IStepNode.ServiceDependencies"/>
/// and the carrier through which the pre-flight inspector pipeline
/// resolves and probes services. Core ships the <see cref="CSharp"/>
/// variant; extensions add their own variants via the open
/// <see cref="External"/> case (see <see cref="IExtensionServiceRef"/>).
/// </summary>
/// <remarks>
/// <para>
/// Following the §2.5 pattern: closed sum at Core (private constructor —
/// no derived case can be added outside this file) plus a single
/// <see cref="External"/> variant carrying an
/// <see cref="IExtensionServiceRef"/>. The closed cases get exhaustive
/// pattern matching; the extension case routes to a registered
/// dispatcher by <see cref="IExtensionServiceRef.Category"/>.
/// </para>
/// </remarks>
public abstract record ServiceRef
{
  private ServiceRef() { }

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
  public sealed record CSharp(Type ServiceType) : ServiceRef
  {
    /// <inheritdoc/>
    public override string DagId => ServiceType.FullName ?? ServiceType.Name;

    /// <inheritdoc/>
    public override string DisplayName => ServiceType.Name;
  }

  /// <summary>
  /// An extension-defined service reference. The wrapped
  /// <see cref="IExtensionServiceRef"/> carries the extension's
  /// rendering and routing data; Core's dispatcher resolution uses
  /// <see cref="IExtensionServiceRef.Category"/> to find the matching
  /// <see cref="IServiceRefDispatcher"/> registered with the host.
  /// </summary>
  public sealed record External(IExtensionServiceRef Cause) : ServiceRef
  {
    /// <inheritdoc/>
    public override string DagId => Cause.DagId;

    /// <inheritdoc/>
    public override string DisplayName => Cause.DisplayName;
  }

  /// <summary>Build a <see cref="CSharp"/> ref from a generic type parameter.</summary>
  public static ServiceRef Of<TService>() where TService : class =>
    new CSharp(typeof(TService));

  /// <summary>Build a <see cref="CSharp"/> ref from a runtime <see cref="Type"/>.</summary>
  public static ServiceRef Of(Type serviceType) => new CSharp(serviceType);
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
public interface IExtensionServiceRef
{
  /// <summary>Stable identity for graph keying — must be unique across all extensions.</summary>
  string DagId { get; }

  /// <summary>Short name used in diagnostic output.</summary>
  string DisplayName { get; }

  /// <summary>
  /// Category discriminator used by Core to find the registered
  /// <see cref="IServiceRefDispatcher"/> capable of resolving this kind
  /// of reference (e.g., <c>"python"</c> for Python services).
  /// </summary>
  string Category { get; }
}
