using System.Linq.Expressions;
using Flowthru.DataFrames;

namespace Flowthru.Extensions.Spark.Tests;

[TestFixture]
[Category("ExpressionTree")]
public class OrderByExpressionTests
{
  private TestFrameProvider _provider = null!;

  [SetUp]
  public void SetUp()
  {
    _provider = new TestFrameProvider();
  }

  // ===================================================================
  //  Tree structure — ascending
  // ===================================================================

  [Test]
  public void OrderBy_ProducesMethodCallExpression_WithCorrectMethodName()
  {
    var frame = new TypedFrame<PersonSchema>(_provider);

    var result = frame.OrderBy(x => x.Age);

    var mce = result.Expression as MethodCallExpression;
    Assert.That(mce, Is.Not.Null);
    Assert.That(mce!.Method.Name, Is.EqualTo("OrderBy"));
  }

  [Test]
  public void OrderBy_PreservesSourceTypeParameter()
  {
    var frame = new TypedFrame<PersonSchema>(_provider);

    var result = frame.OrderBy(x => x.Age);

    Assert.That(result.ElementType, Is.EqualTo(typeof(PersonSchema)));
  }

  [Test]
  public void OrderBy_HasTwoArguments_SourceAndKeySelector()
  {
    var frame = new TypedFrame<PersonSchema>(_provider);

    var result = frame.OrderBy(x => x.Age);

    var mce = (MethodCallExpression)result.Expression;
    Assert.That(mce.Arguments, Has.Count.EqualTo(2));
  }

  [Test]
  public void OrderBy_KeySelector_IsQuotedLambda()
  {
    var frame = new TypedFrame<PersonSchema>(_provider);

    var result = frame.OrderBy(x => x.Age);

    var mce = (MethodCallExpression)result.Expression;
    var quoted = mce.Arguments[1] as UnaryExpression;
    Assert.That(quoted, Is.Not.Null);
    Assert.That(quoted!.NodeType, Is.EqualTo(ExpressionType.Quote));
    Assert.That(quoted.Operand, Is.InstanceOf<LambdaExpression>());
  }

  [Test]
  public void OrderBy_KeySelector_BodyIsMemberAccess_ForKeyProperty()
  {
    var frame = new TypedFrame<PersonSchema>(_provider);

    var result = frame.OrderBy(x => x.Name);

    var mce = (MethodCallExpression)result.Expression;
    var lambda = (LambdaExpression)((UnaryExpression)mce.Arguments[1]).Operand;
    var member = lambda.Body as MemberExpression;
    Assert.That(member, Is.Not.Null);
    Assert.That(member!.Member.Name, Is.EqualTo(nameof(PersonSchema.Name)));
  }

  // ===================================================================
  //  Tree structure — descending
  // ===================================================================

  [Test]
  public void OrderByDescending_ProducesMethodCallExpression_WithCorrectMethodName()
  {
    var frame = new TypedFrame<PersonSchema>(_provider);

    var result = frame.OrderByDescending(x => x.Age);

    var mce = result.Expression as MethodCallExpression;
    Assert.That(mce, Is.Not.Null);
    Assert.That(mce!.Method.Name, Is.EqualTo("OrderByDescending"));
  }

  [Test]
  public void OrderByDescending_PreservesSourceTypeParameter()
  {
    var frame = new TypedFrame<PersonSchema>(_provider);

    var result = frame.OrderByDescending(x => x.Age);

    Assert.That(result.ElementType, Is.EqualTo(typeof(PersonSchema)));
  }

  [Test]
  public void OrderByDescending_KeySelector_IsQuotedLambda()
  {
    var frame = new TypedFrame<PersonSchema>(_provider);

    var result = frame.OrderByDescending(x => x.Age);

    var mce = (MethodCallExpression)result.Expression;
    var quoted = mce.Arguments[1] as UnaryExpression;
    Assert.That(quoted, Is.Not.Null);
    Assert.That(quoted!.NodeType, Is.EqualTo(ExpressionType.Quote));
    Assert.That(quoted.Operand, Is.InstanceOf<LambdaExpression>());
  }

  // ===================================================================
  //  Chaining
  // ===================================================================

  [Test]
  public void OrderBy_CanChainWithWhere()
  {
    var frame = new TypedFrame<PersonSchema>(_provider);

    var result = frame.Where(x => x.IsActive).OrderBy(x => x.Name);

    var mce = result.Expression as MethodCallExpression;
    Assert.That(mce, Is.Not.Null);
    Assert.That(mce!.Method.Name, Is.EqualTo("OrderBy"));

    // Inner expression is the Where call
    var innerMce = mce.Arguments[0] as MethodCallExpression;
    Assert.That(innerMce, Is.Not.Null);
    Assert.That(innerMce!.Method.Name, Is.EqualTo("Where"));
  }

  private static LambdaExpression ExtractLambda(TypedFrame<PersonSchema> frame)
  {
    var mce = (MethodCallExpression)frame.Expression;
    return (LambdaExpression)((UnaryExpression)mce.Arguments[1]).Operand;
  }
}
