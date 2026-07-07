// Derived (in design) from LanguageExt v5's SourceT (https://github.com/louthy/language-ext)
// by Paul Louth. Copyright (c) 2014-2025 Paul Louth. MIT License — see LICENSE-LanguageExt.md.
// Simplified for Flowthru, mirroring the FlowIO fork:
//   - Failure type is RuntimeError (no generic E; no Error abstraction).
//   - No HKT (K<F, A>) / MonadIO — FlowSource is not polymorphic over a monad type-class.
//   - Not a transducer/DSL node hierarchy — FlowSource wraps a pull function directly.
//   - Consumption is compile-to-FlowIO; a bare IAsyncEnumerable is never exposed publicly.

using System.Runtime.CompilerServices;
using Flowthru.Validation.Runtime;

namespace Flowthru.Prelude;

/// <summary>
/// The Flowthru streaming effect type — the streaming sibling of
/// <see cref="FlowIO{A}"/>. A <c>FlowSource&lt;A&gt;</c> is a lazy,
/// resource-safe, error-as-value description of a stream of
/// <typeparamref name="A"/> values whose <em>only</em> consumption path is
/// <see cref="Compile"/>, which lands it back inside a <see cref="FlowIO{A}"/>.
/// </summary>
/// <remarks>
/// <para>
/// <strong>Why compile-to-<see cref="FlowIO{A}"/>.</strong> Enumeration runs
/// entirely inside the <see cref="FlowIO{A}"/> returned by a
/// <see cref="FlowSourceCompiler{A}"/> terminal, so the framework — not the
/// caller — owns the <c>try</c>/<c>finally</c>. Errors surface as values,
/// resources release deterministically, and cancellation threads through, all
/// by construction. A bare <see cref="IAsyncEnumerable{T}"/> is never handed
/// out (the pull function is <c>internal</c>).
/// </para>
/// <para>
/// <strong>Deferred acquisition.</strong> The underlying byte source is a
/// <see cref="FlowResource{TScope}"/> whose acquire runs on the first pull —
/// i.e. only when the compiled <see cref="FlowIO{A}"/> is run. A
/// <c>FlowSource</c> that is built but never compiled/run acquires nothing and
/// therefore leaks nothing; a compiled-but-never-run effect is likewise inert.
/// </para>
/// <para>
/// <strong>Error channel.</strong> By default a failure aborts the stream and
/// surfaces at compile as a terminal <see cref="RuntimeError"/>
/// (<c>EffResult.Failure</c>). For "keep the good rows, quarantine the bad",
/// use <see cref="Attempt"/> to move failures in-band as
/// <c>FlowSource&lt;EffResult&lt;A&gt;&gt;</c> elements, then
/// <c>SkipErrors</c>/<c>Rethrow</c> to collapse back.
/// </para>
/// <para>
/// <strong>Cancel-mid-acquire leak window.</strong> Release is guaranteed on
/// every path <em>once acquire has produced a scope</em>. If cancellation
/// lands after acquire begins but before it returns a scope, no scope exists
/// to release — the underlying resource's own disposal (e.g. a temp file's
/// finalizer) is the backstop, not a masking primitive <see cref="FlowIO{A}"/>
/// does not have.
/// </para>
/// </remarks>
/// <typeparam name="A">The element type produced by the stream.</typeparam>
public sealed class FlowSource<A>
{
  private readonly IFlowResource _resource;
  private readonly Func<object?, CancellationToken, IAsyncEnumerable<A>> _pull;
  private readonly Func<RuntimeError, RuntimeError> _mapError;

  internal FlowSource(
    IFlowResource resource,
    Func<object?, CancellationToken, IAsyncEnumerable<A>> pull,
    Func<RuntimeError, RuntimeError> mapError
  )
  {
    _resource = resource;
    _pull = pull;
    _mapError = mapError;
  }

  // Framework-internal views. These are how sibling combinators (error
  // bridges) rebuild a source without draining it — never public, so no bare
  // IAsyncEnumerable escapes the type.
  internal IFlowResource Resource => _resource;
  internal Func<object?, CancellationToken, IAsyncEnumerable<A>> Pull => _pull;
  internal Func<RuntimeError, RuntimeError> MapErr => _mapError;

  // ── Lazy combinators ──────────────────────────────────────────────────

  /// <summary>Transform each element. Lazy — no acquisition or pull happens.</summary>
  public FlowSource<B> Map<B>(Func<A, B> f) =>
    new(_resource, (scope, ct) => _pull(scope, ct).Select(f), _mapError);

  /// <summary>Keep only elements matching <paramref name="predicate"/>. Lazy.</summary>
  public FlowSource<A> Where(Func<A, bool> predicate) =>
    new(_resource, (scope, ct) => _pull(scope, ct).Where(predicate), _mapError);

  /// <summary>
  /// Transform the terminal failure. Composes with any existing mapping
  /// (existing applied first). Used by the storage layer to translate a
  /// provider exception into a typed <see cref="RuntimeError.SchemaMismatch"/>.
  /// </summary>
  public FlowSource<A> MapError(Func<RuntimeError, RuntimeError> f) =>
    new(_resource, _pull, e => f(_mapError(e)));

  /// <summary>
  /// Move failures in-band: the stream can no longer fail terminally; a
  /// failure becomes a trailing <c>EffResult.Failure</c> element after which
  /// the stream stops (fs2 <c>attempt</c> semantics).
  /// </summary>
  public FlowSource<EffResult<A>> Attempt() =>
    new(_resource, (scope, ct) => FlowSource.AttemptImpl(_pull(scope, ct), _mapError, ct), FlowSource.Id);

  /// <summary>The sole consumption path — lands the stream back in a <see cref="FlowIO{A}"/>.</summary>
  public FlowSourceCompiler<A> Compile() => new(this);
}

/// <summary>
/// Terminals that compile a <see cref="FlowSource{A}"/> into a
/// <see cref="FlowIO{A}"/>. Each drives the stream inside the effect envelope:
/// the bracketed source is acquired on the first pull and released on every
/// exit path (completion, failure, cancellation, early termination).
/// </summary>
/// <typeparam name="A">The element type of the compiled stream.</typeparam>
public readonly struct FlowSourceCompiler<A>
{
  private readonly FlowSource<A> _source;

  internal FlowSourceCompiler(FlowSource<A> source) => _source = source;

  /// <summary>
  /// Left-fold the stream into a single value inside the effect envelope.
  /// The bracketed source is acquired on first pull; a mid-stream throw
  /// becomes a typed <see cref="RuntimeError"/> failure.
  /// </summary>
  public FlowIO<S> Fold<S>(S seed, Func<S, A, S> step)
  {
    var source = _source;
    return FlowSource.BracketUse(
      source.Resource,
      scope =>
        FlowIO.LiftAsync(
          async ct =>
          {
            var acc = seed;
            await foreach (var a in source.Pull(scope, ct).WithCancellation(ct).ConfigureAwait(false))
            {
              acc = step(acc, a);
            }

            return acc;
          },
          source: "FlowSource"
        )
        .MapError(err => FlowSource.UnwrapFailure(err, source.MapErr))
    );
  }

  /// <summary>Run the stream for its effects, discarding elements.</summary>
  public FlowIO<FlowUnit> Drain() =>
    Fold(FlowUnit.Default, static (unit, _) => unit);

  /// <summary>Materialise the whole stream into a list. This is O(dataset) by intent — the visible way to collect.</summary>
  public FlowIO<IReadOnlyList<A>> ToList() =>
    Fold(new List<A>(), static (list, a) => { list.Add(a); return list; })
      .Map(static list => (IReadOnlyList<A>)list);

  /// <summary>
  /// Drive the stream into a batch sink inside the effect envelope: open, write
  /// each <see cref="IFlowSink{T}.BatchSize"/>-sized batch as elements arrive,
  /// then complete. The sink is disposed on every exit path (so it can roll
  /// back when completion is not reached), and the byte source is released
  /// after. Pull-based, so a slow sink paces a fast source in O(batch) memory.
  /// </summary>
  public FlowIO<FlowUnit> Into(IFlowSink<A> sink)
  {
    var source = _source;
    var batchSize = Math.Max(1, sink.BatchSize);
    return FlowSource.BracketUse(
      source.Resource,
      scope =>
        FlowIO.LiftAsync(
          async ct =>
          {
            var buffer = new List<A>(batchSize);
            try
            {
              await sink.OpenAsync(ct).ConfigureAwait(false);
              await foreach (var a in source.Pull(scope, ct).WithCancellation(ct).ConfigureAwait(false))
              {
                buffer.Add(a);
                if (buffer.Count >= batchSize)
                {
                  await sink.WriteBatchAsync(buffer, ct).ConfigureAwait(false);
                  buffer.Clear();
                }
              }

              if (buffer.Count > 0)
              {
                await sink.WriteBatchAsync(buffer, ct).ConfigureAwait(false);
              }

              await sink.CompleteAsync(ct).ConfigureAwait(false);
              return FlowUnit.Default;
            }
            finally
            {
              // The finally guarantees disposal on every path: success (cleanup
              // after commit) and failure/cancellation (abort/rollback, since
              // CompleteAsync was not reached).
              await sink.DisposeAsync().ConfigureAwait(false);
            }
          },
          source: "FlowSource.Into"
        )
        .MapError(err => FlowSource.UnwrapFailure(err, source.MapErr))
    );
  }
}

/// <summary>Factories, combinators, and internal machinery for <see cref="FlowSource{A}"/>.</summary>
public static class FlowSource
{
  /// <summary>The identity error map — the default for an un-translated source.</summary>
  internal static readonly Func<RuntimeError, RuntimeError> Id = static e => e;

  /// <summary>
  /// Build a source from a bare pull function with no managed resource. The
  /// pull is invoked lazily on compile-and-run.
  /// </summary>
  public static FlowSource<A> Lift<A>(Func<CancellationToken, IAsyncEnumerable<A>> pull) =>
    new(FlowResource.Empty, (_, ct) => pull(ct), Id);

  /// <summary>
  /// Build a source over a bracketed byte resource. The resource's acquire
  /// runs on the first pull and its release runs on every exit path.
  /// </summary>
  public static FlowSource<A> Bracket<TScope, A>(
    FlowResource<TScope> resource,
    Func<TScope, CancellationToken, IAsyncEnumerable<A>> pull
  ) =>
    new(resource, (scope, ct) => pull((TScope)scope!, ct), Id);

  /// <summary>The empty stream.</summary>
  public static FlowSource<A> Empty<A>() =>
    Lift<A>(static _ => AsyncEnumerable.Empty<A>());

  /// <summary>Build a source from an in-memory sequence (test/sample convenience).</summary>
  public static FlowSource<A> FromEnumerable<A>(IEnumerable<A> items) =>
    Lift<A>(ct => FromEnumerableImpl(items, ct));

  // ── Internal machinery ────────────────────────────────────────────────

  /// <summary>
  /// The pull-scoped bracket over the type-erased <see cref="IFlowResource"/>.
  /// Mirrors <see cref="FlowResource{TScope}.Use{TResult}"/> for the untyped
  /// view: acquire → body → release-on-every-path, body error wins.
  /// </summary>
  internal static FlowIO<S> BracketUse<S>(IFlowResource resource, Func<object?, FlowIO<S>> body) =>
    resource.AcquireUntyped().Bind(scope =>
      body(scope)
        .Catch(bodyError =>
          // Release must run even when cancellation is what aborted the body,
          // so suppress cancellation on the release effect.
          resource.ReleaseUntyped(scope, bodyError).Uncancellable()
            .Catch(static _ => FlowIO<FlowUnit>.Pure(FlowUnit.Default))
            .Bind<S>(_ => FlowIO<S>.Fail(bodyError)))
        .Bind(value =>
          resource.ReleaseUntyped(scope, null).Uncancellable().Map(_ => value)));

  /// <summary>
  /// Unwrap the driver's boundary error. <see cref="FlowIO{A}.LiftAsync"/>
  /// wraps every thrown exception as <see cref="RuntimeError.External"/>; a
  /// <see cref="FlowSourceFailure"/> carries an already-typed inner error to
  /// restore, and the source's own translator is applied last.
  /// </summary>
  internal static RuntimeError UnwrapFailure(RuntimeError err, Func<RuntimeError, RuntimeError> mapError) =>
    err switch
    {
      RuntimeError.External { Cause: FlowSourceFailure fsf } => mapError(fsf.Error),
      _ => mapError(err),
    };

  internal static async IAsyncEnumerable<EffResult<A>> AttemptImpl<A>(
    IAsyncEnumerable<A> source,
    Func<RuntimeError, RuntimeError> mapError,
    [EnumeratorCancellation] CancellationToken ct
  )
  {
    await using var e = source.GetAsyncEnumerator(ct);
    while (true)
    {
      // C# forbids `yield` inside a catch, so the try/catch only *captures* the
      // outcome; the yield happens afterwards.
      bool moved;
      A current = default!;
      EffResult<A>? failure = null;
      try
      {
        moved = await e.MoveNextAsync().ConfigureAwait(false);
        if (moved)
        {
          current = e.Current;
        }
      }
      // Cancellation is a control signal, not a dead-letter row — let it abort.
      catch (OperationCanceledException)
      {
        throw;
      }
      catch (FlowSourceFailure fsf)
      {
        moved = false;
        failure = new EffResult<A>.Failure(mapError(fsf.Error));
      }
      catch (Exception ex)
      {
        moved = false;
        failure = new EffResult<A>.Failure(mapError(new RuntimeError.External("FlowSource", ex)));
      }

      if (failure is not null)
      {
        yield return failure;
        yield break;
      }

      if (!moved)
      {
        yield break;
      }

      yield return new EffResult<A>.Success(current);
    }
  }

  private static async IAsyncEnumerable<A> FromEnumerableImpl<A>(
    IEnumerable<A> items,
    [EnumeratorCancellation] CancellationToken ct
  )
  {
    await Task.CompletedTask.ConfigureAwait(false); // async-iterator requirement; no real await
    foreach (var item in items)
    {
      ct.ThrowIfCancellationRequested();
      yield return item;
    }
  }
}

/// <summary>
/// Error-channel bridges between the terminal-failure and per-item-failure
/// representations. Extension methods (rather than instance methods) because
/// they are only valid where the element type is itself an
/// <see cref="EffResult{A}"/>.
/// </summary>
public static class FlowSourceErrorBridges
{
  /// <summary>
  /// Collapse an in-band stream back to a terminal-failure stream: a
  /// <c>Failure</c> element re-raises as the terminal error and stops the
  /// stream (TryStream <c>try_next</c> semantics).
  /// </summary>
  public static FlowSource<X> Rethrow<X>(this FlowSource<EffResult<X>> source) =>
    new(source.Resource, (scope, ct) => RethrowImpl(source.Pull(scope, ct), ct), source.MapErr);

  /// <summary>
  /// Drop <c>Failure</c> elements (optionally reporting each) and keep the
  /// successes — the dead-letter path.
  /// </summary>
  public static FlowSource<X> SkipErrors<X>(
    this FlowSource<EffResult<X>> source,
    Action<RuntimeError>? onError = null
  ) =>
    new(source.Resource, (scope, ct) => SkipErrorsImpl(source.Pull(scope, ct), onError, ct), source.MapErr);

  private static async IAsyncEnumerable<X> RethrowImpl<X>(
    IAsyncEnumerable<EffResult<X>> source,
    [EnumeratorCancellation] CancellationToken ct
  )
  {
    await foreach (var r in source.WithCancellation(ct).ConfigureAwait(false))
    {
      switch (r)
      {
        case EffResult<X>.Success s:
          yield return s.Value;
          break;
        case EffResult<X>.Failure f:
          throw new FlowSourceFailure(f.Error);
      }
    }
  }

  private static async IAsyncEnumerable<X> SkipErrorsImpl<X>(
    IAsyncEnumerable<EffResult<X>> source,
    Action<RuntimeError>? onError,
    [EnumeratorCancellation] CancellationToken ct
  )
  {
    await foreach (var r in source.WithCancellation(ct).ConfigureAwait(false))
    {
      switch (r)
      {
        case EffResult<X>.Success s:
          yield return s.Value;
          break;
        case EffResult<X>.Failure f:
          onError?.Invoke(f.Error);
          break;
      }
    }
  }
}

/// <summary>
/// Internal carrier that lets a producer fail the stream terminally with an
/// already-typed <see cref="RuntimeError"/>. The compile driver unwraps it
/// (see <see cref="FlowSource.UnwrapFailure"/>) so the typed error survives
/// the throw-across-<c>yield</c> boundary rather than flattening to
/// <see cref="RuntimeError.External"/>.
/// </summary>
internal sealed class FlowSourceFailure : Exception
{
  public RuntimeError Error { get; }

  public FlowSourceFailure(RuntimeError error)
    : base(error.Message) => Error = error;
}
