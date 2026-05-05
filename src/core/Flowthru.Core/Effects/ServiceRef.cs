namespace Flowthru.Core.Effects;

/// <summary>
/// Stable identity for a service that a step depends on. Used by the
/// preflight loop to dispatch reachability probes and by the metadata
/// pipeline to render service nodes in the DAG.
/// </summary>
/// <remarks>
/// <para>
/// A sealed hierarchy distinguishes services that live on the C# side
/// (resolved through <see cref="System.IServiceProvider"/>) from services
/// that live on the Python side (resolved through the Python extension's
/// inspector registry). The metadata layer reads only
/// <see cref="DagId"/> and <see cref="DisplayName"/> — language-agnostic
/// strings — so the Mermaid renderer, JSON metadata provider, and DAG
/// flatteners do not need to know the variant.
/// </para>
/// <para>
/// Equality is value-based (records), so two refs identifying the same
/// underlying service collapse to a single DAG node when grouped by ref.
/// </para>
/// </remarks>
public abstract record ServiceRef
{
  /// <summary>
  /// Stable string identifier used to deduplicate service references
  /// across steps and to key DAG node IDs. Must be unique per logical
  /// service across all variants.
  /// </summary>
  public abstract string DagId { get; }

  /// <summary>
  /// Short, human-readable name for this service — typically the
  /// unqualified class/interface name. Used as the label on DAG service
  /// nodes.
  /// </summary>
  public abstract string DisplayName { get; }

  // ── Variants ────────────────────────────────────────────────────────

  /// <summary>
  /// A C# service type registered with <see cref="System.IServiceProvider"/>.
  /// The runtime resolves the instance via the DI container and dispatches
  /// to the matching <see cref="IFlowthruInspector{TService}"/> registration.
  /// </summary>
  /// <param name="ServiceType">
  /// The .NET <see cref="Type"/> of the service. Must be a concrete or
  /// interface type registered with the DI container.
  /// </param>
  public sealed record CSharp(Type ServiceType) : ServiceRef
  {
    /// <inheritdoc />
    public override string DagId => ServiceType.FullName ?? ServiceType.Name;

    /// <inheritdoc />
    public override string DisplayName => ServiceType.Name;
  }

  /// <summary>
  /// A Python class identified by its fully-qualified module-and-class
  /// path. The Python extension's inspector registry resolves the
  /// matching <c>RegisterService</c> entry and dispatches to the sidecar
  /// inspector module.
  /// </summary>
  /// <param name="ClassPath">
  /// Fully-qualified Python class path (e.g.
  /// <c>"Services.pyannote_diarizer.PyannoteDiarizer"</c>) emitted by the
  /// <c>@step(services=[...])</c> decorator at registration time.
  /// </param>
  public sealed record Python(string ClassPath) : ServiceRef
  {
    /// <inheritdoc />
    public override string DagId => ClassPath;

    /// <inheritdoc />
    public override string DisplayName
    {
      get
      {
        var idx = ClassPath.LastIndexOf('.');
        return idx < 0 ? ClassPath : ClassPath.Substring(idx + 1);
      }
    }
  }

  // ── Conveniences ────────────────────────────────────────────────────

  /// <summary>
  /// Factory for the common case of referring to a C# service by type.
  /// Equivalent to <c>new ServiceRef.CSharp(typeof(T))</c>.
  /// </summary>
  public static ServiceRef Of<T>()
    where T : notnull => new CSharp(typeof(T));

  /// <summary>Factory for a C# service ref from a runtime <see cref="Type"/>.</summary>
  public static ServiceRef Of(Type type) => new CSharp(type);

  /// <summary>Factory for a Python service ref from a fully-qualified class path.</summary>
  public static ServiceRef OfPython(string classPath) => new Python(classPath);
}
