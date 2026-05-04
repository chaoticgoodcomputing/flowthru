using Flowthru.Core.Effects;

namespace Flowthru.Core.Tests.Validation;

/// <summary>
/// Tests for the applicative <see cref="FlowValidation"/> type. Verifies that
/// failures accumulate across <see cref="FlowValidation.Combine(FlowValidation[])"/>
/// rather than short-circuiting, and that the type's surface is deliberately
/// applicative — no <c>Bind</c>.
/// </summary>
[TestFixture]
[Category("Validation")]
public class FlowValidationTests
{
  [Test]
  public void Pass_HasNoFailures()
  {
    var v = FlowValidation.Pass;

    Assert.That(v.IsValid, Is.True);
    Assert.That(v.Failures, Is.Empty);
  }

  [Test]
  public void Fail_BuildsSingleFailure()
  {
    var v = FlowValidation.Fail("source", "boom");

    Assert.That(v.IsValid, Is.False);
    Assert.That(v.Failures, Has.Count.EqualTo(1));
    Assert.That(v.Failures[0].Source, Is.EqualTo("source"));
    Assert.That(v.Failures[0].Message, Is.EqualTo("boom"));
    Assert.That(v.Failures[0].Exception, Is.Null);
  }

  [Test]
  public void Fail_CarriesException()
  {
    var ex = new InvalidOperationException("inner");
    var v = FlowValidation.Fail("source", "boom", ex);

    Assert.That(v.Failures[0].Exception, Is.SameAs(ex));
  }

  [Test]
  public void Combine_OfAllPassing_IsPass()
  {
    var v = FlowValidation.Combine(
      FlowValidation.Pass,
      FlowValidation.Pass,
      FlowValidation.Pass
    );

    Assert.That(v.IsValid, Is.True);
  }

  [Test]
  public void Combine_AccumulatesFailuresInOrder()
  {
    var v = FlowValidation.Combine(
      FlowValidation.Fail("a", "first"),
      FlowValidation.Pass,
      FlowValidation.Fail("b", "second"),
      FlowValidation.Fail("c", "third")
    );

    Assert.That(v.IsValid, Is.False);
    Assert.That(v.Failures.Select(f => f.Message), Is.EqualTo(new[] { "first", "second", "third" }));
    Assert.That(v.Failures.Select(f => f.Source), Is.EqualTo(new[] { "a", "b", "c" }));
  }

  [Test]
  public void Combine_OfEmptyEnumerable_IsPass()
  {
    var v = FlowValidation.Combine(Enumerable.Empty<FlowValidation>());

    Assert.That(v.IsValid, Is.True);
  }

  [Test]
  public void Map_TransformsEachFailure()
  {
    var v = FlowValidation.Combine(
      FlowValidation.Fail("a", "x"),
      FlowValidation.Fail("b", "y")
    );

    var mapped = v.Map(f => f with { Source = $"catalog/{f.Source}" });

    Assert.That(mapped.Failures.Select(f => f.Source), Is.EqualTo(new[] { "catalog/a", "catalog/b" }));
  }

  [Test]
  public void Map_OfPass_StaysPass()
  {
    var mapped = FlowValidation.Pass.Map(f => f with { Message = "changed" });

    Assert.That(mapped.IsValid, Is.True);
    Assert.That(mapped.Failures, Is.Empty);
  }

  [Test]
  public void DefaultStruct_BehavesAsPass()
  {
    // A `default(FlowValidation)` value should not throw and should report
    // as passing. Defends against accidental misuse where someone constructs
    // a FlowValidation without the static factory.
    var v = default(FlowValidation);

    Assert.That(v.IsValid, Is.True);
    Assert.That(v.Failures, Is.Empty);
  }
}
