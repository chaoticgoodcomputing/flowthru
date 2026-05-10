namespace Flowthru.Step.Python;

/// <summary>
/// Fluent builder for declaring how a Python service's preflight inspector
/// should be located. Used inside the lambda passed to
/// <see cref="Flowthru.Step.Python.PythonRuntimeOptions.RegisterService(string, System.Action{PythonServiceBuilder})"/>.
/// </summary>
/// <remarks>
/// <para>
/// Mirrors the .NET <c>AddFlowServiceInspector&lt;TService, TInspector&gt;</c>
/// shape: the service class is unmodified user code; the inspector is a
/// separately-defined unit (a Python module exporting an <c>inspect(svc)</c>
/// function) that the framework wires together with the service via this
/// builder.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// python.RegisterService("Services.PyannoteDiarizer", svc => svc
///     .WithInspector("Services.pyannote_diarizer_inspector"));
/// </code>
/// </example>
public sealed class PythonServiceBuilder
{
  private const string DefaultInspectorFunction = "inspect";

  private readonly string _serviceClassPath;
  private string? _inspectorModule;
  private string _inspectorFunction = DefaultInspectorFunction;

  internal PythonServiceBuilder(string serviceClassPath)
  {
    _serviceClassPath = serviceClassPath;
  }

  /// <summary>
  /// Declares the Python module that contains the inspector function.
  /// </summary>
  /// <param name="inspectorModule">
  /// Fully-qualified Python module path — e.g.
  /// <c>"Services.pyannote_diarizer_inspector"</c>.
  /// </param>
  /// <param name="function">
  /// Function name within the module. Defaults to <c>"inspect"</c>;
  /// override only when the project follows a different naming convention.
  /// </param>
  /// <returns>This builder, for chaining.</returns>
  public PythonServiceBuilder WithInspector(
    string inspectorModule,
    string function = DefaultInspectorFunction
  )
  {
    if (string.IsNullOrWhiteSpace(inspectorModule))
    {
      throw new ArgumentException(
        "Inspector module path cannot be null or whitespace.",
        nameof(inspectorModule)
      );
    }
    if (string.IsNullOrWhiteSpace(function))
    {
      throw new ArgumentException(
        "Inspector function name cannot be null or whitespace.",
        nameof(function)
      );
    }

    _inspectorModule = inspectorModule;
    _inspectorFunction = function;
    return this;
  }

  /// <summary>
  /// Materializes the configured registration. Called by
  /// <see cref="Flowthru.Step.Python.PythonRuntimeOptions.RegisterService(string, System.Action{PythonServiceBuilder})"/>
  /// after the user's configure callback has run.
  /// </summary>
  /// <exception cref="InvalidOperationException">
  /// Thrown if no inspector was declared via
  /// <see cref="WithInspector(string, string)"/>. Every registered service
  /// must have a sidecar inspector — that is the whole point of the
  /// registration.
  /// </exception>
  internal PythonServiceRegistration Build()
  {
    if (_inspectorModule is null)
    {
      throw new InvalidOperationException(
        $"RegisterService(\"{_serviceClassPath}\", ...) requires "
          + ".WithInspector(...) to specify the sidecar inspector module."
      );
    }

    return new PythonServiceRegistration(
      ServiceClassPath: _serviceClassPath,
      InspectorModule: _inspectorModule,
      InspectorFunction: _inspectorFunction
    );
  }
}
