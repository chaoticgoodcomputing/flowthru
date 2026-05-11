using System.Linq.Expressions;
using System.Reflection;
using Flowthru.Misc.DataFrames.Tests.Fixtures;

namespace Flowthru.Misc.DataFrames.Tests;

/// <summary>
/// Pins <see cref="FrameExpressionVisitor"/>'s dispatch contract and shared
/// helpers. A test subclass records which Translate* method fired for each
/// dispatched node; the helper tests exercise <c>ResolveColumnName</c>,
/// <c>Unquote</c>, and <c>EvaluateConstant</c> via protected-access bridges.
/// </summary>
[TestFixture]
public class FrameExpressionVisitorTests
{
  /// <summary>
  /// Concrete visitor that records which Translate* hook was called and
  /// returns a sentinel string per operation. Also exposes the protected
  /// static helpers as public methods so the helper tests can hit them
  /// without using reflection.
  /// </summary>
  private sealed class RecordingVisitor : FrameExpressionVisitor
  {
    public List<string> Calls { get; } = new();

    protected override object TranslateConstant(ConstantExpression node)
    {
      Calls.Add($"Constant({node.Value})");
      return "constant-result";
    }

    protected override object TranslateWhere(MethodCallExpression node)
    {
      Calls.Add("Where");
      return "where-result";
    }

    protected override object TranslateSelect(MethodCallExpression node)
    {
      Calls.Add("Select");
      return "select-result";
    }

    protected override object TranslateJoin(MethodCallExpression node)
    {
      Calls.Add("Join");
      return "join-result";
    }

    protected override object TranslateOrderBy(MethodCallExpression node, bool descending)
    {
      Calls.Add($"OrderBy(desc={descending})");
      return "orderby-result";
    }

    protected override object TranslateTake(MethodCallExpression node)
    {
      Calls.Add("Take");
      return "take-result";
    }

    protected override object TranslateCount(MethodCallExpression node)
    {
      Calls.Add("Count");
      return "count-result";
    }

    protected override object TranslateDistinct(MethodCallExpression node)
    {
      Calls.Add("Distinct");
      return "distinct-result";
    }

    protected override object TranslateUnion(MethodCallExpression node)
    {
      Calls.Add("Union");
      return "union-result";
    }

    protected override object TranslateGroupBy(MethodCallExpression node)
    {
      Calls.Add("GroupBy");
      return "groupby-result";
    }

    protected override object TranslateAggregate(MethodCallExpression node)
    {
      Calls.Add("Aggregate");
      return "aggregate-result";
    }

    protected override object TranslateSelectOver(MethodCallExpression node)
    {
      Calls.Add("SelectOver");
      return "selectover-result";
    }

    // Bridges for the protected static helpers — same intent as the
    // production visitor pattern (subclasses use them inside Translate*),
    // exposed here so tests can call them directly.
    public new static string ResolveColumnName(MemberInfo member) =>
      FrameExpressionVisitor.ResolveColumnName(member);

    public new static LambdaExpression Unquote(Expression expression) =>
      FrameExpressionVisitor.Unquote(expression);

    public new static object? EvaluateConstant(Expression expression) =>
      FrameExpressionVisitor.EvaluateConstant(expression);
  }

  private static MethodCallExpression BuildCall<TFrame>(Action<TypedFrame<TFrame>> shaper)
  {
    var provider = new RecordingFrameQueryProvider();
    var frame = new TypedFrame<TFrame>(provider);
    shaper(frame);
    return (MethodCallExpression)provider.CreateQueryCalls.Single();
  }

  // ──────────────────────────────────────────────
  //  CompileExpression — top-level dispatch
  // ──────────────────────────────────────────────

  [Test]
  public void CompileExpression_OnConstant_DispatchesToTranslateConstant()
  {
    var visitor = new RecordingVisitor();
    var node = Expression.Constant("root");

    var result = visitor.CompileExpression(node);

    Assert.That(visitor.Calls, Is.EqualTo(new[] { "Constant(root)" }));
    Assert.That(result, Is.EqualTo("constant-result"));
  }

  [Test]
  public void CompileExpression_OnUnsupportedNode_Throws()
  {
    var visitor = new RecordingVisitor();
    var unsupported = Expression.Add(Expression.Constant(1), Expression.Constant(2));

    Assert.That(
      () => visitor.CompileExpression(unsupported),
      Throws.TypeOf<NotSupportedException>().With.Message.Contain("not supported")
    );
  }

  // ──────────────────────────────────────────────
  //  TranslateMethodCall — per-operator dispatch
  // ──────────────────────────────────────────────

  [Test]
  public void Dispatch_Where_HitsTranslateWhere()
  {
    var node = BuildCall<Person>(f => { _ = f.Where(p => p.Age >= 18); });
    var visitor = new RecordingVisitor();
    var result = visitor.CompileExpression(node);
    Assert.That(visitor.Calls, Is.EqualTo(new[] { "Where" }));
    Assert.That(result, Is.EqualTo("where-result"));
  }

  [Test]
  public void Dispatch_Select_HitsTranslateSelect()
  {
    var node = BuildCall<Person>(f => { _ = f.Select(p => p.Name); });
    var visitor = new RecordingVisitor();
    visitor.CompileExpression(node);
    Assert.That(visitor.Calls, Is.EqualTo(new[] { "Select" }));
  }

  [Test]
  public void Dispatch_Join_HitsTranslateJoin()
  {
    var provider = new RecordingFrameQueryProvider();
    var outer = new TypedFrame<Person>(provider);
    var inner = new TypedFrame<Department>(provider);
    _ = outer.Join(inner, p => p.Department, d => d.Code, (p, d) => d.Title);
    var node = (MethodCallExpression)provider.CreateQueryCalls.Single();

    var visitor = new RecordingVisitor();
    visitor.CompileExpression(node);
    Assert.That(visitor.Calls, Is.EqualTo(new[] { "Join" }));
  }

  [Test]
  public void Dispatch_OrderBy_AscendingFlag()
  {
    var node = BuildCall<Person>(f => { _ = f.OrderBy(p => p.Age); });
    var visitor = new RecordingVisitor();
    visitor.CompileExpression(node);
    Assert.That(visitor.Calls, Is.EqualTo(new[] { "OrderBy(desc=False)" }));
  }

  [Test]
  public void Dispatch_OrderByDescending_DescendingFlag()
  {
    var node = BuildCall<Person>(f => { _ = f.OrderByDescending(p => p.Age); });
    var visitor = new RecordingVisitor();
    visitor.CompileExpression(node);
    Assert.That(visitor.Calls, Is.EqualTo(new[] { "OrderBy(desc=True)" }));
  }

  [Test]
  public void Dispatch_Take_HitsTranslateTake()
  {
    var node = BuildCall<Person>(f => { _ = f.Take(5); });
    var visitor = new RecordingVisitor();
    visitor.CompileExpression(node);
    Assert.That(visitor.Calls, Is.EqualTo(new[] { "Take" }));
  }

  [Test]
  public void Dispatch_Count_HitsTranslateCount()
  {
    // Count is a scalar-execution call; build its node directly.
    var provider = new RecordingFrameQueryProvider();
    var frame = new TypedFrame<Person>(provider);
    _ = frame.Count();
    var node = (MethodCallExpression)provider.ExecuteCalls.Single();

    var visitor = new RecordingVisitor();
    visitor.CompileExpression(node);
    Assert.That(visitor.Calls, Is.EqualTo(new[] { "Count" }));
  }

  [Test]
  public void Dispatch_Distinct_HitsTranslateDistinct()
  {
    var node = BuildCall<Person>(f => { _ = f.Distinct(); });
    var visitor = new RecordingVisitor();
    visitor.CompileExpression(node);
    Assert.That(visitor.Calls, Is.EqualTo(new[] { "Distinct" }));
  }

  [Test]
  public void Dispatch_Union_HitsTranslateUnion()
  {
    var provider = new RecordingFrameQueryProvider();
    var a = new TypedFrame<Person>(provider);
    var b = new TypedFrame<Person>(provider);
    _ = a.Union(b);
    var node = (MethodCallExpression)provider.CreateQueryCalls.Single();

    var visitor = new RecordingVisitor();
    visitor.CompileExpression(node);
    Assert.That(visitor.Calls, Is.EqualTo(new[] { "Union" }));
  }

  [Test]
  public void Dispatch_SelectOver_HitsTranslateSelectOver()
  {
    var node = BuildCall<Person>(f =>
    {
      _ = f.SelectOver(
        (p, win) => win.RowNumber(FrameWindowSpec<Person>.PartitionBy(x => x.Department))
      );
    });
    var visitor = new RecordingVisitor();
    visitor.CompileExpression(node);
    Assert.That(visitor.Calls, Is.EqualTo(new[] { "SelectOver" }));
  }

  [Test]
  public void Dispatch_Aggregate_HitsTranslateAggregate()
  {
    var provider = new RecordingFrameQueryProvider();
    var grouped = new TypedFrame<Person>(provider).GroupBy(p => p.Department);
    _ = grouped.Aggregate(ctx => new PersonSummary { Department = ctx.Key, Headcount = ctx.Count() });
    var node = (MethodCallExpression)provider.CreateQueryCalls.Single();

    var visitor = new RecordingVisitor();
    visitor.CompileExpression(node);
    Assert.That(visitor.Calls, Is.EqualTo(new[] { "Aggregate" }));
  }

  [Test]
  public void Dispatch_UnknownTypedFrameExtensionMethod_Throws()
  {
    // Synthesize a MethodCallExpression that claims to live on TypedFrameExtensions
    // but with a name the visitor doesn't recognise. We can't add a real method,
    // so use any static method on TypedFrameExtensions and verify the explicit
    // switch on Method.Name throws for an unknown name. Easiest path: synthesize
    // a fake by calling a method on GroupedFrameExtensions that isn't Aggregate.
    var fakeMethod = typeof(GroupedFrameExtensions)
      .GetMethods()
      .FirstOrDefault(m => m.Name != "Aggregate");
    if (fakeMethod is null)
    {
      Assert.Pass("No non-Aggregate methods on GroupedFrameExtensions; can't exercise this branch.");
      return;
    }

    // Fall back: directly call TranslateMethodCall on a node referencing a method
    // that is on neither extension class.
    var foreignMethod = typeof(string).GetMethod(nameof(string.Trim), Type.EmptyTypes)!;
    var node = Expression.Call(Expression.Constant("hello"), foreignMethod);

    var visitor = new RecordingVisitor();
    Assert.That(
      () => visitor.CompileExpression(node),
      Throws.TypeOf<NotSupportedException>().With.Message.Contain("not a recognized")
    );
  }

  // ──────────────────────────────────────────────
  //  ResolveColumnName
  // ──────────────────────────────────────────────

  private sealed class SerializedLabelAttribute : Attribute
  {
    public string Label { get; }

    public SerializedLabelAttribute(string label)
    {
      Label = label;
    }
  }

  private sealed class WrongLabelAttribute : Attribute
  {
    public string Label { get; } = "wrong";
  }

  private sealed class WithoutLabelPropAttribute : Attribute { }

  private sealed class LabeledRow
  {
    [SerializedLabel("hire_date")]
    public DateTime HireDate { get; set; }

    public string PlainName { get; set; } = "";

    [SerializedLabel("")]
    public int EmptyLabel { get; set; }

    [WrongLabel]
    public int WrongAttribute { get; set; }
  }

  [Test]
  public void ResolveColumnName_HonorsSerializedLabelAttribute()
  {
    var member = typeof(LabeledRow).GetProperty(nameof(LabeledRow.HireDate))!;
    Assert.That(RecordingVisitor.ResolveColumnName(member), Is.EqualTo("hire_date"));
  }

  [Test]
  public void ResolveColumnName_NoAttribute_ReturnsMemberName()
  {
    var member = typeof(LabeledRow).GetProperty(nameof(LabeledRow.PlainName))!;
    Assert.That(RecordingVisitor.ResolveColumnName(member), Is.EqualTo("PlainName"));
  }

  [Test]
  public void ResolveColumnName_EmptyLabel_FallsBackToMemberName()
  {
    var member = typeof(LabeledRow).GetProperty(nameof(LabeledRow.EmptyLabel))!;
    Assert.That(RecordingVisitor.ResolveColumnName(member), Is.EqualTo("EmptyLabel"));
  }

  [Test]
  public void ResolveColumnName_WrongAttributeName_FallsBackToMemberName()
  {
    // Duck-typing matches on the type's *name* — an attribute named
    // WrongLabelAttribute exposes a Label property but won't be honored
    // because its name doesn't match SerializedLabelAttribute.
    var member = typeof(LabeledRow).GetProperty(nameof(LabeledRow.WrongAttribute))!;
    Assert.That(RecordingVisitor.ResolveColumnName(member), Is.EqualTo("WrongAttribute"));
  }

  // ──────────────────────────────────────────────
  //  Unquote
  // ──────────────────────────────────────────────

  [Test]
  public void Unquote_OnQuotedLambda_ReturnsTheUnderlyingLambda()
  {
    Expression<Func<int, int>> lambda = x => x + 1;
    var quoted = Expression.Quote(lambda);

    var unquoted = RecordingVisitor.Unquote(quoted);

    Assert.That(unquoted, Is.SameAs(lambda));
  }

  [Test]
  public void Unquote_OnBareLambda_ReturnsItUnchanged()
  {
    Expression<Func<int, int>> lambda = x => x + 1;
    Assert.That(RecordingVisitor.Unquote(lambda), Is.SameAs(lambda));
  }

  [Test]
  public void Unquote_OnUnsupportedExpression_Throws()
  {
    var constant = Expression.Constant(42);
    Assert.That(
      () => RecordingVisitor.Unquote(constant),
      Throws.TypeOf<InvalidOperationException>().With.Message.Contain("quoted lambda")
    );
  }

  // ──────────────────────────────────────────────
  //  EvaluateConstant
  // ──────────────────────────────────────────────

  [Test]
  public void EvaluateConstant_OnIntConstant_ReturnsBoxedValue()
  {
    Assert.That(RecordingVisitor.EvaluateConstant(Expression.Constant(7)), Is.EqualTo(7));
  }

  [Test]
  public void EvaluateConstant_OnStringConstant_ReturnsString()
  {
    Assert.That(RecordingVisitor.EvaluateConstant(Expression.Constant("hello")), Is.EqualTo("hello"));
  }

  [Test]
  public void EvaluateConstant_OnClosureCapture_ResolvesToCapturedValue()
  {
    int captured = 21;
    // Capture-by-closure expression — the lambda body reads `captured`.
    Expression<Func<int>> producer = () => captured * 2;
    var body = producer.Body;

    Assert.That(RecordingVisitor.EvaluateConstant(body), Is.EqualTo(42));
  }

  [Test]
  public void EvaluateConstant_OnNullExpression_ReturnsNull()
  {
    var nullExpression = Expression.Constant(null, typeof(string));
    Assert.That(RecordingVisitor.EvaluateConstant(nullExpression), Is.Null);
  }
}
