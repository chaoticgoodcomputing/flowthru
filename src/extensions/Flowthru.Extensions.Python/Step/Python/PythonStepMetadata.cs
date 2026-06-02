namespace Flowthru.Step.Python;

/// <summary>
/// Decorator-derived metadata captured at <see cref="IPythonExecutor.ValidateStep"/>
/// time. Surfaces the <c>@step</c> decorator's contract — declared
/// input schemas, declared output schemas, and the service classes the
/// step depends on — for downstream consumption by the registration
/// flow (<see cref="Flowthru.Validation.Runtime.ServiceDependency"/> wiring) and the pre-flight validation
/// hook (schema-name agreement check).
/// </summary>
/// <param name="Inputs">
/// Schema names declared in <c>@step(inputs=[…])</c>. Order is significant —
/// position N corresponds to the N-th element of the C# input tuple.
/// </param>
/// <param name="Outputs">
/// Schema names declared in <c>@step(outputs=[…])</c>. Order is significant.
/// </param>
/// <param name="Services">
/// Fully-qualified Python class paths declared in <c>@step(services=[…])</c>
/// (e.g. <c>"Services.PyannoteDiarizer"</c>). Empty when the step
/// declares no service dependencies.
/// </param>
public sealed record PythonStepMetadata(
  IReadOnlyList<string> Inputs,
  IReadOnlyList<string> Outputs,
  IReadOnlyList<string> Services
)
{
  /// <summary>An empty metadata record — no inputs, outputs, or services.</summary>
  public static PythonStepMetadata Empty { get; } =
    new(Array.Empty<string>(), Array.Empty<string>(), Array.Empty<string>());
}
