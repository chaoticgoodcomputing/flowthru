using Flowthru.Prelude;

namespace Flowthru.Tests.Kits.Prelude;

/// <summary>
/// The laws of <see cref="Validated{TError, TValue}"/>. Subclasses bind
/// concrete representative types via the abstract members and inherit a
/// fixed set of law tests covering applicative-zip accumulation, monadic-bind
/// short-circuit, functor identity, and <see cref="Validated.ZipAll"/>
/// behaviour.
/// </summary>
/// <typeparam name="TError">The error type the Validateds carry.</typeparam>
/// <typeparam name="TValue">The success type the Validateds carry.</typeparam>
public abstract class ValidatedLaws<TError, TValue>
{
  /// <summary>Sample success values to use when constructing valid Validateds.</summary>
  protected abstract IEnumerable<TValue> SampleValues { get; }

  /// <summary>Sample error values to use when constructing invalid Validateds.</summary>
  protected abstract IEnumerable<TError> SampleErrors { get; }

  /// <summary>
  /// Equality predicate over <typeparamref name="TValue"/>. Defaults to
  /// <see cref="EqualityComparer{TValue}.Default"/>.
  /// </summary>
  protected virtual bool ValuesEqual(TValue a, TValue b) =>
    EqualityComparer<TValue>.Default.Equals(a, b);

  // ── Functor laws ───────────────────────────────────────────────────────

  /// <summary>Functor identity: <c>v.Map(x => x) == v</c>.</summary>
  [Test]
  public void FunctorIdentityLaw_OnValid()
  {
    foreach (var a in SampleValues)
    {
      var v = Validated<TError, TValue>.Pure(a);
      var lhs = v.Map(x => x);
      Assert.That(lhs, Is.InstanceOf<Validated<TError, TValue>.Valid>());
      Assert.That(ValuesEqual(((Validated<TError, TValue>.Valid)lhs).Value, a), Is.True);
    }
  }

  /// <summary>Map on Invalid is a no-op (errors pass through unchanged).</summary>
  [Test]
  public void MapPreservesErrorsOnInvalid()
  {
    var error = SampleErrors.First();
    var v = Validated<TError, TValue>.Fail(error);
    var mapped = v.Map(x => x);
    Assert.That(mapped, Is.InstanceOf<Validated<TError, TValue>.Invalid>());
    var inv = (Validated<TError, TValue>.Invalid)mapped;
    Assert.That(inv.Errors, Has.Count.EqualTo(1));
    Assert.That(inv.Errors[0], Is.EqualTo(error));
  }

  // ── Applicative-zip accumulation ───────────────────────────────────────

  /// <summary>
  /// Zip of two Valids produces a Valid with the tuple of values.
  /// </summary>
  [Test]
  public void ZipOfTwoValidsProducesValid()
  {
    var values = SampleValues.ToList();
    if (values.Count < 2)
    {
      Assert.Pass("Need at least two sample values for this law; skipping.");
      return;
    }
    var a = Validated<TError, TValue>.Pure(values[0]);
    var b = Validated<TError, TValue>.Pure(values[1]);
    var zipped = a.Zip(b);
    Assert.That(zipped, Is.InstanceOf<Validated<TError, (TValue, TValue)>.Valid>());
    var v = (Validated<TError, (TValue, TValue)>.Valid)zipped;
    Assert.That(ValuesEqual(v.Value.Item1, values[0]), Is.True);
    Assert.That(ValuesEqual(v.Value.Item2, values[1]), Is.True);
  }

  /// <summary>
  /// Zip of two Invalids accumulates errors from both sides.
  /// </summary>
  [Test]
  public void ZipOfTwoInvalidsAccumulatesErrors()
  {
    var errors = SampleErrors.ToList();
    if (errors.Count < 2)
    {
      Assert.Pass("Need at least two sample errors for this law; skipping.");
      return;
    }
    var a = Validated<TError, TValue>.Fail(errors[0]);
    var b = Validated<TError, TValue>.Fail(errors[1]);
    var zipped = a.Zip(b);
    Assert.That(zipped, Is.InstanceOf<Validated<TError, (TValue, TValue)>.Invalid>());
    var inv = (Validated<TError, (TValue, TValue)>.Invalid)zipped;
    Assert.That(inv.Errors, Has.Count.EqualTo(2));
    Assert.That(inv.Errors, Does.Contain(errors[0]));
    Assert.That(inv.Errors, Does.Contain(errors[1]));
  }

  /// <summary>
  /// Zip of Valid + Invalid surfaces the Invalid's errors only.
  /// </summary>
  [Test]
  public void ZipOfValidAndInvalidPropagatesInvalidErrors()
  {
    var error = SampleErrors.First();
    var value = SampleValues.First();
    var a = Validated<TError, TValue>.Pure(value);
    var b = Validated<TError, TValue>.Fail(error);
    var zipped = a.Zip(b);
    Assert.That(zipped, Is.InstanceOf<Validated<TError, (TValue, TValue)>.Invalid>());
    var inv = (Validated<TError, (TValue, TValue)>.Invalid)zipped;
    Assert.That(inv.Errors, Has.Count.EqualTo(1));
    Assert.That(inv.Errors[0], Is.EqualTo(error));
  }

  // ── Monadic bind short-circuit ─────────────────────────────────────────

  /// <summary>
  /// Bind on Valid runs the continuation; bind on Invalid does not.
  /// </summary>
  [Test]
  public void BindRunsContinuationOnValidOnly()
  {
    var value = SampleValues.First();
    var error = SampleErrors.First();

    // Valid path: continuation runs.
    var validCalled = false;
    var validBound = Validated<TError, TValue>.Pure(value).Bind(_ =>
    {
      validCalled = true;
      return Validated<TError, TValue>.Pure(value);
    });
    Assert.That(validCalled, Is.True, "Bind should call continuation on Valid");
    Assert.That(validBound, Is.InstanceOf<Validated<TError, TValue>.Valid>());

    // Invalid path: continuation does not run.
    var invalidCalled = false;
    var invalidBound = Validated<TError, TValue>.Fail(error).Bind(_ =>
    {
      invalidCalled = true;
      return Validated<TError, TValue>.Pure(value);
    });
    Assert.That(invalidCalled, Is.False, "Bind should short-circuit on Invalid");
    Assert.That(invalidBound, Is.InstanceOf<Validated<TError, TValue>.Invalid>());
  }

  // ── ZipAll n-ary applicative ───────────────────────────────────────────

  /// <summary>
  /// <see cref="Validated.ZipAll"/> over an all-Valid sequence returns a
  /// Valid containing every value, in order.
  /// </summary>
  [Test]
  public void ZipAllOverValidsCollectsValues()
  {
    var values = SampleValues.Take(3).ToList();
    if (values.Count == 0)
    {
      Assert.Pass("Need at least one sample value for this law; skipping.");
      return;
    }
    var validateds = values.Select(v => Validated<TError, TValue>.Pure(v));
    var result = Validated.ZipAll(validateds);
    Assert.That(result, Is.InstanceOf<Validated<TError, IReadOnlyList<TValue>>.Valid>());
    var valid = (Validated<TError, IReadOnlyList<TValue>>.Valid)result;
    Assert.That(valid.Value, Has.Count.EqualTo(values.Count));
    for (int i = 0; i < values.Count; i++)
    {
      Assert.That(ValuesEqual(valid.Value[i], values[i]), Is.True);
    }
  }

  /// <summary>
  /// <see cref="Validated.ZipAll"/> over a mixed sequence accumulates every
  /// error from the Invalid entries; values from Valid entries are dropped.
  /// </summary>
  [Test]
  public void ZipAllOverMixedAccumulatesErrors()
  {
    var values = SampleValues.Take(2).ToList();
    var errors = SampleErrors.Take(2).ToList();
    if (values.Count < 1 || errors.Count < 2)
    {
      Assert.Pass("Need >=1 value and >=2 errors for this law; skipping.");
      return;
    }
    var validateds = new[]
    {
      Validated<TError, TValue>.Pure(values[0]),
      Validated<TError, TValue>.Fail(errors[0]),
      Validated<TError, TValue>.Fail(errors[1]),
    };
    var result = Validated.ZipAll(validateds);
    Assert.That(result, Is.InstanceOf<Validated<TError, IReadOnlyList<TValue>>.Invalid>());
    var inv = (Validated<TError, IReadOnlyList<TValue>>.Invalid)result;
    Assert.That(inv.Errors, Has.Count.EqualTo(2));
    Assert.That(inv.Errors, Does.Contain(errors[0]));
    Assert.That(inv.Errors, Does.Contain(errors[1]));
  }
}
