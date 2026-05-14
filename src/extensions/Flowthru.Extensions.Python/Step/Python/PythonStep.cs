using Flowthru.Data.Catalog;
using Flowthru.Data.Storage;
using Flowthru.Prelude;
using Flowthru.Validation.Runtime;

namespace Flowthru.Step.Python;

/// <summary>
/// Typed step archetype for Python-backed transforms. Implements
/// <see cref="IStepNode{TIn, TOut}"/> directly (Core's
/// <see cref="Step{TIn, TOut}"/> is sealed) and exposes
/// <see cref="ModuleName"/> + <see cref="FunctionName"/> so the
/// <c>PythonStepValidationHook</c> can pattern-match
/// <c>case PythonStep&lt;,&gt;</c> rather than reflecting on private
/// fields the way the legacy implementation did.
/// </summary>
/// <remarks>
/// <para>
/// <strong>Pure description.</strong> Constructing a
/// <see cref="PythonStep{TIn, TOut}"/> performs no IO — it captures
/// the executor reference and the module/function pair, nothing more.
/// Module-import, decorator-presence, and schema-agreement checks all
/// live in <c>PythonStepValidationHook</c> and surface as
/// <see cref="Validation.PreFlight.Python.PythonPreFlightError"/>
/// values during pre-flight. This honors CONTRIBUTING.md's "Decision
/// rule 2": environmental concerns belong in the pre-flight phase,
/// not at construction.
/// </para>
/// <para>
/// <strong>Service dependencies.</strong> Pass any
/// <see cref="ServiceRef"/>s explicitly via the
/// <c>serviceDependencies</c> constructor argument. The build-time
/// source generator (Q2 path) bakes them in from
/// <c>@step(services=[…])</c>; the string-based <c>AddPythonStep</c>
/// escape hatch leaves them empty unless the caller passes them. The
/// pre-flight hook surfaces any decorator/declaration disagreement as
/// a structured error.
/// </para>
/// </remarks>
/// <typeparam name="TIn">
/// Transform input type — single-input steps use the input element
/// type directly; multi-input steps use a value tuple.
/// </typeparam>
/// <typeparam name="TOut">Transform output type — same shape rules.</typeparam>
public sealed class PythonStep<TIn, TOut> : IStepNode<TIn, TOut>
{
  private readonly Func<FlowIO<TIn>> _loadInputs;
  private readonly Func<TOut, FlowIO<FlowUnit>> _saveOutputs;

  /// <summary>
  /// Construct a typed Python step description. No IO is performed —
  /// see the type-level remarks for the rationale.
  /// </summary>
  public PythonStep(
    string label,
    string moduleName,
    string functionName,
    Func<TIn, FlowIO<TOut>> transform,
    IReadOnlyList<IItem> inputs,
    IReadOnlyList<IItem> outputs,
    Func<FlowIO<TIn>> loadInputs,
    Func<TOut, FlowIO<FlowUnit>> saveOutputs,
    NodeTraits? traits = null,
    IReadOnlyList<ServiceRef>? serviceDependencies = null,
    string? flowLabel = null,
    string? codeVersion = null
  )
  {
    Label = label ?? throw new ArgumentNullException(nameof(label));
    ModuleName = moduleName ?? throw new ArgumentNullException(nameof(moduleName));
    FunctionName = functionName ?? throw new ArgumentNullException(nameof(functionName));
    Transform = transform ?? throw new ArgumentNullException(nameof(transform));
    Inputs = inputs ?? throw new ArgumentNullException(nameof(inputs));
    Outputs = outputs ?? throw new ArgumentNullException(nameof(outputs));
    _loadInputs = loadInputs ?? throw new ArgumentNullException(nameof(loadInputs));
    _saveOutputs = saveOutputs ?? throw new ArgumentNullException(nameof(saveOutputs));
    Traits = traits ?? new NodeTraits();
    ServiceDependencies = serviceDependencies ?? Array.Empty<ServiceRef>();
    FlowLabel = flowLabel ?? string.Empty;
    CodeVersion = codeVersion;
  }

  /// <inheritdoc/>
  public string Label { get; }

  /// <inheritdoc/>
  public string FlowLabel { get; private set; }

  /// <inheritdoc/>
  public void OnAddedToFlow(string flowLabel)
  {
    if (string.IsNullOrEmpty(FlowLabel))
      FlowLabel = flowLabel;
  }

  /// <inheritdoc/>
  public NodeTraits Traits { get; }

  /// <summary>
  /// Dotted Python module name resolved by the executor's
  /// <c>sys.path</c> at pre-flight and run time.
  /// </summary>
  public string ModuleName { get; }

  /// <summary>Function name within <see cref="ModuleName"/>.</summary>
  public string FunctionName { get; }

  /// <inheritdoc/>
  public Func<TIn, FlowIO<TOut>> Transform { get; }

  /// <inheritdoc/>
  public IReadOnlyList<IItem> Inputs { get; }

  /// <inheritdoc/>
  public IReadOnlyList<IItem> Outputs { get; }

  /// <inheritdoc/>
  public IReadOnlyList<ServiceRef> ServiceDependencies { get; }

  /// <inheritdoc/>
  public string? CodeVersion { get; }

  /// <inheritdoc/>
  public FlowIO<ValidationResult> Validate() =>
    FlowIO.Pure(ValidationResult.Success());

  /// <inheritdoc/>
  public FlowIO<FlowUnit> Execute() =>
    from input in _loadInputs()
    from output in Transform(input)
    from _ in _saveOutputs(output)
    select FlowUnit.Default;
}
