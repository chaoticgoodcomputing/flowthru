using Flowthru.Prelude;

namespace Flowthru.Core.Tests.Prelude;

/// <summary>
/// Pins the unit-type laws on <see cref="FlowUnit"/>: every value is equal,
/// the comparator collapses to zero, and the four ordering operators are
/// constant in the expected directions. The struct exists to flow through
/// generic combinators (<see cref="FlowIO{A}"/> with <c>A = FlowUnit</c>)
/// where the bound value carries no information; the operator behaviour
/// matters for downstream LINQ / dictionary keys / equality checks.
/// </summary>
[TestFixture]
public class FlowUnitTests
{
  [Test]
  public void Default_IsEqualToConstructed()
  {
    var a = FlowUnit.Default;
    var b = new FlowUnit();
    Assert.That(a, Is.EqualTo(b));
  }

  [Test]
  public void Equals_TypedOverload_AlwaysTrue()
  {
    Assert.That(FlowUnit.Default.Equals(FlowUnit.Default), Is.True);
  }

  [Test]
  public void Equals_ObjectOverload_TrueForFlowUnit()
  {
    object boxed = FlowUnit.Default;
    Assert.That(FlowUnit.Default.Equals(boxed), Is.True);
  }

  [Test]
  public void Equals_ObjectOverload_FalseForOtherTypes()
  {
    Assert.That(FlowUnit.Default.Equals("not a flow unit"), Is.False);
    Assert.That(FlowUnit.Default.Equals(null), Is.False);
  }

  [Test]
  public void GetHashCode_IsZero()
  {
    Assert.That(FlowUnit.Default.GetHashCode(), Is.EqualTo(0));
  }

  [Test]
  public void ToString_ReturnsUnitLiteral()
  {
    Assert.That(FlowUnit.Default.ToString(), Is.EqualTo("()"));
  }

  [Test]
  public void CompareTo_AlwaysZero()
  {
    Assert.That(FlowUnit.Default.CompareTo(FlowUnit.Default), Is.EqualTo(0));
  }

  // The reflexivity tests below intentionally compare FlowUnit.Default
  // against itself — the unit type has exactly one inhabitant and its
  // operator laws are precisely "every comparison is the constant
  // implied by the operator". CS1718 ("comparison made to same
  // variable") fires on the self-comparison; suppressed here because
  // the self-comparison IS the test.
#pragma warning disable CS1718

  [Test]
  public void EqualityOperator_AlwaysTrue()
  {
    Assert.That(FlowUnit.Default == FlowUnit.Default, Is.True);
  }

  [Test]
  public void InequalityOperator_AlwaysFalse()
  {
    Assert.That(FlowUnit.Default != FlowUnit.Default, Is.False);
  }

  // The four ordering operators encode the unit-type law that there is no
  // strict ordering: < and > are always false, <= and >= always true.
  [Test]
  public void LessThanOperator_AlwaysFalse()
  {
    Assert.That(FlowUnit.Default < FlowUnit.Default, Is.False);
  }

  [Test]
  public void LessThanOrEqualOperator_AlwaysTrue()
  {
    Assert.That(FlowUnit.Default <= FlowUnit.Default, Is.True);
  }

  [Test]
  public void GreaterThanOperator_AlwaysFalse()
  {
    Assert.That(FlowUnit.Default > FlowUnit.Default, Is.False);
  }

  [Test]
  public void GreaterThanOrEqualOperator_AlwaysTrue()
  {
    Assert.That(FlowUnit.Default >= FlowUnit.Default, Is.True);
  }

#pragma warning restore CS1718

  [Test]
  public void ImplicitConversion_ToValueTuple_RoundTrips()
  {
    ValueTuple v = FlowUnit.Default;
    FlowUnit back = v;
    Assert.That(back, Is.EqualTo(FlowUnit.Default));
  }
}
