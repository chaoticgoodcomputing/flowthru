using System.Linq.Expressions;
using Flowthru.Misc.DataFrames;

namespace Flowthru.Extensions.Spark.Tests;

/// <summary>
/// Schema for testing string method expression translation.
/// </summary>
public record ProductSchema
{
  public required string Sku { get; init; }
  public required string Category { get; init; }
  public required string Description { get; init; }
}

/// <summary>
/// Aggregate result schema for string-method Select tests.
/// </summary>
public record ProductNormalizedSchema
{
  public required string Sku { get; init; }
  public required string Category { get; init; }
}

[TestFixture]
[Category("ExpressionTree")]
public class StringMethodExpressionTests
{
  private TestFrameProvider _provider = null!;

  [SetUp]
  public void SetUp()
  {
    _provider = new TestFrameProvider();
  }

  // ===================================================================
  //  ToUpper / ToLower
  // ===================================================================

  [Test]
  public void Select_WithToUpper_BodyContainsMethodCall_Named_ToUpper()
  {
    var frame = new TypedFrame<ProductSchema>(_provider);

    var result = frame.Select(x => new ProductNormalizedSchema
    {
      Sku = x.Sku.ToUpper(),
      Category = x.Category,
    });

    var mce = ExtractBindingMethodCall(result, nameof(ProductNormalizedSchema.Sku));
    Assert.That(mce, Is.Not.Null);
    Assert.That(mce!.Method.Name, Is.EqualTo(nameof(string.ToUpper)));
    Assert.That(mce.Method.DeclaringType, Is.EqualTo(typeof(string)));
  }

  [Test]
  public void Select_WithToLower_BodyContainsMethodCall_Named_ToLower()
  {
    var frame = new TypedFrame<ProductSchema>(_provider);

    var result = frame.Select(x => new ProductNormalizedSchema
    {
      Sku = x.Sku,
      Category = x.Category.ToLower(),
    });

    var mce = ExtractBindingMethodCall(result, nameof(ProductNormalizedSchema.Category));
    Assert.That(mce, Is.Not.Null);
    Assert.That(mce!.Method.Name, Is.EqualTo(nameof(string.ToLower)));
  }

  // ===================================================================
  //  Contains / StartsWith / EndsWith
  // ===================================================================

  [Test]
  public void Where_WithStringContains_BodyContainsMethodCall_Named_Contains()
  {
    var frame = new TypedFrame<ProductSchema>(_provider);

    var result = frame.Where(x => x.Description.Contains("organic"));

    var mce = ExtractWherePredicateCall(result);
    Assert.That(mce, Is.Not.Null);
    Assert.That(mce!.Method.Name, Is.EqualTo(nameof(string.Contains)));
    Assert.That(mce.Method.DeclaringType, Is.EqualTo(typeof(string)));
  }

  [Test]
  public void Where_WithStringContains_HasCorrectLiteralArgument()
  {
    var frame = new TypedFrame<ProductSchema>(_provider);

    var result = frame.Where(x => x.Sku.Contains("BULK"));

    var mce = ExtractWherePredicateCall(result);
    var arg = mce!.Arguments[0] as ConstantExpression;
    Assert.That(arg, Is.Not.Null);
    Assert.That(arg!.Value, Is.EqualTo("BULK"));
  }

  [Test]
  public void Where_WithStartsWith_BodyContainsMethodCall_Named_StartsWith()
  {
    var frame = new TypedFrame<ProductSchema>(_provider);

    var result = frame.Where(x => x.Category.StartsWith("Food"));

    var mce = ExtractWherePredicateCall(result);
    Assert.That(mce, Is.Not.Null);
    Assert.That(mce!.Method.Name, Is.EqualTo(nameof(string.StartsWith)));
  }

  [Test]
  public void Where_WithEndsWith_BodyContainsMethodCall_Named_EndsWith()
  {
    var frame = new TypedFrame<ProductSchema>(_provider);

    var result = frame.Where(x => x.Sku.EndsWith("-XL"));

    var mce = ExtractWherePredicateCall(result);
    Assert.That(mce, Is.Not.Null);
    Assert.That(mce!.Method.Name, Is.EqualTo(nameof(string.EndsWith)));
  }

  // ===================================================================
  //  Replace
  // ===================================================================

  [Test]
  public void Select_WithReplace_BodyContainsMethodCall_Named_Replace()
  {
    var frame = new TypedFrame<ProductSchema>(_provider);

    var result = frame.Select(x => new ProductNormalizedSchema
    {
      Sku = x.Sku.Replace("-", "_"),
      Category = x.Category,
    });

    var mce = ExtractBindingMethodCall(result, nameof(ProductNormalizedSchema.Sku));
    Assert.That(mce, Is.Not.Null);
    Assert.That(mce!.Method.Name, Is.EqualTo(nameof(string.Replace)));
  }

  [Test]
  public void Select_WithReplace_HasTwoArguments_OldAndNew()
  {
    var frame = new TypedFrame<ProductSchema>(_provider);

    var result = frame.Select(x => new ProductNormalizedSchema
    {
      Sku = x.Sku.Replace("-", "_"),
      Category = x.Category,
    });

    var mce = ExtractBindingMethodCall(result, nameof(ProductNormalizedSchema.Sku));
    Assert.That(mce!.Arguments, Has.Count.EqualTo(2));

    var arg0 = mce.Arguments[0] as ConstantExpression;
    var arg1 = mce.Arguments[1] as ConstantExpression;
    Assert.That(arg0!.Value, Is.EqualTo("-"));
    Assert.That(arg1!.Value, Is.EqualTo("_"));
  }

  // ===================================================================
  //  string.Length (property access → MemberExpression)
  // ===================================================================

  [Test]
  public void Where_WithStringLengthComparison_BodyContainsMemberAccess_Named_Length()
  {
    var frame = new TypedFrame<ProductSchema>(_provider);

    var result = frame.Where(x => x.Description.Length > 10);

    var mce = (MethodCallExpression)result.Expression;
    var lambda = (LambdaExpression)((UnaryExpression)mce.Arguments[1]).Operand;

    // lambda body is: x.Description.Length > 10
    var binary = (BinaryExpression)lambda.Body;
    var left = binary.Left;

    // string.Length is a MemberExpression
    Assert.That(left, Is.InstanceOf<MemberExpression>());
    var me = (MemberExpression)left;
    Assert.That(me.Member.Name, Is.EqualTo(nameof(string.Length)));
    Assert.That(me.Member.DeclaringType, Is.EqualTo(typeof(string)));
  }

  // ===================================================================
  //  Helpers
  // ===================================================================

  private static MethodCallExpression? ExtractBindingMethodCall(
    TypedFrame<ProductNormalizedSchema> frame,
    string memberName
  )
  {
    var mce = (MethodCallExpression)frame.Expression;
    var lambda = (LambdaExpression)((UnaryExpression)mce.Arguments[1]).Operand;
    var mie = (MemberInitExpression)lambda.Body;
    var binding = mie.Bindings.OfType<MemberAssignment>().Single(b => b.Member.Name == memberName);
    return binding.Expression as MethodCallExpression;
  }

  private static MethodCallExpression? ExtractWherePredicateCall(TypedFrame<ProductSchema> frame)
  {
    var mce = (MethodCallExpression)frame.Expression;
    var lambda = (LambdaExpression)((UnaryExpression)mce.Arguments[1]).Operand;
    return lambda.Body as MethodCallExpression;
  }
}
