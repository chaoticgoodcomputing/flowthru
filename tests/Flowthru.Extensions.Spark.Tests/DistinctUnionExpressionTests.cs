using System.Linq.Expressions;
using Flowthru.Misc.DataFrames;

namespace Flowthru.Extensions.Spark.Tests;

[TestFixture]
[Category("ExpressionTree")]
public class DistinctUnionExpressionTests
{
    private TestFrameProvider _provider = null!;

    [SetUp]
    public void SetUp()
    {
        _provider = new TestFrameProvider();
    }

    // ===================================================================
    //  Distinct
    // ===================================================================

    [Test]
    public void Distinct_ProducesMethodCallExpression_WithCorrectMethodName()
    {
        var frame = new TypedFrame<PersonSchema>(_provider);

        var result = frame.Distinct();

        var mce = result.Expression as MethodCallExpression;
        Assert.That(mce, Is.Not.Null);
        Assert.That(mce!.Method.Name, Is.EqualTo("Distinct"));
    }

    [Test]
    public void Distinct_PreservesSourceTypeParameter()
    {
        var frame = new TypedFrame<PersonSchema>(_provider);

        var result = frame.Distinct();

        Assert.That(result.ElementType, Is.EqualTo(typeof(PersonSchema)));
    }

    [Test]
    public void Distinct_HasOneArgument_TheSourceExpression()
    {
        var frame = new TypedFrame<PersonSchema>(_provider);

        var result = frame.Distinct();

        var mce = (MethodCallExpression)result.Expression;
        Assert.That(mce.Arguments, Has.Count.EqualTo(1));
    }

    [Test]
    public void Distinct_CanChainAfterWhere()
    {
        var frame = new TypedFrame<PersonSchema>(_provider);

        var result = frame.Where(x => x.IsActive).Distinct();

        var mce = result.Expression as MethodCallExpression;
        Assert.That(mce!.Method.Name, Is.EqualTo("Distinct"));

        var innerMce = mce.Arguments[0] as MethodCallExpression;
        Assert.That(innerMce!.Method.Name, Is.EqualTo("Where"));
    }

    // ===================================================================
    //  Union
    // ===================================================================

    [Test]
    public void Union_ProducesMethodCallExpression_WithCorrectMethodName()
    {
        var left = new TypedFrame<PersonSchema>(_provider);
        var right = new TypedFrame<PersonSchema>(_provider);

        var result = left.Union(right);

        var mce = result.Expression as MethodCallExpression;
        Assert.That(mce, Is.Not.Null);
        Assert.That(mce!.Method.Name, Is.EqualTo("Union"));
    }

    [Test]
    public void Union_PreservesSourceTypeParameter()
    {
        var left = new TypedFrame<PersonSchema>(_provider);
        var right = new TypedFrame<PersonSchema>(_provider);

        var result = left.Union(right);

        Assert.That(result.ElementType, Is.EqualTo(typeof(PersonSchema)));
    }

    [Test]
    public void Union_HasTwoArguments_LeftAndRight()
    {
        var left = new TypedFrame<PersonSchema>(_provider);
        var right = new TypedFrame<PersonSchema>(_provider);

        var result = left.Union(right);

        var mce = (MethodCallExpression)result.Expression;
        Assert.That(mce.Arguments, Has.Count.EqualTo(2));
    }

    [Test]
    public void Union_BothArguments_AreConstantExpressions_ForRootFrames()
    {
        var left = new TypedFrame<PersonSchema>(_provider);
        var right = new TypedFrame<PersonSchema>(_provider);

        var result = left.Union(right);

        var mce = (MethodCallExpression)result.Expression;
        Assert.That(mce.Arguments[0], Is.InstanceOf<ConstantExpression>());
        Assert.That(mce.Arguments[1], Is.InstanceOf<ConstantExpression>());
    }

    [Test]
    public void Union_CanChainWithDistinct()
    {
        var left = new TypedFrame<PersonSchema>(_provider);
        var right = new TypedFrame<PersonSchema>(_provider);

        var result = left.Union(right).Distinct();

        var mce = result.Expression as MethodCallExpression;
        Assert.That(mce!.Method.Name, Is.EqualTo("Distinct"));

        var innerMce = mce.Arguments[0] as MethodCallExpression;
        Assert.That(innerMce!.Method.Name, Is.EqualTo("Union"));
    }
}
