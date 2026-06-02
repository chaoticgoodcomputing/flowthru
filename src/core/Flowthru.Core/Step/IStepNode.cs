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
  /// Chokepoint hook invoked by <c>FlowBuilder.Add</c> when this step
  /// is appended to a flow. The default implementation is a no-op —
  /// hand-rolled <see cref="IStepNode"/> implementations that already
  /// carry a meaningful <see cref="FlowLabel"/> need not opt in.
  /// Framework-shipped concrete step types override this to stamp the
  /// defining flow's label when construction left the slot empty,
  /// closing the drift hazard where an extension factory forgets to
  /// thread <c>flowLabel: builder.Label</c> through its constructor.
  /// Implementations should be idempotent and stamp-if-empty so an
  /// explicit ctor-supplied label is never overwritten.
  /// </summary>
  /// <param name="flowLabel">
  /// The defining flow's label, supplied by the
  /// <c>FlowBuilder</c> appending this step.
  /// </param>
  void OnAddedToFlow(string flowLabel) { /* default: no-op */ }

  /// <summary>
  /// Build-time identity of the step's transform logic. Source-generated
  /// steps (decorated with <see cref="FlowthruStepAttribute"/>) thread the
  /// <c>StepMetadataGenerator</c>-computed SHA-256 prefix of the step
  /// class's normalized source text; the Python extension stamps an
  /// equivalent identity derived from <c>.py</c> source, interpreter
  /// version, and dependency manifest. Default-interface implementation
  /// returns <c>null</c> — the explicit "unknown identity" signal that
  /// downstream cache-plan consumers treat as cache-miss.
  /// </summary>
  /// <remarks>
  /// <para>
  /// A non-null <see cref="CodeVersion"/> is a promise that two runs of
  /// the same step with the same inputs will produce equivalent outputs.
  /// Hand-rolled <see cref="IStepNode"/> implementations should leave
  /// this null unless they can offer that guarantee. Returning a stale
  /// or fabricated value silently invalidates downstream caches — the
  /// null default is the fail-safe.
  /// </para>
  /// <para>
  /// <strong>Scope (v1).</strong> The source-generator-computed digest
  /// covers the step class's own source text only. Cross-assembly
  /// type-symbol changes — e.g., a schema record renamed in another
  /// project — are not reflected. Downstream cache-plan logic must
  /// therefore also incorporate input item digests when deciding cache
  /// hits; <see cref="CodeVersion"/> is one dimension of that identity,
  /// not the whole.
  /// </para>
  /// </remarks>
  string? CodeVersion => null;

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
  /// <see cref="ServiceDependency"/> is resolved by the host's DI container
  /// (for the <see cref="ServiceDependency.CSharp"/> variant) or by a
  /// registered <see cref="IServiceDependencyDispatcher"/> (for the
  /// <see cref="ServiceDependency.External"/> variant) before the step runs.
  /// </summary>
  IReadOnlyList<ServiceDependency> ServiceDependencies { get; }

  /// <summary>
  /// Canonical lowercase identifier for the language a step's transform
  /// was authored in. <c>null</c> or empty signals the host runtime's
  /// primary language (i.e., .NET / C#) — metadata providers should
  /// omit any language tag in that case. Non-default extensions (e.g.,
  /// the Python extension's <c>PythonStep</c>) override to a stable
  /// identifier so renderers can surface the distinction without
  /// taking a dependency on the extension's concrete step type.
  /// </summary>
  /// <remarks>
  /// <para>
  /// The Mermaid metadata provider appends <c>" (value)"</c> to the
  /// rendered step label when this property is non-empty; other
  /// providers map the identifier into their own native distinction.
  /// The renderer itself never enumerates languages — Core declares
  /// the slot, language extensions populate it, providers consume it.
  /// </para>
  /// </remarks>
  string? SourceLanguage => null;

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
