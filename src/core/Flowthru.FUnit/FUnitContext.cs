using System.Reflection;
using Flowthru.Data.Catalog;
using Flowthru.Data.Storage;
using Flowthru.Prelude;
using Flowthru.Validation.Runtime;
using Microsoft.Extensions.DependencyInjection;

namespace Flowthru.Step.Testing;

/// <summary>
/// Framework-agnostic base class for Flowthru step tests. Subclass it
/// in any test framework (NUnit, xUnit, MSTest) to gain typed step
/// invocation, sample-data helpers, pre-flight validation against any
/// <see cref="INode"/>, and a per-test DI service collection
/// auto-populated from
/// <see cref="FUnitStubContainerAttribute"/>-marked classes.
/// </summary>
/// <remarks>
/// <para>
/// Authoring shape (per §2.4 / §4.5):
/// </para>
/// <code>
/// [FlowthruStep]
/// public static class FooStep
/// {
///   public static Func&lt;TIn, TOut&gt; Create() =&gt; input =&gt; { /* … */ };
///
/// #if FUNIT_ENABLED
///   public class Tests : FUnitContext
///   {
///     [FUnitStepTest(typeof(FooStep))]
///     public void TheRightThing() =&gt;
///       Assert.That(Invoke(Create(), sampleInput), Is.EqualTo(expected));
///   }
/// #endif
/// }
/// </code>
/// <para>
/// Per §2.5, the <see cref="Validate"/> sugar method lets tests run
/// pre-flight validation against any <see cref="INode"/> — typically
/// a <see cref="IItem{T}"/>'s adapter inspection, or a step's own
/// validate hook — without leaving the test framework.
/// </para>
/// </remarks>
public class FUnitContext : IDisposable
{
  private bool _disposed;
  private IServiceProvider? _serviceProvider;
  private readonly ServiceCollection _services = new();

  /// <summary>
  /// Constructs the test context and auto-registers any
  /// <see cref="FUnitStubContainerAttribute"/>-attributed static
  /// classes from the derived class's assembly. Per-test or
  /// per-fixture <see cref="Services"/> registrations made after the
  /// constructor runs override stub-container defaults.
  /// </summary>
  public FUnitContext()
  {
    RegisterStubContainers();
  }

  /// <summary>
  /// DI service collection for the test. Register services here
  /// before the first call to <see cref="ServiceProvider"/>; frozen
  /// on first access. Stub registrations from
  /// <see cref="FUnitStubContainerAttribute"/>-marked classes are
  /// added during construction, so per-test code added here runs
  /// second and wins on conflicts.
  /// </summary>
  public IServiceCollection Services => _services;

  /// <summary>Sample-data helpers — see <see cref="SampleBuilder"/>.</summary>
  public SampleBuilder Samples { get; } = new();

  /// <summary>Lazily-built DI provider. Freezes <see cref="Services"/> on first access.</summary>
  protected IServiceProvider ServiceProvider =>
    _serviceProvider ??= _services.BuildServiceProvider();

  /// <summary>
  /// Resolve a required service from the test's DI container. Sugar
  /// for <c>ServiceProvider.GetRequiredService&lt;T&gt;()</c>.
  /// </summary>
  protected T GetRequiredService<T>() where T : notnull =>
    ServiceProvider.GetRequiredService<T>();

  /// <summary>Invoke a synchronous step transform with the given input.</summary>
  protected TOutput Invoke<TInput, TOutput>(Func<TInput, TOutput> step, TInput input) =>
    step(input);

  /// <summary>Invoke an asynchronous step transform with the given input.</summary>
  protected Task<TOutput> InvokeAsync<TInput, TOutput>(
    Func<TInput, Task<TOutput>> step,
    TInput input
  ) => step(input);

  /// <summary>Invoke an asynchronous, cancellable step transform.</summary>
  protected Task<TOutput> InvokeAsync<TInput, TOutput>(
    Func<TInput, CancellationToken, Task<TOutput>> step,
    TInput input,
    CancellationToken cancellationToken = default
  ) => step(input, cancellationToken);

  // ===========================================================================
  // Streaming (FlowSource) invocation
  //
  // The streaming siblings of Invoke/InvokeAsync. A FlowSource's only exit is
  // Compile(), whose terminals return a FlowIO run with .Run(ct) → EffResult.
  // These helpers compile/drain a source (or a step returning one) and unwind
  // the EffResult to a materialised list — mirroring how Validate unwinds an
  // EffResult<ValidationResult> for assertion sites.
  // ===========================================================================

  /// <summary>
  /// Compile and run a <see cref="FlowSource{A}"/> to a materialised list,
  /// unwinding the <see cref="EffResult{A}"/> for assertion. A terminal
  /// <see cref="RuntimeError"/> (a failed compile) surfaces as a thrown
  /// <see cref="InvalidOperationException"/> so <c>Assert.CatchAsync</c> works;
  /// use <see cref="RunStream"/> to inspect the typed error instead.
  /// </summary>
  protected async Task<IReadOnlyList<A>> InvokeStream<A>(
    FlowSource<A> source,
    CancellationToken cancellationToken = default
  )
  {
    var result = await source.Compile().ToList().Run(cancellationToken).ConfigureAwait(false);
    return result switch
    {
      EffResult<IReadOnlyList<A>>.Success ok => ok.Value,
      EffResult<IReadOnlyList<A>>.Failure failure =>
        throw new InvalidOperationException(failure.Error.Message),
      _ => throw new InvalidOperationException("Unreachable: EffResult is a closed sum"),
    };
  }

  /// <summary>
  /// Invoke a streaming step transform over an in-memory sample and materialise
  /// the output — the streaming analogue of <see cref="Invoke{TInput,TOutput}"/>.
  /// </summary>
  protected Task<IReadOnlyList<TOutput>> InvokeStream<TInput, TOutput>(
    Func<FlowSource<TInput>, FlowSource<TOutput>> step,
    IEnumerable<TInput> input,
    CancellationToken cancellationToken = default
  ) => InvokeStream(step(FlowSource.FromEnumerable(input)), cancellationToken);

  /// <summary>
  /// Invoke a streaming step transform over an existing <see cref="FlowSource{A}"/>
  /// input and materialise the output.
  /// </summary>
  protected Task<IReadOnlyList<TOutput>> InvokeStream<TInput, TOutput>(
    Func<FlowSource<TInput>, FlowSource<TOutput>> step,
    FlowSource<TInput> input,
    CancellationToken cancellationToken = default
  ) => InvokeStream(step(input), cancellationToken);

  /// <summary>
  /// Compile and run a <see cref="FlowSource{A}"/>, returning the raw
  /// <see cref="EffResult{A}"/> so a test can assert <em>both</em> error modes:
  /// a <c>Success</c> list of rows, or a terminal <c>Failure</c> carrying the
  /// typed <see cref="RuntimeError"/> from a failed compile. For per-item
  /// (dead-letter) failures, run a <c>FlowSource&lt;EffResult&lt;T&gt;&gt;</c>
  /// through <see cref="InvokeStream{A}"/> and assert on each element, or
  /// collapse it with <c>SkipErrors</c>/<c>Rethrow</c> first.
  /// </summary>
  protected Task<EffResult<IReadOnlyList<A>>> RunStream<A>(
    FlowSource<A> source,
    CancellationToken cancellationToken = default
  ) => source.Compile().ToList().Run(cancellationToken);

  /// <summary>
  /// Drive a <see cref="FlowSource{A}"/> into a sink inside the effect envelope
  /// and return the raw <see cref="EffResult{A}"/>. Use with a
  /// <see cref="RecordingSink{T}"/> (or another <see cref="IFlowSink{T}"/> test
  /// double) to observe the sink's batch lifecycle and its release on every exit
  /// path.
  /// </summary>
  protected Task<EffResult<FlowUnit>> DrainInto<A>(
    FlowSource<A> source,
    IFlowSink<A> sink,
    CancellationToken cancellationToken = default
  ) => source.Compile().Into(sink).Run(cancellationToken);

  /// <summary>
  /// Run pre-flight <see cref="INode.Validate"/> on any DAG node and
  /// surface the inner <see cref="ValidationResult"/> directly. Pure
  /// sugar over <c>node.Validate().Run()</c> with the
  /// <see cref="EffResult{A}"/> wrapper unwound for assertion sites.
  /// </summary>
  protected async Task<ValidationResult> Validate(
    INode node,
    CancellationToken cancellationToken = default
  )
  {
    var result = await node.Validate().Run(cancellationToken).ConfigureAwait(false);
    return result switch
    {
      EffResult<ValidationResult>.Success ok => ok.Value,
      EffResult<ValidationResult>.Failure failure =>
        ValidationResult.FromException(node.Label, new InvalidOperationException(failure.Error.Message)),
      _ => throw new InvalidOperationException("Unreachable: EffResult is a closed sum"),
    };
  }

  /// <summary>
  /// Reflects on the derived test class's assembly to find every
  /// <see cref="FUnitStubContainerAttribute"/>-attributed type, then
  /// invokes each one's
  /// <c>public static void Configure(IServiceCollection)</c> method
  /// against <see cref="_services"/>. Containers without the
  /// expected method signature are silently ignored — the analyzer
  /// reports those separately.
  /// </summary>
  private void RegisterStubContainers()
  {
    var assembly = GetType().Assembly;
    Type[] types;
    try
    {
      types = assembly.GetTypes();
    }
    catch (ReflectionTypeLoadException ex)
    {
      types = ex.Types.Where(t => t is not null).Cast<Type>().ToArray();
    }

    foreach (var type in types)
    {
      if (type.GetCustomAttribute<FUnitStubContainerAttribute>() is null) continue;

      var configureMethod = type.GetMethod(
        name: "Configure",
        bindingAttr: BindingFlags.Public | BindingFlags.Static,
        binder: null,
        types: new[] { typeof(IServiceCollection) },
        modifiers: null
      );
      if (configureMethod is null) continue;

      configureMethod.Invoke(null, new object[] { _services });
    }
  }

  /// <inheritdoc/>
  public void Dispose()
  {
    Dispose(disposing: true);
    GC.SuppressFinalize(this);
  }

  /// <summary>Standard dispose pattern.</summary>
  protected virtual void Dispose(bool disposing)
  {
    if (_disposed) return;
    if (disposing && _serviceProvider is IDisposable disposable) disposable.Dispose();
    _disposed = true;
  }
}
