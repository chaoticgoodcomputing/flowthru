// Derived from LanguageExt v5 (https://github.com/louthy/language-ext) by Paul Louth.
// Copyright (c) 2014-2025 Paul Louth. MIT License — see LICENSE-LanguageExt.md.
// Simplified for Flowthru:
//   - Error accumulation is via IReadOnlyList<TError>; no Monoid<F> trait.
//   - No HKT (K<F, A>) — not polymorphic over an applicative type-class.
//   - No transformer (ValidationT) — Validated is concrete.
//   - Naming: 'Validated' (Flowthru house name) instead of LanguageExt's 'Validation'.

namespace Flowthru.Prelude;

/// <summary>
/// Error-accumulating applicative. Unlike <see cref="EffResult{T}"/> (and
/// monadic <c>Either</c>/<c>Result</c>), <see cref="Validated{TError, TValue}"/>
/// composes via <see cref="Zip{TOther}"/> in a way that preserves both sides'
/// errors when both fail. This is the right shape for pre-flight validation:
/// the user gets every check's outcome at once, not one error per re-run.
/// </summary>
/// <remarks>
/// <para>
/// LINQ syntax (<c>from … in … select …</c>) is monadic — it short-circuits on
/// first <see cref="Invalid"/>. Use <see cref="Zip{TOther}"/> or
/// <see cref="Validated.ZipAll{TError, TValue}"/> when you specifically want
/// accumulation.
/// </para>
/// </remarks>
/// <typeparam name="TError">The error type. Typically a closed sum.</typeparam>
/// <typeparam name="TValue">The success value type.</typeparam>
public abstract record Validated<TError, TValue>
{
  private Validated() { }

  public sealed record Valid(TValue Value) : Validated<TError, TValue>;

  public sealed record Invalid(IReadOnlyList<TError> Errors) : Validated<TError, TValue>;

  /// <summary>True if this is a <see cref="Valid"/>.</summary>
  public bool IsValid => this is Valid;

  /// <summary>True if this is an <see cref="Invalid"/>.</summary>
  public bool IsInvalid => this is Invalid;

  // ───────────────────────────────────────────────────────────────────────
  // Constructors
  // ───────────────────────────────────────────────────────────────────────

  /// <summary>Construct a successful Validated.</summary>
  public static Validated<TError, TValue> Pure(TValue value) => new Valid(value);

  /// <summary>Construct a failed Validated with a single error.</summary>
  public static Validated<TError, TValue> Fail(TError error) => new Invalid([error]);

  /// <summary>Construct a failed Validated with one or more errors.</summary>
  public static Validated<TError, TValue> Fail(IReadOnlyList<TError> errors) =>
    new Invalid(errors);

  // ───────────────────────────────────────────────────────────────────────
  // Combinators
  // ───────────────────────────────────────────────────────────────────────

  /// <summary>Transform the success value.</summary>
  public Validated<TError, TNext> Map<TNext>(Func<TValue, TNext> f) =>
    this switch
    {
      Valid v => new Validated<TError, TNext>.Valid(f(v.Value)),
      Invalid i => new Validated<TError, TNext>.Invalid(i.Errors),
      _ => throw new InvalidOperationException(
        "Unreachable: Validated is a closed sum"
      ),
    };

  /// <summary>
  /// Accumulating combinator. Zips two Validateds — if both are invalid,
  /// the resulting Invalid contains errors from both sides. This is the
  /// applicative-style composition that distinguishes Validated from a
  /// short-circuiting Result/Either.
  /// </summary>
  public Validated<TError, (TValue, TOther)> Zip<TOther>(Validated<TError, TOther> other) =>
    (this, other) switch
    {
      (Valid a, Validated<TError, TOther>.Valid b) =>
        new Validated<TError, (TValue, TOther)>.Valid((a.Value, b.Value)),
      (Invalid a, Validated<TError, TOther>.Invalid b) =>
        new Validated<TError, (TValue, TOther)>.Invalid([.. a.Errors, .. b.Errors]),
      (Invalid a, _) => new Validated<TError, (TValue, TOther)>.Invalid(a.Errors),
      (_, Validated<TError, TOther>.Invalid b) =>
        new Validated<TError, (TValue, TOther)>.Invalid(b.Errors),
      _ => throw new InvalidOperationException(
        "Unreachable: Validated is a closed sum"
      ),
    };

  /// <summary>
  /// Monadic bind — short-circuits on the first <see cref="Invalid"/>.
  /// Use this when later checks genuinely depend on earlier ones; use
  /// <see cref="Zip{TOther}"/> when checks are independent and you want
  /// every error at once.
  /// </summary>
  public Validated<TError, TNext> Bind<TNext>(Func<TValue, Validated<TError, TNext>> f) =>
    this switch
    {
      Valid v => f(v.Value),
      Invalid i => new Validated<TError, TNext>.Invalid(i.Errors),
      _ => throw new InvalidOperationException(
        "Unreachable: Validated is a closed sum"
      ),
    };

  /// <summary>
  /// Terminal pattern match. Use this to consume a Validated at the boundary
  /// where you must collapse the sum into a single result type.
  /// </summary>
  public TResult Match<TResult>(
    Func<TValue, TResult> onValid,
    Func<IReadOnlyList<TError>, TResult> onInvalid
  ) =>
    this switch
    {
      Valid v => onValid(v.Value),
      Invalid i => onInvalid(i.Errors),
      _ => throw new InvalidOperationException(
        "Unreachable: Validated is a closed sum"
      ),
    };

  // ───────────────────────────────────────────────────────────────────────
  // LINQ Syntax (monadic, short-circuit on Invalid)
  // ───────────────────────────────────────────────────────────────────────

  public Validated<TError, TNext> Select<TNext>(Func<TValue, TNext> f) => Map(f);

  public Validated<TError, TFinal> SelectMany<TNext, TFinal>(
    Func<TValue, Validated<TError, TNext>> bind,
    Func<TValue, TNext, TFinal> project
  ) => Bind(t => bind(t).Map(n => project(t, n)));
}

/// <summary>
/// Static helpers for <see cref="Validated{TError, TValue}"/> that take an
/// arbitrary number of inputs.
/// </summary>
public static class Validated
{
  /// <summary>
  /// Combine an enumerable of Validateds into a single Validated of a list.
  /// Errors from every <see cref="Validated{TError, TValue}.Invalid"/> input
  /// are accumulated; a single Invalid in the input does not short-circuit.
  /// </summary>
  public static Validated<TError, IReadOnlyList<TValue>> ZipAll<TError, TValue>(
    IEnumerable<Validated<TError, TValue>> validateds
  )
  {
    var values = new List<TValue>();
    var errors = new List<TError>();

    foreach (var v in validateds)
    {
      switch (v)
      {
        case Validated<TError, TValue>.Valid valid:
          values.Add(valid.Value);
          break;
        case Validated<TError, TValue>.Invalid invalid:
          errors.AddRange(invalid.Errors);
          break;
      }
    }

    return errors.Count > 0
      ? new Validated<TError, IReadOnlyList<TValue>>.Invalid(errors)
      : new Validated<TError, IReadOnlyList<TValue>>.Valid(values);
  }
}
