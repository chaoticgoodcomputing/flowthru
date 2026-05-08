using Flowthru.Prelude;
using Flowthru.Validation.Runtime;

namespace Flowthru.Tests.Kits.Prelude;

/// <summary>
/// The laws of <see cref="FlowIO{A}"/> as a monad. Subclasses bind a concrete
/// representative type for <c>A</c> via the abstract members and inherit a
/// fixed set of law tests covering monad laws (left identity, right identity,
/// associativity), exception capture, cancellation propagation, and basic
/// combinator behaviour.
/// </summary>
/// <typeparam name="A">
/// The success type of effects under test. Subclasses pick a representative
/// type and provide value samples via <see cref="SampleValues"/>.
/// </typeparam>
/// <remarks>
/// <para>
/// <strong>Why a kit instead of inline tests?</strong> Future Core types and
/// extension authors may build their own <see cref="FlowIO{A}"/> values via
/// custom combinators or sub-classing strategies. Inheriting these laws
/// guarantees they preserve the contract; a regression in any combinator
/// surfaces as the same law failing across every consumer.
/// </para>
/// </remarks>
public abstract class FlowIOLaws<A>
{
  /// <summary>
  /// Sample values of type <typeparamref name="A"/> the law tests use to
  /// build representative effects. At least two distinct values recommended.
  /// </summary>
  protected abstract IEnumerable<A> SampleValues { get; }

  /// <summary>
  /// An equality predicate over <typeparamref name="A"/>. Defaults to
  /// <see cref="EqualityComparer{A}.Default"/>; override for types whose
  /// default equality doesn't match the law's intent.
  /// </summary>
  protected virtual bool AreEqual(A a, A b) =>
    EqualityComparer<A>.Default.Equals(a, b);

  // ── Helpers ────────────────────────────────────────────────────────────

  private static async Task<EffResult<TValue>> RunAsync<TValue>(FlowIO<TValue> eff) =>
    await eff.Run(CancellationToken.None);

  private static async Task<TValue> RunSuccessAsync<TValue>(FlowIO<TValue> eff)
  {
    var result = await RunAsync(eff);
    return result switch
    {
      EffResult<TValue>.Success s => s.Value,
      EffResult<TValue>.Failure f =>
        throw new AssertionException($"Expected success, got failure: {f.Error.Message}"),
      _ => throw new InvalidOperationException("Unreachable"),
    };
  }

  private static async Task<RuntimeError> RunFailureAsync<TValue>(FlowIO<TValue> eff)
  {
    var result = await RunAsync(eff);
    return result switch
    {
      EffResult<TValue>.Failure f => f.Error,
      EffResult<TValue>.Success s =>
        throw new AssertionException($"Expected failure, got success: {s.Value}"),
      _ => throw new InvalidOperationException("Unreachable"),
    };
  }

  // ── Monad laws ─────────────────────────────────────────────────────────

  /// <summary>
  /// Left identity: <c>Pure(a).Bind(f) == f(a)</c>. Wrapping a value in
  /// <see cref="FlowIO{A}.Pure"/> and immediately binding is equivalent to
  /// just calling <c>f(a)</c>.
  /// </summary>
  [Test]
  public async Task LeftIdentityLaw()
  {
    foreach (var a in SampleValues)
    {
      Func<A, FlowIO<A>> f = x => FlowIO<A>.Pure(x);
      var lhs = await RunSuccessAsync(FlowIO<A>.Pure(a).Bind(f));
      var rhs = await RunSuccessAsync(f(a));
      Assert.That(AreEqual(lhs, rhs), Is.True,
        $"Left identity violated: Pure({a}).Bind(f) != f({a})");
    }
  }

  /// <summary>
  /// Right identity: <c>m.Bind(Pure) == m</c>. Binding with the pure
  /// constructor leaves the effect unchanged.
  /// </summary>
  [Test]
  public async Task RightIdentityLaw()
  {
    foreach (var a in SampleValues)
    {
      var m = FlowIO<A>.Pure(a);
      var lhs = await RunSuccessAsync(m.Bind(FlowIO<A>.Pure));
      var rhs = await RunSuccessAsync(m);
      Assert.That(AreEqual(lhs, rhs), Is.True,
        $"Right identity violated: m.Bind(Pure) != m for value {a}");
    }
  }

  /// <summary>
  /// Associativity: <c>m.Bind(f).Bind(g) == m.Bind(x => f(x).Bind(g))</c>.
  /// Sequenced binds compose without parenthesisation mattering.
  /// </summary>
  [Test]
  public async Task AssociativityLaw()
  {
    foreach (var a in SampleValues)
    {
      Func<A, FlowIO<A>> f = FlowIO<A>.Pure;
      Func<A, FlowIO<A>> g = FlowIO<A>.Pure;
      var m = FlowIO<A>.Pure(a);

      var lhs = await RunSuccessAsync(m.Bind(f).Bind(g));
      var rhs = await RunSuccessAsync(m.Bind(x => f(x).Bind(g)));

      Assert.That(AreEqual(lhs, rhs), Is.True,
        $"Associativity violated for value {a}");
    }
  }

  // ── Functor laws ───────────────────────────────────────────────────────

  /// <summary>
  /// Functor identity: <c>m.Map(x => x) == m</c>.
  /// </summary>
  [Test]
  public async Task FunctorIdentityLaw()
  {
    foreach (var a in SampleValues)
    {
      var m = FlowIO<A>.Pure(a);
      var lhs = await RunSuccessAsync(m.Map(x => x));
      var rhs = await RunSuccessAsync(m);
      Assert.That(AreEqual(lhs, rhs), Is.True,
        $"Functor identity violated for value {a}");
    }
  }

  // ── Failure-as-value invariant ─────────────────────────────────────────

  /// <summary>
  /// <c>FlowIO.Lift(throwingFunc).Run()</c> returns
  /// <see cref="EffResult{A}.Failure"/> with a
  /// <see cref="RuntimeError.External"/> wrapping the thrown exception —
  /// it does <em>not</em> propagate the exception as an unhandled throw.
  /// </summary>
  [Test]
  public async Task LiftCapturesExceptionsAsExternalError()
  {
    var marker = new InvalidOperationException("intentional");
    var eff = FlowIO<A>.Lift(() => throw marker, source: "test.lift");

    var error = await RunFailureAsync(eff);
    Assert.That(error, Is.InstanceOf<RuntimeError.External>());
    var ext = (RuntimeError.External)error;
    Assert.That(ext.Cause, Is.SameAs(marker));
    Assert.That(ext.Source, Is.EqualTo("test.lift"));
  }

  /// <summary>
  /// <c>FlowIO.LiftAsync</c> with a cancelled token surfaces as
  /// <see cref="RuntimeError.Cancelled"/>, not <see cref="RuntimeError.External"/>.
  /// </summary>
  [Test]
  public async Task LiftAsyncMapsCancellationToCancelledVariant()
  {
    using var cts = new CancellationTokenSource();
    cts.Cancel();

    var eff = FlowIO<A>.LiftAsync(async ct =>
    {
      ct.ThrowIfCancellationRequested();
      await Task.Yield();
      return SampleValues.First();
    });

    var result = await eff.Run(cts.Token);
    Assert.That(result, Is.InstanceOf<EffResult<A>.Failure>());
    var fail = (EffResult<A>.Failure)result;
    Assert.That(fail.Error, Is.InstanceOf<RuntimeError.Cancelled>());
  }

  // ── Bind short-circuits on failure ─────────────────────────────────────

  /// <summary>
  /// <c>Fail(e).Bind(f)</c> does not call <c>f</c>; the failure propagates.
  /// </summary>
  [Test]
  public async Task BindShortCircuitsOnFailure()
  {
    var marker = new RuntimeError.External("test", new InvalidOperationException("x"));
    var called = false;
    var eff = FlowIO<A>.Fail(marker).Bind(_ =>
    {
      called = true;
      return FlowIO<A>.Pure(SampleValues.First());
    });

    var error = await RunFailureAsync(eff);
    Assert.That(called, Is.False, "Bind should not call its continuation on failure");
    Assert.That(error, Is.SameAs(marker));
  }

  /// <summary>
  /// <c>Catch</c> recovers from a failure but passes successes through.
  /// </summary>
  [Test]
  public async Task CatchRecoversFromFailure()
  {
    var marker = new RuntimeError.External("test", new InvalidOperationException("x"));
    var recovered = SampleValues.First();
    var eff = FlowIO<A>.Fail(marker).Catch(_ => FlowIO<A>.Pure(recovered));

    var value = await RunSuccessAsync(eff);
    Assert.That(AreEqual(value, recovered), Is.True);
  }

  /// <summary>
  /// <c>MapError</c> transforms the failure value and leaves successes alone.
  /// </summary>
  [Test]
  public async Task MapErrorTransformsFailureOnly()
  {
    var original = new RuntimeError.External("a", new InvalidOperationException("a"));
    var replacement = new RuntimeError.Cancelled("replaced");

    // On failure path: error gets transformed.
    var failed = FlowIO<A>.Fail(original).MapError(_ => replacement);
    var failedError = await RunFailureAsync(failed);
    Assert.That(failedError, Is.SameAs(replacement));

    // On success path: value is unchanged, MapError doesn't fire.
    var sample = SampleValues.First();
    var succeeded = FlowIO<A>.Pure(sample).MapError(_ => replacement);
    var succeededValue = await RunSuccessAsync(succeeded);
    Assert.That(AreEqual(succeededValue, sample), Is.True);
  }
}
