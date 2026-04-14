using System.Reflection;
using Flowthru.DataFrames;

namespace Flowthru.Extensions.Spark.Tests;

[TestFixture]
[Category("ExpressionTree")]
public class ColumnNameResolutionTests
{
  // ===================================================================
  //  ResolveColumnName via FrameExpressionVisitor (protected static,
  //  tested indirectly via a minimal subclass)
  // ===================================================================

  [Test]
  public void ResolveColumnName_WithoutSerializedLabel_ReturnsPropertyName()
  {
    var member = typeof(PersonSchema).GetProperty(nameof(PersonSchema.Name))!;

    var result = TestableVisitor.TestResolveColumnName(member);

    Assert.That(result, Is.EqualTo("Name"));
  }

  [Test]
  public void ResolveColumnName_WithSerializedLabel_ReturnsLabelValue()
  {
    var member = typeof(LabeledSchema).GetProperty(nameof(LabeledSchema.FullName))!;

    var result = TestableVisitor.TestResolveColumnName(member);

    Assert.That(result, Is.EqualTo("full_name"));
  }

  [Test]
  public void ResolveColumnName_PropertyWithoutLabel_FallsBackToPropertyName()
  {
    var member = typeof(LabeledSchema).GetProperty(nameof(LabeledSchema.Department))!;

    var result = TestableVisitor.TestResolveColumnName(member);

    Assert.That(result, Is.EqualTo("Department"));
  }

  [Test]
  public void ResolveColumnName_AllLabeledProperties_ResolveCorrectly()
  {
    var props = typeof(LabeledSchema).GetProperties();
    var expected = new Dictionary<string, string>
    {
      ["FullName"] = "full_name",
      ["EmployeeId"] = "employee_id",
      ["Department"] = "Department",
    };

    foreach (var prop in props)
    {
      var resolved = TestableVisitor.TestResolveColumnName(prop);
      Assert.That(resolved, Is.EqualTo(expected[prop.Name]), $"Failed for property {prop.Name}");
    }
  }

  // ===================================================================
  //  Minimal subclass exposing protected static methods for testing
  // ===================================================================

  private class TestableVisitor : FrameExpressionVisitor
  {
    public static string TestResolveColumnName(MemberInfo member) => ResolveColumnName(member);

    protected override object TranslateConstant(System.Linq.Expressions.ConstantExpression node) =>
      throw new NotImplementedException();

    protected override object TranslateWhere(System.Linq.Expressions.MethodCallExpression node) =>
      throw new NotImplementedException();

    protected override object TranslateSelect(System.Linq.Expressions.MethodCallExpression node) =>
      throw new NotImplementedException();

    protected override object TranslateJoin(System.Linq.Expressions.MethodCallExpression node) =>
      throw new NotImplementedException();

    protected override object TranslateOrderBy(
      System.Linq.Expressions.MethodCallExpression node,
      bool descending
    ) => throw new NotImplementedException();

    protected override object TranslateTake(System.Linq.Expressions.MethodCallExpression node) =>
      throw new NotImplementedException();

    protected override object TranslateCount(System.Linq.Expressions.MethodCallExpression node) =>
      throw new NotImplementedException();

    protected override object TranslateDistinct(System.Linq.Expressions.MethodCallExpression node) =>
      throw new NotImplementedException();

    protected override object TranslateUnion(System.Linq.Expressions.MethodCallExpression node) =>
      throw new NotImplementedException();

    protected override object TranslateGroupBy(System.Linq.Expressions.MethodCallExpression node) =>
      throw new NotImplementedException();

    protected override object TranslateAggregate(
      System.Linq.Expressions.MethodCallExpression node
    ) => throw new NotImplementedException();
  }
}
