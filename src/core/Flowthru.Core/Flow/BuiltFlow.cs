using Flowthru.Data.Catalog;

namespace Flowthru.Flow;

/// <summary>
/// Immutable description of a flow ready to run. Holds the
/// topologically-ordered step list and the producer map the slicer
/// needs. Construction is via <see cref="FlowBuilder.Build"/> — flows
/// are not invoked until <c>RunAsync</c> is called.
/// </summary>
/// <remarks>
/// Per §2.6, construction returns a description, not an action. A
/// <see cref="BuiltFlow"/> can be inspected, sliced, and re-run
/// without re-validation.
/// </remarks>
public sealed class BuiltFlow
{
  private readonly IReadOnlyList<IStepNode> _orderedSteps;
  private readonly IReadOnlyDictionary<string, IStepNode> _producerByItemLabel;

  internal BuiltFlow(
    string label,
    IReadOnlyList<IStepNode> orderedSteps,
    IReadOnlyDictionary<string, IStepNode> producerByItemLabel
  )
  {
    Label = label;
    _orderedSteps = orderedSteps;
    _producerByItemLabel = producerByItemLabel;
  }

  /// <summary>The flow's label — used as the slicing key for multi-flow merging (§2.4).</summary>
  public string Label { get; }

  /// <summary>The topologically-ordered step list.</summary>
  public IReadOnlyList<IStepNode> Steps => _orderedSteps;

  /// <summary>
  /// Run the entire flow under default options via the default
  /// <see cref="ParallelFlowScheduler"/>. Convenience for tests +
  /// scripts; production hosting paths resolve their own
  /// <see cref="IFlowScheduler"/> through DI.
  /// </summary>
  public Task<FlowResult> RunAsync(CancellationToken cancellationToken = default) =>
    new ParallelFlowScheduler().ExecuteAsync(this, ExecutionOptions.Default, cancellationToken);

  /// <summary>Run the entire flow under <paramref name="options"/> via the default scheduler.</summary>
  public Task<FlowResult> RunAsync(ExecutionOptions options, CancellationToken cancellationToken = default) =>
    new ParallelFlowScheduler().ExecuteAsync(this, options, cancellationToken);

  /// <summary>
  /// Run only the subgraph that produces the items named in
  /// <paramref name="targetItemLabels"/>. The labels reference
  /// <c>IItem.Label</c>s declared as outputs by some step in
  /// the flow. Equivalent to
  /// <see cref="RunSliceAsync(FlowSliceStrategy, ExecutionOptions?, CancellationToken)"/>
  /// with a <see cref="FlowSliceStrategy.To"/> strategy.
  /// </summary>
  public Task<FlowResult> RunSliceAsync(
    IEnumerable<string> targetItemLabels,
    ExecutionOptions? options = null,
    CancellationToken cancellationToken = default
  )
  {
    var sliced = new BuiltFlow(
      Label,
      FlowSlicing.SliceTo(_orderedSteps, _producerByItemLabel, targetItemLabels),
      _producerByItemLabel
    );
    return new ParallelFlowScheduler().ExecuteAsync(
      sliced,
      options ?? ExecutionOptions.Default,
      cancellationToken
    );
  }

  /// <summary>
  /// Run the subgraph described by <paramref name="strategy"/>. The
  /// strategy may compose primitives (<see cref="FlowSliceStrategy.From"/>,
  /// <see cref="FlowSliceStrategy.To"/>, <see cref="FlowSliceStrategy.Only"/>,
  /// <see cref="FlowSliceStrategy.Flows"/>) via
  /// <see cref="FlowSliceStrategy.And"/> / <see cref="FlowSliceStrategy.Or"/>,
  /// and may use glob wildcards (<c>*</c>, <c>?</c>) in item and step
  /// labels.
  /// </summary>
  public Task<FlowResult> RunSliceAsync(
    FlowSliceStrategy strategy,
    ExecutionOptions? options = null,
    CancellationToken cancellationToken = default
  )
  {
    if (strategy is null) throw new ArgumentNullException(nameof(strategy));
    var sliced = new BuiltFlow(
      Label,
      strategy.Apply(_orderedSteps, _producerByItemLabel),
      _producerByItemLabel
    );
    return new ParallelFlowScheduler().ExecuteAsync(
      sliced,
      options ?? ExecutionOptions.Default,
      cancellationToken
    );
  }
}
