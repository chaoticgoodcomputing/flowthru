using System.Linq.Expressions;
using System.Reflection;
using Flowthru.Misc.DataFrames.Tests.Fixtures;

namespace Flowthru.Misc.DataFrames.Tests;

/// <summary>
/// Pins <see cref="GroupedFrame{TKey,TSource}"/> + the
/// <see cref="GroupedFrameExtensions.Aggregate"/> follow-up, plus
/// <see cref="AggregationContext{TKey,TSource}"/>'s throw-only marker
/// contract. The grouped frame can only be obtained via
/// <see cref="TypedFrameExtensions.GroupBy"/> (its constructor is internal),
/// and the only legal next step is <c>.Aggregate(...)</c>.
/// </summary>
[TestFixture]
public class GroupedFrameTests
{
  private static GroupedFrame<string, Person> NewGroupedFrame(out RecordingFrameQueryProvider provider)
  {
    provider = new RecordingFrameQueryProvider();
    return new TypedFrame<Person>(provider).GroupBy(p => p.Department);
  }

  [Test]
  public void GroupedFrame_ExposesExpression_FromGroupByCall()
  {
    var grouped = NewGroupedFrame(out _);
    Assert.That(grouped.Expression, Is.InstanceOf<MethodCallExpression>());
    Assert.That(((MethodCallExpression)grouped.Expression).Method.Name, Is.EqualTo("GroupBy"));
  }

  // ──────────────────────────────────────────────
  //  Aggregate — argument validation
  // ──────────────────────────────────────────────

  [Test]
  public void Aggregate_NullSource_Throws()
  {
    Expression<Func<AggregationContext<string, Person>, PersonSummary>> selector =
      ctx => new PersonSummary { Department = ctx.Key, Headcount = ctx.Count() };
    Assert.That(
      () => GroupedFrameExtensions.Aggregate<string, Person, PersonSummary>(
        source: null!,
        selector
      ),
      Throws.TypeOf<ArgumentNullException>()
    );
  }

  [Test]
  public void Aggregate_NullSelector_Throws()
  {
    var grouped = NewGroupedFrame(out _);
    Assert.That(
      () => grouped.Aggregate<string, Person, PersonSummary>(resultSelector: null!),
      Throws.TypeOf<ArgumentNullException>()
    );
  }

  // ──────────────────────────────────────────────
  //  Aggregate — happy path
  // ──────────────────────────────────────────────

  [Test]
  public void Aggregate_DispatchesAggregateMethodCall_ThroughProvider()
  {
    var grouped = NewGroupedFrame(out var provider);

    var aggregated = grouped.Aggregate(ctx => new PersonSummary
    {
      Department = ctx.Key,
      Headcount = ctx.Count(),
      AvgSalary = ctx.Avg(p => p.Salary),
    });

    Assert.That(aggregated, Is.TypeOf<TypedFrame<PersonSummary>>());
    Assert.That(provider.CreateQueryCalls, Has.Count.EqualTo(1));
    var mce = (MethodCallExpression)provider.CreateQueryCalls[0];
    Assert.That(mce.Method.Name, Is.EqualTo(nameof(GroupedFrameExtensions.Aggregate)));
    Assert.That(mce.Method.DeclaringType, Is.EqualTo(typeof(GroupedFrameExtensions)));
    Assert.That(mce.Arguments, Has.Count.EqualTo(2));
    Assert.That(mce.Arguments[0], Is.SameAs(grouped.Expression));
    Assert.That(mce.Arguments[1].NodeType, Is.EqualTo(ExpressionType.Quote));
  }

  // ──────────────────────────────────────────────
  //  AggregationContext — throw-only marker
  // ──────────────────────────────────────────────

  private static AggregationContext<string, Person> NewAggregationContext()
  {
    var ctor = typeof(AggregationContext<string, Person>).GetConstructor(
      BindingFlags.Instance | BindingFlags.NonPublic,
      Type.EmptyTypes
    );
    Assert.That(ctor, Is.Not.Null);
    return (AggregationContext<string, Person>)ctor!.Invoke(null);
  }

  [Test]
  public void AggregationContext_HasNoPublicConstructor()
  {
    var publicCtors = typeof(AggregationContext<string, Person>).GetConstructors(
      BindingFlags.Public | BindingFlags.Instance
    );
    Assert.That(publicCtors, Is.Empty);
  }

  [Test]
  public void AggregationContext_Key_Throws()
  {
    var ctx = NewAggregationContext();
    Assert.That(() => _ = ctx.Key, Throws.TypeOf<InvalidOperationException>());
  }

  [Test]
  public void AggregationContext_AvgDouble_Throws()
  {
    var ctx = NewAggregationContext();
    Expression<Func<Person, double>> col = p => (double)p.Age;
    Assert.That(() => ctx.Avg(col), Throws.TypeOf<InvalidOperationException>());
  }

  [Test]
  public void AggregationContext_AvgDecimal_Throws()
  {
    var ctx = NewAggregationContext();
    Expression<Func<Person, decimal>> col = p => p.Salary;
    Assert.That(() => ctx.Avg(col), Throws.TypeOf<InvalidOperationException>());
  }

  [Test]
  public void AggregationContext_AvgInt_Throws()
  {
    var ctx = NewAggregationContext();
    Expression<Func<Person, int>> col = p => p.Age;
    Assert.That(() => ctx.Avg(col), Throws.TypeOf<InvalidOperationException>());
  }

  [Test]
  public void AggregationContext_SumDouble_Throws()
  {
    var ctx = NewAggregationContext();
    Expression<Func<Person, double>> col = p => (double)p.Age;
    Assert.That(() => ctx.Sum(col), Throws.TypeOf<InvalidOperationException>());
  }

  [Test]
  public void AggregationContext_SumDecimal_Throws()
  {
    var ctx = NewAggregationContext();
    Expression<Func<Person, decimal>> col = p => p.Salary;
    Assert.That(() => ctx.Sum(col), Throws.TypeOf<InvalidOperationException>());
  }

  [Test]
  public void AggregationContext_SumInt_Throws()
  {
    var ctx = NewAggregationContext();
    Expression<Func<Person, int>> col = p => p.Age;
    Assert.That(() => ctx.Sum(col), Throws.TypeOf<InvalidOperationException>());
  }

  [Test]
  public void AggregationContext_Max_Throws()
  {
    var ctx = NewAggregationContext();
    Expression<Func<Person, int>> col = p => p.Age;
    Assert.That(() => ctx.Max(col), Throws.TypeOf<InvalidOperationException>());
  }

  [Test]
  public void AggregationContext_Min_Throws()
  {
    var ctx = NewAggregationContext();
    Expression<Func<Person, int>> col = p => p.Age;
    Assert.That(() => ctx.Min(col), Throws.TypeOf<InvalidOperationException>());
  }

  [Test]
  public void AggregationContext_CountNoArgs_Throws()
  {
    var ctx = NewAggregationContext();
    Assert.That(() => ctx.Count(), Throws.TypeOf<InvalidOperationException>());
  }
}
