using Flowthru.Data.Catalog;
using Flowthru.Step.Python;
using Flowthru.Validation.Runtime;

namespace Flowthru.Flow;

/// <summary>
/// <c>AddPythonStep</c> extension methods on <see cref="FlowBuilder"/>.
/// The hand-written 1×1 overload lives here; the source generator
/// emits the multi-arity matrix (1..8 inputs × 1..8 outputs, minus the
/// 1×1 cell) into the same <c>partial</c> class.
/// </summary>
/// <remarks>
/// <para>
/// <strong>Pure description.</strong> No <c>AddPythonStep</c> overload
/// performs IO during <see cref="FlowBuilder"/> construction —
/// per CONTRIBUTING.md's "Decision rule 2" the module-import,
/// decorator-presence, and schema-agreement checks all live in the
/// pre-flight phase via <c>PythonStepValidationHook</c>. Failing the
/// flow at construction time would force every test that exercises a
/// flow's shape to spin up a Python environment first; deferring to
/// pre-flight surfaces every Python problem in one accumulated
/// validation result instead of throwing
/// at the first one — see <see cref="Flowthru.Prelude.Validated{TError, TValue}"/>.
/// </para>
/// <para>
/// <strong>Service dependencies.</strong> Pass any
/// <see cref="ServiceRef"/>s via the <c>services</c> argument. The
/// build-time source-generator path bakes them in from
/// <c>@step(services=[…])</c>; the string-based escape hatch leaves
/// them empty unless the caller supplies them. The pre-flight hook
/// detects decorator/declaration disagreement and surfaces it as a
/// structured <c>PythonPreFlightError</c>.
/// </para>
/// </remarks>
public static partial class PythonStepFactory
{
  /// <summary>
  /// Add a Python step with one input and one output.
  /// </summary>
  /// <typeparam name="TIn">Input type — must match the catalog item's element type.</typeparam>
  /// <typeparam name="TOut">Output type — must match the catalog item's element type.</typeparam>
  /// <param name="builder">Flow builder.</param>
  /// <param name="label">Unique step label within the flow.</param>
  /// <param name="module">
  /// Dotted Python module name (e.g. <c>"Flows.DataScience.Steps.train_model"</c>),
  /// resolvable via the executor's configured <c>sys.path</c>.
  /// </param>
  /// <param name="function">Function name within the module.</param>
  /// <param name="input">Catalog item providing the input value.</param>
  /// <param name="output">Catalog item receiving the output value.</param>
  /// <param name="executor">Python executor (subprocess or Python.NET).</param>
  /// <param name="services">
  /// Optional service-dependency list. Each entry is typically a
  /// <c>ServiceRef.External(new PythonServiceRef("Module.Class"))</c>;
  /// pre-flight will dispatch each through
  /// <c>PythonServiceRefDispatcher</c>. Leave <c>null</c> when the
  /// step has no service dependencies.
  /// </param>
  /// <param name="codeVersion">
  /// Optional build-time identity for the step's transform logic.
  /// Typically computed via
  /// <see cref="PythonCodeVersion.Derive(string?, string?, string?)"/>
  /// from the <c>.py</c> source path, interpreter version, and
  /// dependency manifest. When null the step's
  /// <see cref="IStepNode.CodeVersion"/> stays null and downstream
  /// cache-plan consumers treat it as cache-miss.
  /// </param>
  public static FlowBuilder AddPythonStep<TIn, TOut>(
    this FlowBuilder builder,
    string label,
    string module,
    string function,
    IItem<TIn> input,
    IItem<TOut> output,
    IPythonExecutor executor,
    IReadOnlyList<ServiceRef>? services = null,
    string? codeVersion = null
  )
    where TIn : notnull
    where TOut : notnull
  {
    if (builder is null) throw new ArgumentNullException(nameof(builder));
    if (string.IsNullOrWhiteSpace(label))
      throw new ArgumentException("Label cannot be null or whitespace.", nameof(label));
    if (string.IsNullOrWhiteSpace(module))
      throw new ArgumentException("Module name cannot be null or whitespace.", nameof(module));
    if (string.IsNullOrWhiteSpace(function))
      throw new ArgumentException("Function name cannot be null or whitespace.", nameof(function));
    if (input is null) throw new ArgumentNullException(nameof(input));
    if (output is null) throw new ArgumentNullException(nameof(output));
    if (executor is null) throw new ArgumentNullException(nameof(executor));

    var step = new PythonStep<TIn, TOut>(
      label: label,
      moduleName: module,
      functionName: function,
      transform: in_ => executor.Invoke<TIn, TOut>(module, function, in_),
      inputs: new[] { (IItem)input },
      outputs: new[] { (IItem)output },
      loadInputs: () => input.Load(),
      saveOutputs: out_ => output.Save(out_),
      serviceDependencies: services,
      // FlowLabel is stamped by FlowBuilder.Add via IStepNode.OnAddedToFlow —
      // we no longer thread `flowLabel: builder.Label` through every factory,
      // since the chokepoint guarantees every framework-shipped step type
      // inherits its defining flow's label automatically.
      codeVersion: codeVersion
    );

    return builder.Add(step);
  }
}
