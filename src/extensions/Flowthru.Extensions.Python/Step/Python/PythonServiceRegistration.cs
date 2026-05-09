namespace Flowthru.Step.Python;

/// <summary>
/// One service ↔ sidecar-inspector linkage, populated by
/// <c>UsePython(o =&gt; o.RegisterService(...))</c> and consumed by
/// <c>PythonServiceRefDispatcher</c> through the inspector registry.
/// </summary>
/// <param name="ServiceClassPath">
/// Fully-qualified Python class path of the service —
/// e.g. <c>"Services.PyannoteDiarizer"</c>. Matches the strings emitted
/// by <c>@step(services=[…])</c>'s <c>__flowthru_services__</c>
/// attribute, so the pre-flight pipeline can look up registrations by
/// exact string equality on
/// <see cref="Validation.Runtime.Python.PythonServiceRef.ClassPath"/>.
/// </param>
/// <param name="InspectorModule">
/// Fully-qualified Python module path of the sidecar inspector —
/// e.g. <c>"Services.pyannote_diarizer_inspector"</c>.
/// </param>
/// <param name="InspectorFunction">
/// Function name within the inspector module. Defaults to <c>"inspect"</c>;
/// override only when the project follows a different naming convention.
/// </param>
public sealed record PythonServiceRegistration(
  string ServiceClassPath,
  string InspectorModule,
  string InspectorFunction
)
{
  /// <summary>
  /// Module portion of <see cref="ServiceClassPath"/> — everything
  /// before the last dot.
  /// </summary>
  public string ServiceModule
  {
    get
    {
      var idx = ServiceClassPath.LastIndexOf('.');
      return idx < 0 ? string.Empty : ServiceClassPath.Substring(0, idx);
    }
  }

  /// <summary>
  /// Class-name portion of <see cref="ServiceClassPath"/> — everything
  /// after the last dot, or the whole path when there is no dot.
  /// </summary>
  public string ServiceClass
  {
    get
    {
      var idx = ServiceClassPath.LastIndexOf('.');
      return idx < 0 ? ServiceClassPath : ServiceClassPath.Substring(idx + 1);
    }
  }
}
