using System.Linq.Expressions;
using Flowthru.Misc.DataFrames;

namespace Flowthru.Extensions.Spark.Tests;

[TestFixture]
[Category("ExpressionTree")]
public class WhereExpressionTests
{
    private TestFrameProvider _provider = null!;

    [SetUp]
    public void SetUp()
    {
        _provider = new TestFrameProvider();
    }

    // ===================================================================
    //  Tree structure
    // ===================================================================

    [Test]
    public void Where_ProducesMethodCallExpression_WithCorrectMethodName()
    {
        var frame = new TypedFrame<PersonSchema>(_provider);

        var result = frame.Where(x => x.Age > 18);

        var mce = result.Expression as MethodCallExpression;
        Assert.That(mce, Is.Not.Null);
        Assert.That(mce!.Method.Name, Is.EqualTo("Where"));
    }

    [Test]
    public void Where_PreservesTypeParameter()
    {
        var frame = new TypedFrame<PersonSchema>(_provider);

        var result = frame.Where(x => x.Age > 18);

        Assert.That(result.ElementType, Is.EqualTo(typeof(PersonSchema)));
    }

    [Test]
    public void Where_HasTwoArguments_SourceAndQuotedPredicate()
    {
        var frame = new TypedFrame<PersonSchema>(_provider);

        var result = frame.Where(x => x.Age > 18);

        var mce = (MethodCallExpression)result.Expression;
        Assert.That(mce.Arguments, Has.Count.EqualTo(2));
    }

    [Test]
    public void Where_SecondArgument_IsQuotedLambda()
    {
        var frame = new TypedFrame<PersonSchema>(_provider);

        var result = frame.Where(x => x.Age > 18);

        var mce = (MethodCallExpression)result.Expression;
        var quotedArg = mce.Arguments[1] as UnaryExpression;
        Assert.That(quotedArg, Is.Not.Null);
        Assert.That(quotedArg!.NodeType, Is.EqualTo(ExpressionType.Quote));
        Assert.That(quotedArg.Operand, Is.InstanceOf<LambdaExpression>());
    }

    // ===================================================================
    //  Predicate lambda structure
    // ===================================================================

    [Test]
    public void Where_SimpleBinaryPredicate_HasGreaterThanNodeType()
    {
        var frame = new TypedFrame<PersonSchema>(_provider);

        var result = frame.Where(x => x.Age > 18);

        var lambda = ExtractLambda(result);
        var body = lambda.Body as BinaryExpression;
        Assert.That(body, Is.Not.Null);
        Assert.That(body!.NodeType, Is.EqualTo(ExpressionType.GreaterThan));
    }

    [Test]
    public void Where_PropertyAccess_ReferencesCorrectMember()
    {
        var frame = new TypedFrame<PersonSchema>(_provider);

        var result = frame.Where(x => x.Age > 18);

        var lambda = ExtractLambda(result);
        var binary = (BinaryExpression)lambda.Body;
        var memberAccess = binary.Left as MemberExpression;
        Assert.That(memberAccess, Is.Not.Null);
        Assert.That(memberAccess!.Member.Name, Is.EqualTo("Age"));
    }

    [Test]
    public void Where_BooleanPredicate_ProducesMemberAccessDirectly()
    {
        var frame = new TypedFrame<PersonSchema>(_provider);

        var result = frame.Where(x => x.IsActive);

        var lambda = ExtractLambda(result);
        var memberAccess = lambda.Body as MemberExpression;
        Assert.That(memberAccess, Is.Not.Null);
        Assert.That(memberAccess!.Member.Name, Is.EqualTo("IsActive"));
    }

    // ===================================================================
    //  Chaining
    // ===================================================================

    [Test]
    public void Where_Chained_NestsExpressionsCorrectly()
    {
        var frame = new TypedFrame<PersonSchema>(_provider);

        var result = frame.Where(x => x.Age > 18).Where(x => x.IsActive);

        // Outer is a Where call
        var outer = (MethodCallExpression)result.Expression;
        Assert.That(outer.Method.Name, Is.EqualTo("Where"));

        // Its source (first argument) is also a Where call
        var inner = outer.Arguments[0] as MethodCallExpression;
        Assert.That(inner, Is.Not.Null);
        Assert.That(inner!.Method.Name, Is.EqualTo("Where"));
    }

    // ===================================================================
    //  Helpers
    // ===================================================================

    private static LambdaExpression ExtractLambda<T>(TypedFrame<T> frame)
    {
        var mce = (MethodCallExpression)frame.Expression;
        var quote = (UnaryExpression)mce.Arguments[1];
        return (LambdaExpression)quote.Operand;
    }
}
