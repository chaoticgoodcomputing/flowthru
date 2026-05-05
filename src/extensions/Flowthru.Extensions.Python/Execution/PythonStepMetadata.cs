namespace Flowthru.Extensions.Python.Execution;

/// <summary>
/// Metadata extracted from a Python step's <c>@step</c> decorator at
/// registration time. Returned by
/// <see cref="IPythonExecutor.ValidateStep(string, string)"/> so callers
/// can flow service-dependency information into <c>FlowStep</c> alongside
/// the existing C# <c>[FlowthruStep]</c> source-generated metadata.
/// </summary>
/// <param name="Services">
/// Fully-qualified Python class paths (e.g.
/// <c>"Services.pyannote_diarizer.PyannoteDiarizer"</c>) of the services
/// the step depends on. Sourced from the decorator's
/// <c>__flowthru_services__</c> attribute. Empty when the step declares
/// no service dependencies.
/// </param>
public sealed record PythonStepMetadata(
  IReadOnlyList<string> Services
)
{
  /// <summary>An empty metadata record — no services, no other metadata.</summary>
  public static PythonStepMetadata Empty { get; } =
    new(System.Array.Empty<string>());
}
