// Derived from LanguageExt v5 (https://github.com/louthy/language-ext) by Paul Louth.
// Copyright (c) 2014-2025 Paul Louth. MIT License — see LICENSE-LanguageExt.md.
// Simplified for Flowthru:
//   - Failure type is RuntimeError (no generic E parameter; no Error abstraction).
//   - No HKT (K<F, A>) — Eff is not polymorphic over a monad type-class.
//   - No ReaderT/IO transformer stack — Eff wraps a Func directly.
//   - Always async — the single Run method returns Task<EffResult<T>>.

namespace Flowthru.Prelude;

/// <summary>
/// Result of running an <see cref="Eff{TRuntime, T}"/>. Closed sum: either the
/// effect produced a value (<see cref="Success"/>) or it failed with a
/// <see cref="RuntimeError"/> (<see cref="Failure"/>).
/// </summary>
public abstract record EffResult<T>
{
  private EffResult() { }

  public sealed record Success(T Value) : EffResult<T>;

  public sealed record Failure(RuntimeError Error) : EffResult<T>;

  /// <summary>True if this is a <see cref="Success"/>.</summary>
  public bool IsSuccess => this is Success;

  /// <summary>True if this is a <see cref="Failure"/>.</summary>
  public bool IsFailure => this is Failure;

  /// <summary>
  /// Terminal pattern match. Use this to consume an EffResult at the host
  /// boundary where you must collapse the sum into a single result type.
  /// </summary>
  public TResult Match<TResult>(
    Func<T, TResult> onSuccess,
    Func<RuntimeError, TResult> onFailure
  ) =>
    this switch
    {
      Success s => onSuccess(s.Value),
      Failure f => onFailure(f.Error),
      _ => throw new InvalidOperationException("Unreachable: EffResult is a closed sum"),
    };
}

/// <summary>
/// Capability-environment-typed effect. An <c>Eff&lt;TRuntime, T&gt;</c> is a
/// description of a computation that, given a <typeparamref name="TRuntime"/>
/// providing the capabilities the computation requires, yields either a
/// value of type <typeparamref name="T"/> or a <see cref="RuntimeError"/>.
/// </summary>
/// <remarks>
/// <para>
/// Eff is the substrate of every Flowthru runtime path. It is lazy — no side
/// effect runs until <see cref="Run"/> is called. Errors are values, not
/// exceptions: <see cref="Lift"/> and <see cref="LiftAsync"/> capture thrown
/// exceptions and route them through <see cref="RuntimeError"/>.
/// </para>
/// <para>
/// Capability requirements are expressed as constraints on
/// <typeparamref name="TRuntime"/>:
/// <c>where TRuntime : Has&lt;TRuntime, IStepRunner&gt;</c>. The C# generic
/// constraint solver enforces host satisfaction at compile time — there is
/// no separate analyzer or registry check.
/// </para>
/// </remarks>
public sealed class Eff<TRuntime, T>
{
  private readonly Func<TRuntime, CancellationToken, Task<EffResult<T>>> _run;

  private Eff(Func<TRuntime, CancellationToken, Task<EffResult<T>>> run)
  {
    _run = run;
  }

  // ───────────────────────────────────────────────────────────────────────
  // Run
  // ───────────────────────────────────────────────────────────────────────

  /// <summary>
  /// Run the effect against a runtime. This is the only operation that
  /// performs side effects — every combinator above this point is pure.
  /// </summary>
  public Task<EffResult<T>> Run(
    TRuntime runtime,
    CancellationToken cancellationToken = default
  ) => _run(runtime, cancellationToken);

  // ───────────────────────────────────────────────────────────────────────
  // Constructors
  // ───────────────────────────────────────────────────────────────────────

  /// <summary>Lift a value into an effect that always succeeds.</summary>
  public static Eff<TRuntime, T> Pure(T value) =>
    new((_, _) => Task.FromResult<EffResult<T>>(new EffResult<T>.Success(value)));

  /// <summary>Lift an error into an effect that always fails.</summary>
  public static Eff<TRuntime, T> Fail(RuntimeError error) =>
    new((_, _) => Task.FromResult<EffResult<T>>(new EffResult<T>.Failure(error)));

  /// <summary>
  /// Lift a synchronous side-effecting function. Exceptions are captured and
  /// routed through <see cref="RuntimeError.External"/>;
  /// <see cref="OperationCanceledException"/> becomes
  /// <see cref="RuntimeError.Cancelled"/>.
  /// </summary>
  /// <param name="f">The side-effecting function.</param>
  /// <param name="source">
  /// Diagnostic label included in <see cref="RuntimeError.External.Source"/>
  /// when the function throws. Defaults to <c>"Eff.Lift"</c>.
  /// </param>
  public static Eff<TRuntime, T> Lift(Func<TRuntime, T> f, string source = "Eff.Lift") =>
    new(
      (rt, ct) =>
      {
        if (ct.IsCancellationRequested)
        {
          return Task.FromResult<EffResult<T>>(
            new EffResult<T>.Failure(new RuntimeError.Cancelled("Cancellation requested"))
          );
        }

        try
        {
          return Task.FromResult<EffResult<T>>(new EffResult<T>.Success(f(rt)));
        }
        catch (OperationCanceledException)
        {
          return Task.FromResult<EffResult<T>>(
            new EffResult<T>.Failure(new RuntimeError.Cancelled("Operation cancelled"))
          );
        }
        catch (Exception ex)
        {
          return Task.FromResult<EffResult<T>>(
            new EffResult<T>.Failure(new RuntimeError.External(source, ex))
          );
        }
      }
    );

  /// <summary>
  /// Lift an asynchronous side-effecting function. Exceptions are captured
  /// and routed through <see cref="RuntimeError.External"/>;
  /// <see cref="OperationCanceledException"/> becomes
  /// <see cref="RuntimeError.Cancelled"/>.
  /// </summary>
  public static Eff<TRuntime, T> LiftAsync(
    Func<TRuntime, CancellationToken, Task<T>> f,
    string source = "Eff.LiftAsync"
  ) =>
    new(
      async (rt, ct) =>
      {
        try
        {
          var value = await f(rt, ct).ConfigureAwait(false);
          return new EffResult<T>.Success(value);
        }
        catch (OperationCanceledException)
        {
          return new EffResult<T>.Failure(new RuntimeError.Cancelled("Operation cancelled"));
        }
        catch (Exception ex)
        {
          return new EffResult<T>.Failure(new RuntimeError.External(source, ex));
        }
      }
    );

  // ───────────────────────────────────────────────────────────────────────
  // Combinators
  // ───────────────────────────────────────────────────────────────────────

  /// <summary>Transform the success value.</summary>
  public Eff<TRuntime, TNext> Map<TNext>(Func<T, TNext> f) =>
    new(async (rt, ct) =>
    {
      var result = await _run(rt, ct).ConfigureAwait(false);
      return result switch
      {
        EffResult<T>.Success s => new EffResult<TNext>.Success(f(s.Value)),
        EffResult<T>.Failure x => new EffResult<TNext>.Failure(x.Error),
        _ => throw new InvalidOperationException("Unreachable: EffResult is a closed sum"),
      };
    });

  /// <summary>Sequence another effect that depends on the success value.</summary>
  public Eff<TRuntime, TNext> Bind<TNext>(Func<T, Eff<TRuntime, TNext>> f) =>
    new(async (rt, ct) =>
    {
      var result = await _run(rt, ct).ConfigureAwait(false);
      return result switch
      {
        EffResult<T>.Success s => await f(s.Value).Run(rt, ct).ConfigureAwait(false),
        EffResult<T>.Failure x => new EffResult<TNext>.Failure(x.Error),
        _ => throw new InvalidOperationException("Unreachable: EffResult is a closed sum"),
      };
    });

  /// <summary>Transform the failure value while leaving successes untouched.</summary>
  public Eff<TRuntime, T> MapError(Func<RuntimeError, RuntimeError> f) =>
    new(async (rt, ct) =>
    {
      var result = await _run(rt, ct).ConfigureAwait(false);
      return result switch
      {
        EffResult<T>.Success s => s,
        EffResult<T>.Failure x => new EffResult<T>.Failure(f(x.Error)),
        _ => throw new InvalidOperationException("Unreachable: EffResult is a closed sum"),
      };
    });

  /// <summary>
  /// Recover from a failure by running a handler effect. Successes pass through.
  /// </summary>
  public Eff<TRuntime, T> Catch(Func<RuntimeError, Eff<TRuntime, T>> handler) =>
    new(async (rt, ct) =>
    {
      var result = await _run(rt, ct).ConfigureAwait(false);
      return result switch
      {
        EffResult<T>.Success s => s,
        EffResult<T>.Failure x => await handler(x.Error).Run(rt, ct).ConfigureAwait(false),
        _ => throw new InvalidOperationException("Unreachable: EffResult is a closed sum"),
      };
    });

  // ───────────────────────────────────────────────────────────────────────
  // LINQ Syntax (monadic, short-circuit on failure)
  // ───────────────────────────────────────────────────────────────────────

  public Eff<TRuntime, TNext> Select<TNext>(Func<T, TNext> f) => Map(f);

  public Eff<TRuntime, TFinal> SelectMany<TNext, TFinal>(
    Func<T, Eff<TRuntime, TNext>> bind,
    Func<T, TNext, TFinal> project
  ) => Bind(t => bind(t).Map(n => project(t, n)));
}
