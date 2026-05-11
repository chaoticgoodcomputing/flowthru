namespace Flowthru.Validation.Runtime.Python;

/// <summary>
/// Identity of a Python service a step depends on. The
/// <see cref="ClassPath"/> is the fully-qualified Python class path
/// (e.g. <c>"Services.PyannoteDiarizer"</c>) emitted by the
/// <c>@step(services=[...])</c> decorator's
/// <c>__flowthru_services__</c> attribute. Pre-flight resolves it
/// through Core's <see cref="IServiceRefDispatcher"/> pipeline by
/// matching <see cref="Category"/> to <c>"python"</c> and dispatching
/// to <c>PythonServiceRefDispatcher</c>.
/// </summary>
/// <remarks>
/// Per §4.8 / §2.5, language-agnostic services live behind the
/// <see cref="ServiceRef.External"/> open-extension variant — Core
/// has no <c>ServiceRef.Python</c> case, and the extension supplies
/// its own identity via <see cref="IExtensionServiceRef"/>.
/// </remarks>
public sealed record PythonServiceRef(string ClassPath) : IExtensionServiceRef
{
  /// <inheritdoc/>
  public string DagId => $"python:{ClassPath}";

  /// <inheritdoc/>
  public string DisplayName
  {
    get
    {
      var idx = ClassPath.LastIndexOf('.');
      return idx < 0 ? ClassPath : ClassPath.Substring(idx + 1);
    }
  }

  /// <inheritdoc/>
  public string Category => "python";

  /// <summary>
  /// Module portion of <see cref="ClassPath"/> — everything before the
  /// last dot. Empty for a top-level class.
  /// </summary>
  public string ServiceModule
  {
    get
    {
      var idx = ClassPath.LastIndexOf('.');
      return idx < 0 ? string.Empty : ClassPath.Substring(0, idx);
    }
  }

  /// <summary>
  /// Class-name portion of <see cref="ClassPath"/> — everything after
  /// the last dot, or the whole path if there is no dot.
  /// </summary>
  public string ServiceClass => DisplayName;

  /// <summary>
  /// Convenience: wrap this <see cref="PythonServiceRef"/> in the
  /// Core-side <see cref="ServiceRef.External"/> envelope so it can be
  /// passed to <c>FlowBuilder.Add(IStepNode)</c> step constructors that
  /// accept <c>IReadOnlyList&lt;ServiceRef&gt;</c>.
  /// </summary>
  public ServiceRef AsServiceRef() => new ServiceRef.External(this);
}
