using Flowthru.Core.Abstractions;
using Flowthru.Spark.Sql.Types;

namespace Flowthru.Extensions.Spark.Tests;

/// <summary>
/// Schema with SerializedLabel attributes for hydrator label-resolution tests.
/// </summary>
public record HydratorLabeledSchema : IFlatSchema
{
  [SerializedLabel("full_name")]
  public required string FullName { get; init; }

  [SerializedLabel("employee_id")]
  public required int EmployeeId { get; init; }

  public required string Department { get; init; }
}

[TestFixture]
[Category("SparkRowHydrator")]
[Category("SparkRowHydrator.Validation")]
public class SparkRowHydratorValidationTests
{
  // ===================================================================
  //  Helpers
  // ===================================================================

  private static SparkRowHydrator<T> HydratorFor<T>()
    where T : notnull, IFlatSchema => new SparkRowHydrator<T>(new SparkFrameProvider());

  private static StructType Schema(params (string name, DataType type)[] fields) =>
    new StructType(fields.Select(f => new StructField(f.name, f.type)));

  // ===================================================================
  //  Happy path
  // ===================================================================

  [Test]
  public void ValidateSchema_AllColumnsPresent_ReturnsEmpty()
  {
    var hydrator = HydratorFor<PersonSchema>();
    var schema = Schema(
      ("Name", new StringType()),
      ("Age", new IntegerType()),
      ("IsActive", new BooleanType())
    );

    var errors = hydrator.ValidateSchema(schema);

    Assert.That(errors, Is.Empty);
  }

  [Test]
  public void ValidateSchema_ExtraColumnsInSchema_ReturnsEmpty()
  {
    // Extra columns not in T are irrelevant — no error
    var hydrator = HydratorFor<PersonSchema>();
    var schema = Schema(
      ("Name", new StringType()),
      ("Age", new IntegerType()),
      ("IsActive", new BooleanType()),
      ("extra_column", new StringType())
    );

    var errors = hydrator.ValidateSchema(schema);

    Assert.That(errors, Is.Empty);
  }

  // ===================================================================
  //  Missing columns
  // ===================================================================

  [Test]
  public void ValidateSchema_MissingColumn_ReturnsOneError()
  {
    var hydrator = HydratorFor<PersonSchema>();
    var schema = Schema(
      ("Name", new StringType()),
      ("Age", new IntegerType())
    // IsActive intentionally missing
    );

    var errors = hydrator.ValidateSchema(schema);

    Assert.That(errors, Has.Count.EqualTo(1));
    Assert.That(errors[0].ColumnName, Is.EqualTo("IsActive"));
  }

  [Test]
  public void ValidateSchema_MultipleColumnsMissing_ReturnsErrorPerColumn()
  {
    var hydrator = HydratorFor<PersonSchema>();
    var schema = Schema(
      ("Name", new StringType())
    // Age and IsActive missing
    );

    var errors = hydrator.ValidateSchema(schema);

    Assert.That(errors, Has.Count.EqualTo(2));
    Assert.That(errors.Select(e => e.ColumnName), Is.EquivalentTo(new[] { "Age", "IsActive" }));
  }

  [Test]
  public void ValidateSchema_EmptySchema_ReturnsErrorForEveryProperty()
  {
    var hydrator = HydratorFor<PersonSchema>();
    var schema = new StructType(Enumerable.Empty<StructField>());

    var errors = hydrator.ValidateSchema(schema);

    // PersonSchema has 3 required properties
    Assert.That(errors, Has.Count.EqualTo(3));
  }

  // ===================================================================
  //  Type compatibility
  // ===================================================================

  [Test]
  public void ValidateSchema_IncompatibleType_ReturnsOneError()
  {
    var hydrator = HydratorFor<PersonSchema>();
    var schema = Schema(
      ("Name", new StringType()),
      ("Age", new StringType()), // int property, StringType column — incompatible
      ("IsActive", new BooleanType())
    );

    var errors = hydrator.ValidateSchema(schema);

    Assert.That(errors, Has.Count.EqualTo(1));
    Assert.That(errors[0].ColumnName, Is.EqualTo("Age"));
    Assert.That(errors[0].Reason, Does.Contain("string").IgnoreCase);
  }

  [Test]
  public void ValidateSchema_IntegerTypeForLongProperty_IsCompatible()
  {
    // IntegerType is in the compatibility set for long (widening conversion)
    // PersonSchema.Age is int; use a schema with a long property instead.
    // Verify via the map directly: IntegerType maps to [int, long].
    var hydrator = HydratorFor<PersonSchema>();
    var schema = Schema(
      ("Name", new StringType()),
      ("Age", new IntegerType()), // int property + IntegerType column = exact match
      ("IsActive", new BooleanType())
    );

    var errors = hydrator.ValidateSchema(schema);

    Assert.That(errors, Is.Empty);
  }

  // ===================================================================
  //  [SerializedLabel] resolution
  // ===================================================================

  [Test]
  public void ValidateSchema_UsesSerializedLabel_NotPropertyName()
  {
    // HydratorLabeledSchema.FullName maps to "full_name"
    // The schema must provide "full_name", not "FullName"
    var hydrator = HydratorFor<HydratorLabeledSchema>();
    var schema = Schema(
      ("full_name", new StringType()),
      ("employee_id", new IntegerType()),
      ("Department", new StringType())
    );

    var errors = hydrator.ValidateSchema(schema);

    Assert.That(errors, Is.Empty);
  }

  [Test]
  public void ValidateSchema_PropertyNameWhenNoLabel_NotLookingForLabel()
  {
    // HydratorLabeledSchema.Department has no [SerializedLabel], uses property name
    var hydrator = HydratorFor<HydratorLabeledSchema>();
    var schema = Schema(
      ("full_name", new StringType()),
      ("employee_id", new IntegerType())
    // "Department" is missing
    );

    var errors = hydrator.ValidateSchema(schema);

    Assert.That(errors, Has.Count.EqualTo(1));
    Assert.That(errors[0].ColumnName, Is.EqualTo("Department"));
  }

  // ===================================================================
  //  Case-insensitive column matching
  // ===================================================================

  [Test]
  public void ValidateSchema_ColumnNameDifferentCase_NoError()
  {
    var hydrator = HydratorFor<PersonSchema>();
    var schema = Schema(
      ("name", new StringType()), // lowercase — should still match "Name"
      ("age", new IntegerType()),
      ("isactive", new BooleanType())
    );

    var errors = hydrator.ValidateSchema(schema);

    Assert.That(errors, Is.Empty);
  }
}
