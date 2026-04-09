using Flowthru.Core.Data;
using Flowthru.Core.Flows;
using Flowthru.Core.Graph;
using Flowthru.Extensions.Python.Execution;
using Flowthru.Extensions.Python.Runtime;
using Flowthru.Extensions.Python.Validation;

namespace Flowthru.Extensions.Python.Steps;

/// <summary>
/// Extension methods for adding Python steps to flows.
/// </summary>
/// <remarks>
/// <para>
/// <strong>Phases 2-5 implementation:</strong>
/// Hand-written 1×1 AddPythonStep(Phase 2-4).
/// Source-generated N×M overloads for multi-I/O support (Phase 5).
/// </para>
/// <para>
/// <strong>Usage:</strong>
/// </para>
/// <code>
/// public static Flow Create(
///     Catalog catalog,
///     IPythonExecutor executor,
///     PythonRuntime runtime)
/// {
///     return FlowBuilder.CreateFlow(flow =>
///     {
///         flow.AddPythonStep(
///             label: "Transform",
///             module: "my_steps.transform",
///             function: "process",
///             input: catalog.RawData,
///             output: catalog.ProcessedData,
///             executor: executor,
///             runtime: runtime
///         );
///     });
/// }
/// </code>
/// <para>
/// <strong>Future phases:</strong>
/// <list type="bullet">
/// <item>Phase 6: Async support</item>
/// </list>
/// </para>
/// </remarks>
public static partial class PythonStepFactory
{
  /// <summary>
  /// Adds a Python step with single input and single output.
  /// </summary>
  /// <typeparam name="TInput">Input type (must match catalog item type).</typeparam>
  /// <typeparam name="TOutput">Output type (must match catalog item type).</typeparam>
  /// <param name="builder">Flow builder instance.</param>
  /// <param name="label">Unique identifier for this step.</param>
  /// <param name="module">
  /// Dotted Python module name (e.g., "Flows.DataScience.train_model").
  /// Must be resolvable via sys.path.
  /// </param>
  /// <param name="function">Python function name within the module.</param>
  /// <param name="input">Catalog item providing input data.</param>
  /// <param name="output">Catalog item to store output data.</param>
  /// <param name="executor">Python executor for invoking the function.</param>
  /// <param name="runtime">Python runtime for GIL management.</param>
  /// <param name="description">Optional step description.</param>
  /// <returns>This builder for method chaining.</returns>
  /// <remarks>
  /// <para>
  /// <strong>Compile-time type safety:</strong>
  /// Generic type parameters are inferred from catalog items.
  /// Mismatched types produce compiler errors.
  /// </para>
  /// <para>
  /// <strong>Registration-time validation (Phase 4):</strong>
  /// <list type="bullet">
  /// <item>Module is importable (exists, no syntax errors)</item>
  /// <item>Function exists in module</item>
  /// <item>@step decorator is present</item>
  /// </list>
  /// </para>
  /// <para>
  /// <strong>Pre-flight validation (Phase 4):</strong>
  /// <list type="bullet">
  /// <item>Decorator schemas match C# generic types</item>
  /// <item>Function signature arity is correct</item>
  /// <item>Dry-run with 0-row data validates output structure</item>
  /// </list>
  /// </para>
  /// </remarks>
  public static FlowBuilder AddPythonStep<TInput, TOutput>(
    this FlowBuilder builder,
    string label,
    string module,
    string function,
    INode<TInput> input,
    INode<TOutput> output,
    IPythonExecutor executor,
    string description = ""
  )
  {
    if (builder == null)
    {
      throw new ArgumentNullException(nameof(builder));
    }

    if (string.IsNullOrWhiteSpace(label))
    {
      throw new ArgumentException("Label cannot be null or whitespace.", nameof(label));
    }

    if (string.IsNullOrWhiteSpace(module))
    {
      throw new ArgumentException("Module name cannot be null or whitespace.", nameof(module));
    }

    if (string.IsNullOrWhiteSpace(function))
    {
      throw new ArgumentException("Function name cannot be null or whitespace.", nameof(function));
    }

    if (input == null)
    {
      throw new ArgumentNullException(nameof(input));
    }

    if (output == null)
    {
      throw new ArgumentNullException(nameof(output));
    }

    if (executor == null)
    {
      throw new ArgumentNullException(nameof(executor));
    }

    // Phase 4: Registration-time validation via executor
    executor.ValidateStep(module, function);

    var wrapper = new PythonStepWrapper<TInput, TOutput>(executor, module, function);

    // Delegate to the existing AddStep infrastructure
    // This ensures DAG scheduling, dependency analysis, and all other
    // Flow mechanics work identically for Python and C# steps
    return builder.AddStep(
      label: label,
      transform: wrapper.GetTransform(),
      input: input,
      output: output,
      description: description
    );
  }
}
