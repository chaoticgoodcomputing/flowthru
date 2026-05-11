using System.Linq.Expressions;
using Flowthru.Misc.DataFrames.Tests.Fixtures;

namespace Flowthru.Misc.DataFrames.Tests;

/// <summary>
/// Pins the contract every <see cref="TypedFrameExtensions"/> method advertises:
///
/// 1. Null arguments throw <see cref="ArgumentNullException"/> at the call site.
/// 2. No native execution happens — the call dispatches a
///    <see cref="MethodCallExpression"/> through
///    <see cref="IFrameQueryProvider.CreateQuery{TElement}"/> with the
///    matching <c>MethodInfo</c> and arguments.
/// 3. The resulting frame's expression carries the full operation chain
///    (so subsequent operations compose without losing history).
///
/// Each test arranges a <see cref="RecordingFrameQueryProvider"/>, invokes a
/// single operator on a root frame, and asserts on the recorded expression.
/// </summary>
[TestFixture]
public class TypedFrameExtensionsTests
{
  private static TypedFrame<Person> NewRootFrame(out RecordingFrameQueryProvider provider)
  {
    provider = new RecordingFrameQueryProvider();
    return new TypedFrame<Person>(provider);
  }

  // ──────────────────────────────────────────────
  //  Where
  // ──────────────────────────────────────────────

  [Test]
  public void Where_NullSource_Throws()
  {
    Expression<Func<Person, bool>> predicate = p => p.Age > 18;
    Assert.That(
      () => TypedFrameExtensions.Where<Person>(source: null!, predicate),
      Throws.TypeOf<ArgumentNullException>()
    );
  }

  [Test]
  public void Where_NullPredicate_Throws()
  {
    var frame = NewRootFrame(out _);
    Assert.That(
      () => frame.Where(predicate: null!),
      Throws.TypeOf<ArgumentNullException>()
    );
  }

  [Test]
  public void Where_BuildsMethodCallNodeNamedWhere()
  {
    var frame = NewRootFrame(out var provider);

    var filtered = frame.Where(p => p.Age >= 18);

    Assert.That(provider.CreateQueryCalls, Has.Count.EqualTo(1));
    var mce = (MethodCallExpression)provider.CreateQueryCalls[0];
    Assert.That(mce.Method.Name, Is.EqualTo(nameof(TypedFrameExtensions.Where)));
    Assert.That(mce.Method.DeclaringType, Is.EqualTo(typeof(TypedFrameExtensions)));
    Assert.That(mce.Arguments, Has.Count.EqualTo(2));
    Assert.That(mce.Arguments[0], Is.SameAs(frame.Expression));
    Assert.That(mce.Arguments[1].NodeType, Is.EqualTo(ExpressionType.Quote));
    Assert.That(filtered, Is.TypeOf<TypedFrame<Person>>());
  }

  [Test]
  public void Where_PreservesElementType()
  {
    var frame = NewRootFrame(out _);
    var filtered = frame.Where(p => p.Age >= 18);
    Assert.That(filtered.ElementType, Is.EqualTo(typeof(Person)));
  }

  // ──────────────────────────────────────────────
  //  Select
  // ──────────────────────────────────────────────

  [Test]
  public void Select_NullSource_Throws()
  {
    Expression<Func<Person, string>> selector = p => p.Name;
    Assert.That(
      () => TypedFrameExtensions.Select<Person, string>(source: null!, selector),
      Throws.TypeOf<ArgumentNullException>()
    );
  }

  [Test]
  public void Select_NullSelector_Throws()
  {
    var frame = NewRootFrame(out _);
    Assert.That(
      () => frame.Select<Person, string>(selector: null!),
      Throws.TypeOf<ArgumentNullException>()
    );
  }

  [Test]
  public void Select_ProjectsToNewElementType()
  {
    var frame = NewRootFrame(out var provider);

    var names = frame.Select(p => p.Name);

    Assert.That(names.ElementType, Is.EqualTo(typeof(string)));
    var mce = (MethodCallExpression)provider.CreateQueryCalls.Single();
    Assert.That(mce.Method.Name, Is.EqualTo(nameof(TypedFrameExtensions.Select)));
    Assert.That(mce.Method.GetGenericArguments(), Is.EqualTo(new[] { typeof(Person), typeof(string) }));
  }

  // ──────────────────────────────────────────────
  //  Join
  // ──────────────────────────────────────────────

  [Test]
  public void Join_NullOuter_Throws()
  {
    var provider = new RecordingFrameQueryProvider();
    var inner = new TypedFrame<Department>(provider);
    Assert.That(
      () =>
        TypedFrameExtensions.Join<Person, Department, string, string>(
          outer: null!,
          inner,
          p => p.Department,
          d => d.Code,
          (p, d) => d.Title
        ),
      Throws.TypeOf<ArgumentNullException>()
    );
  }

  [Test]
  public void Join_NullInner_Throws()
  {
    var outer = NewRootFrame(out _);
    Assert.That(
      () =>
        outer.Join<Person, Department, string, string>(
          inner: null!,
          p => p.Department,
          d => d.Code,
          (p, d) => d.Title
        ),
      Throws.TypeOf<ArgumentNullException>()
    );
  }

  [Test]
  public void Join_AllSelectorsRequired()
  {
    var outer = NewRootFrame(out var provider);
    var inner = new TypedFrame<Department>(provider);

    Assert.That(
      () => outer.Join<Person, Department, string, string>(inner, null!, d => d.Code, (p, d) => d.Title),
      Throws.TypeOf<ArgumentNullException>()
    );
    Assert.That(
      () => outer.Join<Person, Department, string, string>(inner, p => p.Department, null!, (p, d) => d.Title),
      Throws.TypeOf<ArgumentNullException>()
    );
    Assert.That(
      () => outer.Join<Person, Department, string, string>(inner, p => p.Department, d => d.Code, null!),
      Throws.TypeOf<ArgumentNullException>()
    );
  }

  [Test]
  public void Join_DispatchesFiveArgumentMethodCall()
  {
    var outer = NewRootFrame(out var provider);
    var inner = new TypedFrame<Department>(provider);

    var joined = outer.Join(inner, p => p.Department, d => d.Code, (p, d) => d.Title);

    var mce = (MethodCallExpression)provider.CreateQueryCalls.Single();
    Assert.That(mce.Method.Name, Is.EqualTo(nameof(TypedFrameExtensions.Join)));
    Assert.That(mce.Arguments, Has.Count.EqualTo(5));
    Assert.That(mce.Arguments[0], Is.SameAs(outer.Expression));
    Assert.That(mce.Arguments[1], Is.SameAs(inner.Expression));
    for (var i = 2; i < 5; i++)
    {
      Assert.That(mce.Arguments[i].NodeType, Is.EqualTo(ExpressionType.Quote));
    }
    Assert.That(joined.ElementType, Is.EqualTo(typeof(string)));
  }

  // ──────────────────────────────────────────────
  //  OrderBy / OrderByDescending
  // ──────────────────────────────────────────────

  [Test]
  public void OrderBy_NullSource_Throws()
  {
    Expression<Func<Person, int>> key = p => p.Age;
    Assert.That(
      () => TypedFrameExtensions.OrderBy<Person, int>(source: null!, key),
      Throws.TypeOf<ArgumentNullException>()
    );
  }

  [Test]
  public void OrderBy_NullKeySelector_Throws()
  {
    var frame = NewRootFrame(out _);
    Assert.That(
      () => frame.OrderBy<Person, int>(keySelector: null!),
      Throws.TypeOf<ArgumentNullException>()
    );
  }

  [Test]
  public void OrderBy_EmitsOrderByMethodCall()
  {
    var frame = NewRootFrame(out var provider);
    var sorted = frame.OrderBy(p => p.Age);

    var mce = (MethodCallExpression)provider.CreateQueryCalls.Single();
    Assert.That(mce.Method.Name, Is.EqualTo(nameof(TypedFrameExtensions.OrderBy)));
    Assert.That(sorted.ElementType, Is.EqualTo(typeof(Person)));
  }

  [Test]
  public void OrderByDescending_NullSource_Throws()
  {
    Expression<Func<Person, int>> key = p => p.Age;
    Assert.That(
      () => TypedFrameExtensions.OrderByDescending<Person, int>(source: null!, key),
      Throws.TypeOf<ArgumentNullException>()
    );
  }

  [Test]
  public void OrderByDescending_NullKeySelector_Throws()
  {
    var frame = NewRootFrame(out _);
    Assert.That(
      () => frame.OrderByDescending<Person, int>(keySelector: null!),
      Throws.TypeOf<ArgumentNullException>()
    );
  }

  [Test]
  public void OrderByDescending_EmitsOrderByDescendingMethodCall()
  {
    var frame = NewRootFrame(out var provider);
    var sorted = frame.OrderByDescending(p => p.Age);

    var mce = (MethodCallExpression)provider.CreateQueryCalls.Single();
    Assert.That(mce.Method.Name, Is.EqualTo(nameof(TypedFrameExtensions.OrderByDescending)));
  }

  // ──────────────────────────────────────────────
  //  Take
  // ──────────────────────────────────────────────

  [Test]
  public void Take_NullSource_Throws()
  {
    Assert.That(
      () => TypedFrameExtensions.Take<Person>(source: null!, 10),
      Throws.TypeOf<ArgumentNullException>()
    );
  }

  [Test]
  public void Take_CarriesCountConstant()
  {
    var frame = NewRootFrame(out var provider);
    var limited = frame.Take(5);

    var mce = (MethodCallExpression)provider.CreateQueryCalls.Single();
    Assert.That(mce.Method.Name, Is.EqualTo(nameof(TypedFrameExtensions.Take)));
    Assert.That(mce.Arguments, Has.Count.EqualTo(2));
    Assert.That(mce.Arguments[1], Is.TypeOf<ConstantExpression>());
    Assert.That(((ConstantExpression)mce.Arguments[1]).Value, Is.EqualTo(5));
    Assert.That(limited.ElementType, Is.EqualTo(typeof(Person)));
  }

  [Test]
  public void Take_ZeroCount_StillBuildsNode()
  {
    var frame = NewRootFrame(out var provider);
    _ = frame.Take(0);

    var mce = (MethodCallExpression)provider.CreateQueryCalls.Single();
    Assert.That(((ConstantExpression)mce.Arguments[1]).Value, Is.EqualTo(0));
  }

  // ──────────────────────────────────────────────
  //  Count
  // ──────────────────────────────────────────────

  [Test]
  public void Count_NullSource_Throws()
  {
    Assert.That(
      () => TypedFrameExtensions.Count<Person>(source: null!),
      Throws.TypeOf<ArgumentNullException>()
    );
  }

  [Test]
  public void Count_DispatchesThroughExecute_ReturnsProviderScalar()
  {
    var frame = NewRootFrame(out var provider);
    provider.ExecuteScalarResult = 42L;

    var result = frame.Count();

    Assert.That(result, Is.EqualTo(42L));
    Assert.That(provider.ExecuteCalls, Has.Count.EqualTo(1));
    var mce = (MethodCallExpression)provider.ExecuteCalls[0];
    Assert.That(mce.Method.Name, Is.EqualTo(nameof(TypedFrameExtensions.Count)));
    Assert.That(mce.Arguments, Has.Count.EqualTo(1));
    Assert.That(mce.Arguments[0], Is.SameAs(frame.Expression));
  }

  [Test]
  public void Count_DoesNotInvokeCreateQuery()
  {
    var frame = NewRootFrame(out var provider);
    _ = frame.Count();
    Assert.That(provider.CreateQueryCalls, Is.Empty);
  }

  // ──────────────────────────────────────────────
  //  Distinct
  // ──────────────────────────────────────────────

  [Test]
  public void Distinct_NullSource_Throws()
  {
    Assert.That(
      () => TypedFrameExtensions.Distinct<Person>(source: null!),
      Throws.TypeOf<ArgumentNullException>()
    );
  }

  [Test]
  public void Distinct_EmitsSingleArgumentMethodCall()
  {
    var frame = NewRootFrame(out var provider);
    var distinct = frame.Distinct();

    var mce = (MethodCallExpression)provider.CreateQueryCalls.Single();
    Assert.That(mce.Method.Name, Is.EqualTo(nameof(TypedFrameExtensions.Distinct)));
    Assert.That(mce.Arguments, Has.Count.EqualTo(1));
    Assert.That(distinct.ElementType, Is.EqualTo(typeof(Person)));
  }

  // ──────────────────────────────────────────────
  //  Union
  // ──────────────────────────────────────────────

  [Test]
  public void Union_NullSource_Throws()
  {
    var provider = new RecordingFrameQueryProvider();
    var other = new TypedFrame<Person>(provider);
    Assert.That(
      () => TypedFrameExtensions.Union<Person>(source: null!, other),
      Throws.TypeOf<ArgumentNullException>()
    );
  }

  [Test]
  public void Union_NullOther_Throws()
  {
    var frame = NewRootFrame(out _);
    Assert.That(
      () => frame.Union(other: null!),
      Throws.TypeOf<ArgumentNullException>()
    );
  }

  [Test]
  public void Union_EmitsTwoSourceMethodCall()
  {
    var frame = NewRootFrame(out var provider);
    var other = new TypedFrame<Person>(provider);

    var combined = frame.Union(other);

    var mce = (MethodCallExpression)provider.CreateQueryCalls.Single();
    Assert.That(mce.Method.Name, Is.EqualTo(nameof(TypedFrameExtensions.Union)));
    Assert.That(mce.Arguments, Has.Count.EqualTo(2));
    Assert.That(mce.Arguments[0], Is.SameAs(frame.Expression));
    Assert.That(mce.Arguments[1], Is.SameAs(other.Expression));
    Assert.That(combined.ElementType, Is.EqualTo(typeof(Person)));
  }

  // ──────────────────────────────────────────────
  //  GroupBy
  // ──────────────────────────────────────────────

  [Test]
  public void GroupBy_NullSource_Throws()
  {
    Expression<Func<Person, string>> key = p => p.Department;
    Assert.That(
      () => TypedFrameExtensions.GroupBy<Person, string>(source: null!, key),
      Throws.TypeOf<ArgumentNullException>()
    );
  }

  [Test]
  public void GroupBy_NullKeySelector_Throws()
  {
    var frame = NewRootFrame(out _);
    Assert.That(
      () => frame.GroupBy<Person, string>(keySelector: null!),
      Throws.TypeOf<ArgumentNullException>()
    );
  }

  [Test]
  public void GroupBy_ReturnsGroupedFrame_NotTypedFrame()
  {
    // GroupBy is the only operator that breaks out of TypedFrame<T> — the
    // grouped result is a typed anchor that ONLY supports .Aggregate().
    var frame = NewRootFrame(out var provider);

    var grouped = frame.GroupBy(p => p.Department);

    Assert.That(grouped, Is.TypeOf<GroupedFrame<string, Person>>());
    // No CreateQuery call — the GroupedFrame is constructed directly so its
    // expression can be passed to GroupedFrameExtensions.Aggregate later.
    Assert.That(provider.CreateQueryCalls, Is.Empty);
    Assert.That(grouped.Expression, Is.InstanceOf<MethodCallExpression>());
    Assert.That(((MethodCallExpression)grouped.Expression).Method.Name, Is.EqualTo("GroupBy"));
  }

  // ──────────────────────────────────────────────
  //  SelectOver
  // ──────────────────────────────────────────────

  [Test]
  public void SelectOver_NullSource_Throws()
  {
    Expression<Func<Person, WindowContext<Person>, long>> selector =
      (p, win) => win.Rank(FrameWindowSpec<Person>.PartitionBy(x => x.Department).OrderBy(x => x.Salary));
    Assert.That(
      () => TypedFrameExtensions.SelectOver<Person, long>(source: null!, selector),
      Throws.TypeOf<ArgumentNullException>()
    );
  }

  [Test]
  public void SelectOver_NullSelector_Throws()
  {
    var frame = NewRootFrame(out _);
    Assert.That(
      () => frame.SelectOver<Person, long>(selector: null!),
      Throws.TypeOf<ArgumentNullException>()
    );
  }

  [Test]
  public void SelectOver_EmitsSelectOverNode_PreservingSelector()
  {
    var frame = NewRootFrame(out var provider);

    var ranked = frame.SelectOver(
      (p, win) => win.RowNumber(FrameWindowSpec<Person>.PartitionBy(x => x.Department))
    );

    var mce = (MethodCallExpression)provider.CreateQueryCalls.Single();
    Assert.That(mce.Method.Name, Is.EqualTo(nameof(TypedFrameExtensions.SelectOver)));
    Assert.That(mce.Arguments[1].NodeType, Is.EqualTo(ExpressionType.Quote));
    Assert.That(ranked.ElementType, Is.EqualTo(typeof(long)));
  }

  // ──────────────────────────────────────────────
  //  Composition
  // ──────────────────────────────────────────────

  [Test]
  public void OperatorChain_StacksExpressionsThroughTheProvider()
  {
    // Where → OrderBy → Take. Each step should call CreateQuery with the
    // previous step's expression as the head argument — proving frames stay
    // immutable and compose left-to-right.
    var frame = NewRootFrame(out var provider);

    _ = frame.Where(p => p.Age > 18).OrderBy(p => p.Age).Take(10);

    Assert.That(provider.CreateQueryCalls, Has.Count.EqualTo(3));
    var whereCall = (MethodCallExpression)provider.CreateQueryCalls[0];
    var orderByCall = (MethodCallExpression)provider.CreateQueryCalls[1];
    var takeCall = (MethodCallExpression)provider.CreateQueryCalls[2];

    Assert.That(whereCall.Method.Name, Is.EqualTo("Where"));
    Assert.That(orderByCall.Method.Name, Is.EqualTo("OrderBy"));
    Assert.That(takeCall.Method.Name, Is.EqualTo("Take"));

    Assert.That(orderByCall.Arguments[0], Is.SameAs(whereCall));
    Assert.That(takeCall.Arguments[0], Is.SameAs(orderByCall));
  }
}
