using Flowthru.Data.Catalog;

namespace Flowthru.Flow;

/// <summary>
/// Algebra for constructing a <see cref="BuiltFlow"/>. The arity-specialized
/// <c>AddStep&lt;…&gt;</c> overloads are emitted by the
/// <c>FlowBuilderGenerator</c> source generator on the partial half of
/// this class. The hand-written half here carries the shared state and
/// the <see cref="Build"/> closure.
/// </summary>
/// <remarks>
/// <para>
/// Per §2.4, the type-alignment law is enforced at the call site by the
/// generic constraints on each <c>AddStep</c> overload. End users see a
/// red squiggle when they pass an item of the wrong type to a step;
/// what's actually happening is the Kleisli arrow's type wouldn't
/// compose under the catalog's indexed family.
/// </para>
/// <para>
/// <strong>Arity matrix.</strong> The generator emits <c>AddStep</c>
/// overloads for (M inputs × N outputs) where M ∈ {0..5} and N ∈ {0..5}.
/// Each cell exposes sync, async (<c>Task</c>-returning), and async-with-
/// cancellation-token variants. The transform shape follows .NET
/// convention:
/// </para>
/// <code>
/// // (M=1, N=1) — conventional step
/// builder.AddStep&lt;Customer, OrderTotal&gt;(
///   "compute-total",
///   customer =&gt; OrderTotal.From(customer),
///   inputs: catalog.Customer,
///   outputs: catalog.OrderTotal);
///
/// // (M=0, N=1) — source step (no inputs; <c>inputs:</c> parameter is absent)
/// builder.AddStep&lt;Report&gt;(
///   "build-report",
///   () =&gt; Report.Default,
///   catalog.Report);
///
/// // (M=1, N=0) — sink step (no outputs; <c>outputs:</c> parameter is absent)
/// builder.AddStep&lt;Report&gt;(
///   "publish-report",
///   report =&gt; PublisherClient.Send(report),
///   catalog.Report);
///
/// // (M=0, N=0) — pure side-effect step
/// builder.AddStep("warm-cache", () =&gt; CacheClient.Warm());
/// </code>
/// <para>
/// Zero-arity overloads use .NET-idiomatic transform shapes
/// (<c>Action</c>, <c>Func&lt;T&gt;</c>, <c>Action&lt;T&gt;</c>) so the
/// <c>FlowUnit</c> internal type never appears in user-facing signatures.
/// </para>
/// </remarks>
public sealed partial class FlowBuilder
{
  private readonly List<IStepNode> _steps = new();

  internal FlowBuilder(string label)
  {
    Label = label ?? throw new ArgumentNullException(nameof(label));
  }

  /// <summary>The flow's label — also the slicing key (§2.4).</summary>
  public string Label { get; }

  /// <summary>
  /// Append a pre-constructed <see cref="IStepNode"/>. Used by
  /// extension <c>AddStep</c> variants that need to construct their
  /// own concrete step subclass (e.g., <c>AddPythonStep</c>).
  /// </summary>
  public FlowBuilder Add(IStepNode step)
  {
    if (step is null) throw new ArgumentNullException(nameof(step));
    _steps.Add(step);
    return this;
  }

  /// <summary>
  /// Resolve dependencies and return an immutable
  /// <see cref="BuiltFlow"/>. Throws <see cref="FlowBuildException"/>
  /// on cycle or duplicate-producer violations — those are bugs in
  /// the flow's wiring, surfaced eagerly here instead of at run time.
  /// </summary>
  public BuiltFlow Build()
  {
    var result = DependencyAnalyzer.Analyse(_steps);
    return result switch
    {
      DependencyAnalyzer.Result.Ok ok => new BuiltFlow(Label, ok.Order, ok.ProducerByItemLabel),
      DependencyAnalyzer.Result.CycleDetected c => throw new FlowBuildException(c.Message),
      DependencyAnalyzer.Result.DuplicateProducer d => throw new FlowBuildException(d.Message),
      _ => throw new InvalidOperationException("Unreachable: DependencyAnalyzer.Result is a closed sum"),
    };
  }

  /// <summary>
  /// Convenience entry point: <c>FlowBuilder.CreateFlow(label, p =&gt; …)</c>
  /// returns a <see cref="BuiltFlow"/> in one call, mirroring the
  /// authoring shape from §1.4 / §2.4.
  /// </summary>
  public static BuiltFlow CreateFlow(string label, Action<FlowBuilder> configure)
  {
    if (configure is null) throw new ArgumentNullException(nameof(configure));
    var builder = new FlowBuilder(label);
    configure(builder);
    return builder.Build();
  }
}

/// <summary>Thrown when <see cref="FlowBuilder.Build"/> rejects the wiring.</summary>
public sealed class FlowBuildException : Exception
{
  public FlowBuildException(string message) : base(message) { }
}
