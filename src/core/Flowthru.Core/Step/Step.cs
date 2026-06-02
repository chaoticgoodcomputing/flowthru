using Flowthru.Data.Catalog;
using Flowthru.Data.Storage;
using Flowthru.Validation.Runtime;

namespace Flowthru.Step;

/// <summary>
/// Standard <see cref="IStepNode{TIn, TOut}"/> implementation —
/// closes over a <see cref="Func{T, TResult}"/> transform plus the
/// load/save plumbing for its declared inputs and outputs. The
/// arity-specific glue (packing N items into <typeparamref name="TIn"/>,
/// unpacking <typeparamref name="TOut"/> into M items) is supplied by
/// the <c>FlowBuilder.AddStep</c> overloads via the
/// <c>loadInputs</c> and <c>saveOutputs</c>
/// closures, so this class is arity-agnostic.
/// </summary>
/// <typeparam name="TIn">
/// Transform input type — typically a <see cref="ValueTuple"/>
/// (single-element tuples are unwrapped: arity-1 inputs use the bare
/// element type).
/// </typeparam>
/// <typeparam name="TOut">
/// Transform output type — typically a <see cref="ValueTuple"/> for
/// multi-output steps; arity-1 outputs use the bare element type.
/// </typeparam>
public sealed class Step<TIn, TOut> : IStepNode<TIn, TOut>
{
  private readonly Func<FlowIO<TIn>> _loadInputs;
  private readonly Func<TOut, FlowIO<FlowUnit>> _saveOutputs;

  public Step(
    string label,
    Func<TIn, FlowIO<TOut>> transform,
    IReadOnlyList<IItem> inputs,
    IReadOnlyList<IItem> outputs,
    Func<FlowIO<TIn>> loadInputs,
    Func<TOut, FlowIO<FlowUnit>> saveOutputs,
    NodeTraits? traits = null,
    IReadOnlyList<ServiceDependency>? serviceDependencies = null,
    string? flowLabel = null,
    string? codeVersion = null
  )
  {
    Label = label ?? throw new ArgumentNullException(nameof(label));
    Transform = transform ?? throw new ArgumentNullException(nameof(transform));
    Inputs = inputs ?? throw new ArgumentNullException(nameof(inputs));
    Outputs = outputs ?? throw new ArgumentNullException(nameof(outputs));
    _loadInputs = loadInputs ?? throw new ArgumentNullException(nameof(loadInputs));
    _saveOutputs = saveOutputs ?? throw new ArgumentNullException(nameof(saveOutputs));
    Traits = traits ?? new NodeTraits();
    ServiceDependencies = serviceDependencies ?? Array.Empty<ServiceDependency>();
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

  /// <inheritdoc/>
  public Func<TIn, FlowIO<TOut>> Transform { get; }

  /// <inheritdoc/>
  public IReadOnlyList<IItem> Inputs { get; }

  /// <inheritdoc/>
  public IReadOnlyList<IItem> Outputs { get; }

  /// <inheritdoc/>
  public IReadOnlyList<ServiceDependency> ServiceDependencies { get; }

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
