using System.Linq.Expressions;
using Flowthru.Misc.DataFrames;

namespace Flowthru.Extensions.Spark.Tests;

[TestFixture]
[Category("ExpressionTree")]
public class GroupByAggregateExpressionTests
{
  private TestFrameProvider _provider = null!;

  [SetUp]
  public void SetUp()
  {
    _provider = new TestFrameProvider();
  }

  // ===================================================================
  //  GroupBy tree structure
  // ===================================================================

  [Test]
  public void GroupBy_ProducesMethodCallExpression_WithCorrectMethodName()
  {
    var frame = new TypedFrame<SalesSchema>(_provider);

    var result = frame.GroupBy(x => x.Category);

    var mce = result.Expression as MethodCallExpression;
    Assert.That(mce, Is.Not.Null);
    Assert.That(mce!.Method.Name, Is.EqualTo("GroupBy"));
  }

  [Test]
  public void GroupBy_HasTwoArguments_SourceAndKeySelector()
  {
    var frame = new TypedFrame<SalesSchema>(_provider);

    var result = frame.GroupBy(x => x.Category);

    var mce = (MethodCallExpression)result.Expression;
    Assert.That(mce.Arguments, Has.Count.EqualTo(2));
  }

  [Test]
  public void GroupBy_KeySelector_IsQuotedLambda()
  {
    var frame = new TypedFrame<SalesSchema>(_provider);

    var result = frame.GroupBy(x => x.Category);

    var mce = (MethodCallExpression)result.Expression;
    var quoted = mce.Arguments[1] as UnaryExpression;
    Assert.That(quoted, Is.Not.Null);
    Assert.That(quoted!.NodeType, Is.EqualTo(ExpressionType.Quote));
    Assert.That(quoted.Operand, Is.InstanceOf<LambdaExpression>());
  }

  [Test]
  public void GroupBy_KeySelector_BodyIsMemberAccess_ForKeyProperty()
  {
    var frame = new TypedFrame<SalesSchema>(_provider);

    var result = frame.GroupBy(x => x.Category);

    var mce = (MethodCallExpression)result.Expression;
    var lambda = (LambdaExpression)((UnaryExpression)mce.Arguments[1]).Operand;
    var member = lambda.Body as MemberExpression;
    Assert.That(member, Is.Not.Null);
    Assert.That(member!.Member.Name, Is.EqualTo(nameof(SalesSchema.Category)));
  }

  // ===================================================================
  //  Aggregate tree structure
  // ===================================================================

  [Test]
  public void Aggregate_ProducesMethodCallExpression_WithCorrectMethodName()
  {
    var frame = new TypedFrame<SalesSchema>(_provider);

    var result = frame
      .GroupBy(x => x.Category)
      .Aggregate(ctx => new SalesSummarySchema
      {
        Category = ctx.Key,
        TotalAmount = ctx.Sum(x => x.Amount),
        TotalCount = ctx.Count(),
      });

    var mce = result.Expression as MethodCallExpression;
    Assert.That(mce, Is.Not.Null);
    Assert.That(mce!.Method.Name, Is.EqualTo("Aggregate"));
  }

  [Test]
  public void Aggregate_ProducesNewResultType()
  {
    var frame = new TypedFrame<SalesSchema>(_provider);

    var result = frame
      .GroupBy(x => x.Category)
      .Aggregate(ctx => new SalesSummarySchema
      {
        Category = ctx.Key,
        TotalAmount = ctx.Sum(x => x.Amount),
        TotalCount = ctx.Count(),
      });

    Assert.That(result.ElementType, Is.EqualTo(typeof(SalesSummarySchema)));
  }

  [Test]
  public void Aggregate_HasTwoArguments_SourceAndResultSelector()
  {
    var frame = new TypedFrame<SalesSchema>(_provider);

    var result = frame
      .GroupBy(x => x.Category)
      .Aggregate(ctx => new SalesSummarySchema
      {
        Category = ctx.Key,
        TotalAmount = ctx.Sum(x => x.Amount),
        TotalCount = ctx.Count(),
      });

    var mce = (MethodCallExpression)result.Expression;
    Assert.That(mce.Arguments, Has.Count.EqualTo(2));
  }

  [Test]
  public void Aggregate_FirstArgument_IsGroupByCallExpression()
  {
    var frame = new TypedFrame<SalesSchema>(_provider);

    var result = frame
      .GroupBy(x => x.Category)
      .Aggregate(ctx => new SalesSummarySchema
      {
        Category = ctx.Key,
        TotalAmount = ctx.Sum(x => x.Amount),
        TotalCount = ctx.Count(),
      });

    var mce = (MethodCallExpression)result.Expression;
    var innerMce = mce.Arguments[0] as MethodCallExpression;
    Assert.That(innerMce, Is.Not.Null);
    Assert.That(innerMce!.Method.Name, Is.EqualTo("GroupBy"));
  }

  [Test]
  public void Aggregate_ResultSelector_IsQuotedLambda()
  {
    var frame = new TypedFrame<SalesSchema>(_provider);

    var result = frame
      .GroupBy(x => x.Category)
      .Aggregate(ctx => new SalesSummarySchema
      {
        Category = ctx.Key,
        TotalAmount = ctx.Sum(x => x.Amount),
        TotalCount = ctx.Count(),
      });

    var mce = (MethodCallExpression)result.Expression;
    var quoted = mce.Arguments[1] as UnaryExpression;
    Assert.That(quoted, Is.Not.Null);
    Assert.That(quoted!.NodeType, Is.EqualTo(ExpressionType.Quote));
    Assert.That(quoted.Operand, Is.InstanceOf<LambdaExpression>());
  }

  // ===================================================================
  //  AggregationContext method resolution in the expression body
  // ===================================================================

  [Test]
  public void Aggregate_ResultSelector_Body_ContainsMethodCallExpression_ForSum()
  {
    var frame = new TypedFrame<SalesSchema>(_provider);

    var result = frame
      .GroupBy(x => x.Category)
      .Aggregate(ctx => new SalesSummarySchema
      {
        Category = ctx.Key,
        TotalAmount = ctx.Sum(x => x.Amount),
        TotalCount = ctx.Count(),
      });

    var lambda = ExtractResultLambda(result);
    var mie = (MemberInitExpression)lambda.Body;

    // Find the TotalAmount binding — should be a Sum() MethodCallExpression
    var totalAmountBinding = mie
      .Bindings.OfType<MemberAssignment>()
      .Single(b => b.Member.Name == nameof(SalesSummarySchema.TotalAmount));

    var mce = totalAmountBinding.Expression as MethodCallExpression;
    Assert.That(mce, Is.Not.Null);
    Assert.That(mce!.Method.Name, Is.EqualTo(nameof(AggregationContext<object, object>.Sum)));
  }

  [Test]
  public void Aggregate_ResultSelector_Body_ContainsMethodCallExpression_ForCount()
  {
    var frame = new TypedFrame<SalesSchema>(_provider);

    var result = frame
      .GroupBy(x => x.Category)
      .Aggregate(ctx => new SalesSummarySchema
      {
        Category = ctx.Key,
        TotalAmount = ctx.Sum(x => x.Amount),
        TotalCount = ctx.Count(),
      });

    var lambda = ExtractResultLambda(result);
    var mie = (MemberInitExpression)lambda.Body;

    var totalCountBinding = mie
      .Bindings.OfType<MemberAssignment>()
      .Single(b => b.Member.Name == nameof(SalesSummarySchema.TotalCount));

    var mce = totalCountBinding.Expression as MethodCallExpression;
    Assert.That(mce, Is.Not.Null);
    Assert.That(mce!.Method.Name, Is.EqualTo(nameof(AggregationContext<object, object>.Count)));
  }

  // ===================================================================
  //  Helpers
  // ===================================================================

  private static LambdaExpression ExtractResultLambda<TResult>(TypedFrame<TResult> frame)
  {
    var mce = (MethodCallExpression)frame.Expression;
    return (LambdaExpression)((UnaryExpression)mce.Arguments[1]).Operand;
  }
}
