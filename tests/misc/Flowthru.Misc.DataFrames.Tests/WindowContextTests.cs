using System.Linq.Expressions;
using System.Reflection;
using Flowthru.Misc.DataFrames.Tests.Fixtures;

namespace Flowthru.Misc.DataFrames.Tests;

/// <summary>
/// Pins <see cref="WindowContext{TSource}"/>'s "throw-only marker" contract.
/// Every public method must throw <see cref="InvalidOperationException"/>
/// when invoked at runtime — the methods exist only to satisfy the C#
/// compiler inside <c>SelectOver</c> selector expressions. Providers walk
/// the expression tree and translate the calls to native window functions;
/// any actual invocation is a programmer error worth surfacing loudly.
/// </summary>
[TestFixture]
public class WindowContextTests
{
  /// <summary>
  /// Reflectively instantiate the private-constructor type so we can call
  /// instance methods on it. This mirrors what an expression-tree walker
  /// would have to do; production code never builds a real instance.
  /// </summary>
  private static WindowContext<Person> NewContext()
  {
    var ctor = typeof(WindowContext<Person>).GetConstructor(
      BindingFlags.Instance | BindingFlags.NonPublic,
      Type.EmptyTypes
    );
    Assert.That(ctor, Is.Not.Null, "Expected a non-public parameterless constructor.");
    return (WindowContext<Person>)ctor!.Invoke(null);
  }

  [Test]
  public void Constructor_IsNotPubliclyAccessible()
  {
    // Confirms the marker-type design: callers cannot accidentally
    // instantiate a WindowContext and rely on its methods at runtime.
    var publicCtors = typeof(WindowContext<Person>).GetConstructors(BindingFlags.Public | BindingFlags.Instance);
    Assert.That(publicCtors, Is.Empty);
  }

  private static readonly FrameWindowSpec<Person> AnySpec =
    FrameWindowSpec<Person>.PartitionBy(p => p.Department);

  [Test]
  public void RowNumber_Throws()
  {
    var ctx = NewContext();
    Assert.That(() => ctx.RowNumber(AnySpec), Throws.TypeOf<InvalidOperationException>());
  }

  [Test]
  public void Rank_Throws()
  {
    var ctx = NewContext();
    Assert.That(() => ctx.Rank(AnySpec), Throws.TypeOf<InvalidOperationException>());
  }

  [Test]
  public void DenseRank_Throws()
  {
    var ctx = NewContext();
    Assert.That(() => ctx.DenseRank(AnySpec), Throws.TypeOf<InvalidOperationException>());
  }

  [Test]
  public void CumeDist_Throws()
  {
    var ctx = NewContext();
    Assert.That(() => ctx.CumeDist(AnySpec), Throws.TypeOf<InvalidOperationException>());
  }

  [Test]
  public void PercentRank_Throws()
  {
    var ctx = NewContext();
    Assert.That(() => ctx.PercentRank(AnySpec), Throws.TypeOf<InvalidOperationException>());
  }

  [Test]
  public void Count_Throws()
  {
    var ctx = NewContext();
    Assert.That(() => ctx.Count(AnySpec), Throws.TypeOf<InvalidOperationException>());
  }

  [Test]
  public void Lag_Throws()
  {
    var ctx = NewContext();
    Expression<Func<Person, int>> col = p => p.Age;
    Assert.That(() => ctx.Lag(col, 1, AnySpec), Throws.TypeOf<InvalidOperationException>());
  }

  [Test]
  public void Lead_Throws()
  {
    var ctx = NewContext();
    Expression<Func<Person, int>> col = p => p.Age;
    Assert.That(() => ctx.Lead(col, 1, AnySpec), Throws.TypeOf<InvalidOperationException>());
  }

  [Test]
  public void Sum_DoubleOverload_Throws()
  {
    var ctx = NewContext();
    Expression<Func<Person, double>> col = p => (double)p.Age;
    Assert.That(() => ctx.Sum(col, AnySpec), Throws.TypeOf<InvalidOperationException>());
  }

  [Test]
  public void Sum_DecimalOverload_Throws()
  {
    var ctx = NewContext();
    Expression<Func<Person, decimal>> col = p => p.Salary;
    Assert.That(() => ctx.Sum(col, AnySpec), Throws.TypeOf<InvalidOperationException>());
  }

  [Test]
  public void Sum_IntOverload_Throws()
  {
    var ctx = NewContext();
    Expression<Func<Person, int>> col = p => p.Age;
    Assert.That(() => ctx.Sum(col, AnySpec), Throws.TypeOf<InvalidOperationException>());
  }

  [Test]
  public void Avg_DoubleOverload_Throws()
  {
    var ctx = NewContext();
    Expression<Func<Person, double>> col = p => (double)p.Age;
    Assert.That(() => ctx.Avg(col, AnySpec), Throws.TypeOf<InvalidOperationException>());
  }

  [Test]
  public void Avg_IntOverload_Throws()
  {
    var ctx = NewContext();
    Expression<Func<Person, int>> col = p => p.Age;
    Assert.That(() => ctx.Avg(col, AnySpec), Throws.TypeOf<InvalidOperationException>());
  }

  [Test]
  public void Max_Throws()
  {
    var ctx = NewContext();
    Expression<Func<Person, int>> col = p => p.Age;
    Assert.That(() => ctx.Max(col, AnySpec), Throws.TypeOf<InvalidOperationException>());
  }

  [Test]
  public void Min_Throws()
  {
    var ctx = NewContext();
    Expression<Func<Person, int>> col = p => p.Age;
    Assert.That(() => ctx.Min(col, AnySpec), Throws.TypeOf<InvalidOperationException>());
  }
}
