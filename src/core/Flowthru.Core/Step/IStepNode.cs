using Flowthru.Data.Catalog;
using Flowthru.Data.Storage;
using Flowthru.Validation.Runtime;

namespace Flowthru.Step;

/// <summary>
/// Engine-level umbrella for step nodes — the "arrow" archetype of
/// <see cref="INode"/>. Carries the bookkeeping the dependency
/// analyzer and the executor need (declared inputs, declared outputs,
/// service dependencies) plus a non-generic <see cref="Execute"/>
/// dispatch that hides the typed transform behind the engine surface.
/// </summary>
/// <remarks>
/// <para>
/// Per §2.4, the bipartite practical structure stays: items are
/// places, steps are arrows. <see cref="Inputs"/> and
/// <see cref="Outputs"/> reference <see cref="IItem"/> instances by
/// design; the typing on <see cref="IStepNode{TIn, TOut}"/> ties the
/// I/O collection element types to the transform's
/// <c>TIn</c>/<c>TOut</c> at the
/// construction site.
/// </para>
/// </remarks>
public interface IStepNode : INode
{
  /// <summary>
  /// Label of the flow that originally declared this step. Empty when
  /// the step was constructed outside a <c>FlowBuilder</c> context
  /// (e.g., a hand-rolled <see cref="IStepNode"/> implementation that
  /// hasn't tagged itself).
  /// </summary>
  /// <remarks>
  /// Survives merging: when <c>FlowthruService</c> merges multiple
  /// registered flows into a single execution DAG, each step's
  /// <see cref="FlowLabel"/> still names its flow of origin so
  /// downstream metadata renderers can group / colour / cross-link
  /// per-flow even within the merged graph. Default-interface-method
  /// returning <c>""</c> preserves source compatibility with existing
  /// implementors that haven't been updated yet.
  /// </remarks>
  string FlowLabel => string.Empty;

  /// <summary>
  /// Items this step reads at the start of <see cref="Execute"/>.
  /// </summary>
  IReadOnlyList<IItem> Inputs { get; }

  /// <summary>
  /// Items this step writes at the end of <see cref="Execute"/>.
  /// </summary>
  IReadOnlyList<IItem> Outputs { get; }

  /// <summary>
  /// Runtime services this step depends on. Each
  /// <see cref="ServiceRef"/> is resolved by the host's DI container
  /// (for the <see cref="ServiceRef.CSharp"/> variant) or by a
  /// registered <see cref="IServiceRefDispatcher"/> (for the
  /// <see cref="ServiceRef.External"/> variant) before the step runs.
  /// </summary>
  IReadOnlyList<ServiceRef> ServiceDependencies { get; }

  /// <summary>
  /// Untyped, end-to-end execution: load each input item, run the
  /// transform, save each output item, propagate the first failure.
  /// The engine names this without knowing the typed shape of the
  /// step.
  /// </summary>
  FlowIO<FlowUnit> Execute();
}

/// <summary>
/// Typed step archetype — the strongly-typed view of an
/// <see cref="IStepNode"/>. Adds the
/// <see cref="Transform"/> delegate that takes
/// <typeparamref name="TIn"/> (typically a value tuple of input
/// element types) and produces a <see cref="FlowIO{A}"/> of
/// <typeparamref name="TOut"/> (typically a value tuple of output
/// element types). The framework wraps the user's
/// <see cref="FlowthruStepAttribute"/>-decorated factory into this
/// shape at <c>FlowBuilder.AddStep</c> time.
/// </summary>
/// <typeparam name="TIn">
/// Input value type. Single-input steps use the input's element type
/// directly; multi-input steps use a value tuple.
/// </typeparam>
/// <typeparam name="TOut">
/// Output value type. Single-output steps use the output's element
/// type directly; multi-output steps use a value tuple.
/// </typeparam>
public interface IStepNode<TIn, TOut> : IStepNode
{
  /// <summary>The transform — the Kleisli arrow this step represents.</summary>
  Func<TIn, FlowIO<TOut>> Transform { get; }
}
