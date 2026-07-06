using System.Runtime.CompilerServices;
using Flowthru.Prelude;
using Flowthru.Validation.Runtime;

namespace Flowthru.Core.Tests.Prelude;

/// <summary>
/// Tests for the <see cref="FlowSource{A}"/> streaming primitive — the
/// compile-to-<see cref="FlowIO{A}"/> contract, deferred acquisition,
/// bracketed resource safety, and the terminal/per-item error channels.
/// </summary>
[TestFixture]
public class FlowSourceTests
{
  // ── Compile terminals + lazy combinators ───────────────────────────────

  [Test]
  public async Task ToList_YieldsAllElements()
  {
    var result = await FlowSource.FromEnumerable(new[] { 1, 2, 3 }).Compile().ToList().Run();
    Assert.That(result, Is.InstanceOf<EffResult<IReadOnlyList<int>>.Success>());
    Assert.That(Value(result), Is.EqualTo(new[] { 1, 2, 3 }));
  }

  [Test]
  public async Task MapAndWhere_TransformLazily()
  {
    var result = await FlowSource.FromEnumerable(new[] { 1, 2, 3, 4 })
      .Where(x => x % 2 == 0)
      .Map(x => x * 10)
      .Compile()
      .ToList()
      .Run();

    Assert.That(Value(result), Is.EqualTo(new[] { 20, 40 }));
  }

  [Test]
  public async Task Fold_AccumulatesAcrossTheStream()
  {
    var result = await FlowSource.FromEnumerable(new[] { 1, 2, 3, 4 })
      .Compile()
      .Fold(0, (acc, x) => acc + x)
      .Run();

    Assert.That(((EffResult<int>.Success)result).Value, Is.EqualTo(10));
  }

  [Test]
  public async Task Drain_RunsWithoutMaterialising()
  {
    var seen = new List<int>();
    var result = await FlowSource.FromEnumerable(new[] { 1, 2, 3 })
      .Map(x => { seen.Add(x); return x; })
      .Compile()
      .Drain()
      .Run();

    Assert.That(result, Is.InstanceOf<EffResult<FlowUnit>.Success>());
    Assert.That(seen, Is.EqualTo(new[] { 1, 2, 3 }));
  }

  // ── Deferred acquisition ───────────────────────────────────────────────

  [Test]
  public async Task Acquisition_IsDeferredUntilRun()
  {
    var acquired = false;
    var source = FlowSource.Bracket(
      TrackingResource(onAcquire: () => acquired = true, onRelease: _ => { }),
      (scope, ct) => Range(1, 3, ct)
    );

    var effect = source.Compile().ToList(); // built AND compiled — but not run
    Assert.That(acquired, Is.False, "Acquire must not run until the compiled effect is run.");

    await effect.Run();
    Assert.That(acquired, Is.True);
  }

  [Test]
  public void BuiltButNeverCompiled_AcquiresNothing()
  {
    var acquired = false;
    _ = FlowSource.Bracket(
      TrackingResource(onAcquire: () => acquired = true, onRelease: _ => { }),
      (scope, ct) => Range(1, 3, ct)
    ).Map(x => x + 1); // combinators are pure description

    Assert.That(acquired, Is.False);
  }

  // ── Bracketed resource safety ──────────────────────────────────────────

  [Test]
  public async Task Release_RunsOnCompletion_WithNoError()
  {
    var sequence = new List<string>();
    var source = FlowSource.Bracket(
      Sequenced(sequence),
      (scope, ct) => Range(1, 2, ct)
    );

    await source.Compile().Drain().Run();
    Assert.That(sequence, Is.EqualTo(new[] { "acquire", "release(ok)" }));
  }

  [Test]
  public async Task Release_RunsOnMidStreamFailure_WithBodyError()
  {
    var sequence = new List<string>();
    var source = FlowSource.Bracket(
      Sequenced(sequence),
      (scope, ct) => ThrowAfter(1, ct)
    );

    var result = await source.Compile().Drain().Run();

    Assert.That(result, Is.InstanceOf<EffResult<FlowUnit>.Failure>());
    Assert.That(((EffResult<FlowUnit>.Failure)result).Error, Is.InstanceOf<RuntimeError.External>());
    Assert.That(sequence, Is.EqualTo(new[] { "acquire", "release(External)" }));
  }

  [Test]
  public async Task Release_RunsOnCancellation()
  {
    var sequence = new List<string>();
    using var cts = new CancellationTokenSource();
    var source = FlowSource.Bracket(
      Sequenced(sequence),
      (scope, ct) => CancelAfter(1, cts, ct)
    );

    var result = await source.Compile().Drain().Run(cts.Token);

    Assert.That(result, Is.InstanceOf<EffResult<FlowUnit>.Failure>());
    Assert.That(((EffResult<FlowUnit>.Failure)result).Error, Is.InstanceOf<RuntimeError.Cancelled>());
    Assert.That(sequence, Is.EqualTo(new[] { "acquire", "release(Cancelled)" }));
  }

  // ── Error channel ──────────────────────────────────────────────────────

  [Test]
  public async Task TerminalFailure_IsTheDefault()
  {
    var result = await FlowSource.Lift<int>(ThrowAfter1).Compile().ToList().Run();
    Assert.That(result, Is.InstanceOf<EffResult<IReadOnlyList<int>>.Failure>());
    Assert.That(((EffResult<IReadOnlyList<int>>.Failure)result).Error, Is.InstanceOf<RuntimeError.External>());
  }

  [Test]
  public async Task MapError_TranslatesTheTerminalFailure()
  {
    var mapped = new RuntimeError.SchemaMismatch("test", "translated");
    var result = await FlowSource.Lift<int>(ThrowAfter1)
      .MapError(_ => mapped)
      .Compile()
      .ToList()
      .Run();

    Assert.That(((EffResult<IReadOnlyList<int>>.Failure)result).Error, Is.SameAs(mapped));
  }

  [Test]
  public async Task Attempt_MovesFailureInBandAsTrailingElement()
  {
    var result = await FlowSource.Lift<int>(ThrowAfter1).Attempt().Compile().ToList().Run();
    var rows = Value(result);

    Assert.That(rows, Has.Count.EqualTo(2));
    Assert.That(rows[0], Is.InstanceOf<EffResult<int>.Success>());
    Assert.That(((EffResult<int>.Success)rows[0]).Value, Is.EqualTo(1));
    Assert.That(rows[1], Is.InstanceOf<EffResult<int>.Failure>());
  }

  [Test]
  public async Task SkipErrors_KeepsSuccesses_AndReportsFailures()
  {
    var reported = new List<RuntimeError>();
    var result = await FlowSource.Lift<int>(ThrowAfter1)
      .Attempt()
      .SkipErrors(reported.Add)
      .Compile()
      .ToList()
      .Run();

    Assert.That(Value(result), Is.EqualTo(new[] { 1 }));
    Assert.That(reported, Has.Count.EqualTo(1));
  }

  [Test]
  public async Task Rethrow_ReRaisesTheInBandFailureTerminally()
  {
    var result = await FlowSource.Lift<int>(ThrowAfter1)
      .Attempt()
      .Rethrow()
      .Compile()
      .ToList()
      .Run();

    Assert.That(result, Is.InstanceOf<EffResult<IReadOnlyList<int>>.Failure>());
  }

  // ── helpers ────────────────────────────────────────────────────────────

  private static IReadOnlyList<T> Value<T>(EffResult<IReadOnlyList<T>> result) =>
    ((EffResult<IReadOnlyList<T>>.Success)result).Value;

  private static FlowResource<int> TrackingResource(Action onAcquire, Action<RuntimeError?> onRelease) =>
    FlowResource.Make(
      acquire: FlowIO.Lift(() => { onAcquire(); return 0; }),
      release: (_, error) => FlowIO.Lift(() => { onRelease(error); return FlowUnit.Default; })
    );

  private static FlowResource<int> Sequenced(List<string> sequence) =>
    FlowResource.Make(
      acquire: FlowIO.Lift(() => { sequence.Add("acquire"); return 0; }),
      release: (_, error) => FlowIO.Lift(() =>
      {
        sequence.Add(error is null ? "release(ok)" : $"release({error.GetType().Name})");
        return FlowUnit.Default;
      })
    );

  private static async IAsyncEnumerable<int> Range(int start, int count, [EnumeratorCancellation] CancellationToken ct)
  {
    await Task.CompletedTask.ConfigureAwait(false);
    for (var i = 0; i < count; i++)
    {
      ct.ThrowIfCancellationRequested();
      yield return start + i;
    }
  }

  private static async IAsyncEnumerable<int> ThrowAfter(int emit, [EnumeratorCancellation] CancellationToken ct)
  {
    await Task.CompletedTask.ConfigureAwait(false);
    for (var i = 0; i < emit; i++)
    {
      ct.ThrowIfCancellationRequested();
      yield return i + 1;
    }

    throw new InvalidOperationException("boom");
  }

  private static IAsyncEnumerable<int> ThrowAfter1(CancellationToken ct) => ThrowAfter(1, ct);

  private static async IAsyncEnumerable<int> CancelAfter(
    int emit,
    CancellationTokenSource cts,
    [EnumeratorCancellation] CancellationToken ct
  )
  {
    await Task.CompletedTask.ConfigureAwait(false);
    for (var i = 0; i < emit; i++)
    {
      yield return i + 1;
    }

    cts.Cancel();
    ct.ThrowIfCancellationRequested();
  }
}
