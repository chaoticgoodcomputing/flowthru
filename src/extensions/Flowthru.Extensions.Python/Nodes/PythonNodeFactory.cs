using Flowthru.Data;
using Flowthru.Extensions.Python.Execution;
using Flowthru.Extensions.Python.Runtime;
using Flowthru.Extensions.Python.Validation;
using Flowthru.Pipelines;

namespace Flowthru.Extensions.Python.Nodes;

/// <summary>
/// Extension methods for adding Python nodes to pipelines.
/// </summary>
/// <remarks>
/// <para>
/// <strong>Phases 2-5 implementation:</strong>
/// Hand-written 1×1 AddPythonNode for scalar and tabular input/output (Phase 2-4).
/// Source-generated N×M overloads for multi-I/O support (Phase 5).
/// </para>
/// <para>
/// <strong>Usage:</strong>
/// </para>
/// <code>
/// public static Pipeline Create(
///     Catalog catalog,
///     IPythonExecutor executor,
///     PythonRuntime runtime)
/// {
///     return PipelineBuilder.CreatePipeline(pipeline =>
///     {
///         pipeline.AddPythonNode(
///             label: "Transform",
///             module: "my_nodes.transform",
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
public static partial class PythonNodeFactory
{
  /// <summary>
  /// Adds a Python node with single input and single output.
  /// </summary>
  /// <typeparam name="TInput">Input type (must match catalog entry type).</typeparam>
  /// <typeparam name="TOutput">Output type (must match catalog entry type).</typeparam>
  /// <param name="builder">Pipeline builder instance.</param>
  /// <param name="label">Unique identifier for this node.</param>
  /// <param name="module">
  /// Dotted Python module name (e.g., "Pipelines.DataScience.train_model").
  /// Must be resolvable via sys.path.
  /// </param>
  /// <param name="function">Python function name within the module.</param>
  /// <param name="input">Catalog entry providing input data.</param>
  /// <param name="output">Catalog entry to store output data.</param>
  /// <param name="executor">Python executor for invoking the function.</param>
  /// <param name="runtime">Python runtime for GIL management.</param>
  /// <param name="description">Optional node description.</param>
  /// <returns>This builder for method chaining.</returns>
  /// <remarks>
  /// <para>
  /// <strong>Compile-time type safety:</strong>
  /// Generic type parameters are inferred from catalog entries.
  /// Mismatched types produce compiler errors.
  /// </para>
  /// <para>
  /// <strong>Registration-time validation (Phase 4):</strong>
  /// <list type="bullet">
  /// <item>Module is importable (exists, no syntax errors)</item>
  /// <item>Function exists in module</item>
  /// <item>@node decorator is present</item>
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
  public static PipelineBuilder AddPythonNode<TInput, TOutput>(
    this PipelineBuilder builder,
    string label,
    string module,
    string function,
    ICatalogEntry<TInput> input,
    ICatalogEntry<TOutput> output,
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
    executor.ValidateNode(module, function);

    var wrapper = new PythonNodeWrapper<TInput, TOutput>(executor, module, function);

    // Delegate to the existing AddNode infrastructure
    // This ensures DAG scheduling, dependency analysis, and all other
    // pipeline mechanics work identically for Python and C# nodes
    return builder.AddNode(
      label: label,
      transform: wrapper.GetTransform(),
      input: input,
      output: output,
      description: description
    );
  }
}
