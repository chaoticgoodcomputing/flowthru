using System.Linq.Expressions;
using Flowthru.Misc.DataFrames.Tests.Fixtures;

namespace Flowthru.Misc.DataFrames.Tests;

/// <summary>
/// Pins <see cref="FrameWindowSpec{TSource}"/>'s data-carrier contract:
/// immutability (each fluent call returns a new spec), null-key guards,
/// and the empty-state semantics of <see cref="FrameWindowSpec{TSource}.Global"/>.
/// </summary>
[TestFixture]
public class FrameWindowSpecTests
{
  [Test]
  public void Global_HasNoPartitionsOrOrders()
  {
    var spec = FrameWindowSpec<Person>.Global;
    Assert.That(spec.PartitionByExpressions, Is.Empty);
    Assert.That(spec.OrderByExpressions, Is.Empty);
  }

  [Test]
  public void Global_IsSharedSingleton()
  {
    // The Global spec is documented as a starting point. Re-reading it must
    // yield the same instance — otherwise the cheap-shared-empty contract
    // silently turns into per-call allocation. Read into local refs so the
    // NUnit analyzer doesn't flag a same-expression-on-both-sides check.
    var first = FrameWindowSpec<Person>.Global;
    var second = FrameWindowSpec<Person>.Global;
    Assert.That(first, Is.SameAs(second));
  }

  [Test]
  public void PartitionBy_NullKey_Throws()
  {
    Assert.That(
      () => FrameWindowSpec<Person>.PartitionBy<string>(null!),
      Throws.TypeOf<ArgumentNullException>()
    );
  }

  [Test]
  public void PartitionBy_AddsSingleKey()
  {
    Expression<Func<Person, string>> key = p => p.Department;
    var spec = FrameWindowSpec<Person>.PartitionBy(key);

    Assert.That(spec.PartitionByExpressions, Has.Count.EqualTo(1));
    Assert.That(spec.PartitionByExpressions[0], Is.SameAs(key));
    Assert.That(spec.OrderByExpressions, Is.Empty);
  }

  [Test]
  public void ThenPartitionBy_NullKey_Throws()
  {
    var spec = FrameWindowSpec<Person>.PartitionBy(p => p.Department);
    Assert.That(
      () => spec.ThenPartitionBy<int>(null!),
      Throws.TypeOf<ArgumentNullException>()
    );
  }

  [Test]
  public void ThenPartitionBy_AppendsKeyAndReturnsNewSpec()
  {
    var first = FrameWindowSpec<Person>.PartitionBy(p => p.Department);
    var second = first.ThenPartitionBy(p => p.Age);

    Assert.That(second, Is.Not.SameAs(first));
    Assert.That(second.PartitionByExpressions, Has.Count.EqualTo(2));
    Assert.That(first.PartitionByExpressions, Has.Count.EqualTo(1));
  }

  [Test]
  public void OrderBy_NullKey_Throws()
  {
    Assert.That(
      () => FrameWindowSpec<Person>.Global.OrderBy<int>(null!),
      Throws.TypeOf<ArgumentNullException>()
    );
  }

  [Test]
  public void OrderBy_AppendsAscendingPair()
  {
    var spec = FrameWindowSpec<Person>.Global.OrderBy(p => p.Salary);
    Assert.That(spec.OrderByExpressions, Has.Count.EqualTo(1));
    Assert.That(spec.OrderByExpressions[0].Descending, Is.False);
  }

  [Test]
  public void OrderByDescending_NullKey_Throws()
  {
    Assert.That(
      () => FrameWindowSpec<Person>.Global.OrderByDescending<int>(null!),
      Throws.TypeOf<ArgumentNullException>()
    );
  }

  [Test]
  public void OrderByDescending_AppendsDescendingPair()
  {
    var spec = FrameWindowSpec<Person>.Global.OrderByDescending(p => p.Salary);
    Assert.That(spec.OrderByExpressions, Has.Count.EqualTo(1));
    Assert.That(spec.OrderByExpressions[0].Descending, Is.True);
  }

  [Test]
  public void FluentChain_AccumulatesAllKeys_InCallOrder()
  {
    var spec = FrameWindowSpec<Person>
      .PartitionBy(p => p.Department)
      .ThenPartitionBy(p => p.Age)
      .OrderBy(p => p.Salary)
      .OrderByDescending(p => p.Name);

    Assert.That(spec.PartitionByExpressions, Has.Count.EqualTo(2));
    Assert.That(spec.OrderByExpressions, Has.Count.EqualTo(2));
    Assert.That(spec.OrderByExpressions[0].Descending, Is.False);
    Assert.That(spec.OrderByExpressions[1].Descending, Is.True);
  }

  [Test]
  public void Builders_DoNotMutateSource()
  {
    // Any fluent call must produce a new spec — the old one stays usable.
    var original = FrameWindowSpec<Person>.PartitionBy(p => p.Department);
    _ = original.OrderBy(p => p.Salary);
    _ = original.ThenPartitionBy(p => p.Age);

    Assert.That(original.PartitionByExpressions, Has.Count.EqualTo(1));
    Assert.That(original.OrderByExpressions, Is.Empty);
  }

  [Test]
  public void NonGenericInterface_ExposesSamePartitionAndOrderState()
  {
    IFrameWindowSpec spec = FrameWindowSpec<Person>
      .PartitionBy(p => p.Department)
      .OrderBy(p => p.Salary);

    Assert.That(spec.PartitionByExpressions, Has.Count.EqualTo(1));
    Assert.That(spec.OrderByExpressions, Has.Count.EqualTo(1));
  }
}
