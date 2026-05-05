namespace Flowthru.Extensions.Python.Services;

/// <summary>
/// One service ↔ sidecar-inspector linkage, populated by
/// <see cref="Flowthru.Extensions.Python.Runtime.PythonRuntimeOptions.RegisterService(string, System.Action{PythonServiceBuilder})"/>
/// and consumed by the preflight loop via <see cref="IPythonServiceInspectorRegistry"/>.
/// </summary>
/// <param name="ServiceClassPath">
/// Fully-qualified Python class path of the service — e.g.
/// <c>"Services.PyannoteDiarizer"</c>. Matches the strings emitted by the
/// Python <c>@step(services=[...])</c> decorator's <c>__flowthru_services__</c>
/// attribute, so the preflight loop can look up registrations by exact
/// string equality.
/// </param>
/// <param name="InspectorModule">
/// Fully-qualified Python module path of the sidecar inspector — e.g.
/// <c>"Services.pyannote_diarizer_inspector"</c>.
/// </param>
/// <param name="InspectorFunction">
/// Function name within the inspector module. Defaults to <c>"inspect"</c>
/// when not overridden via
/// <see cref="PythonServiceBuilder.WithInspector(string, string)"/>.
/// </param>
public sealed record PythonServiceRegistration(
  string ServiceClassPath,
  string InspectorModule,
  string InspectorFunction
)
{
  /// <summary>
  /// Module portion of <see cref="ServiceClassPath"/> — everything before
  /// the last dot. Empty for a top-level class with no dotted module.
  /// </summary>
  /// <example>
  /// <c>"Services.pyannote_diarizer.PyannoteDiarizer"</c> →
  /// <c>"Services.pyannote_diarizer"</c>.
  /// </example>
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
  /// after the last dot, or the whole path if it has no dots.
  /// </summary>
  /// <example>
  /// <c>"Services.pyannote_diarizer.PyannoteDiarizer"</c> →
  /// <c>"PyannoteDiarizer"</c>.
  /// </example>
  public string ServiceClass
  {
    get
    {
      var idx = ServiceClassPath.LastIndexOf('.');
      return idx < 0 ? ServiceClassPath : ServiceClassPath.Substring(idx + 1);
    }
  }
}
