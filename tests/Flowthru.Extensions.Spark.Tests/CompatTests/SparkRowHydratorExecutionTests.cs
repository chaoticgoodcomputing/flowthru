using Flowthru.Core.Abstractions;
using Flowthru.Misc.DataFrames;
using Flowthru.Spark.Sql;
using Flowthru.Spark.Sql.Types;

namespace Flowthru.Extensions.Spark.Tests.CompatTests;

/// <summary>
/// End-to-end tests for <see cref="SparkRowHydrator{T}"/> that require a live JVM.
/// These tests exercise the full pipeline: TypedFrame → CompileToNative → ValidateSchema
/// → Collect → hydrated rows.
///
/// Automatically skipped (Inconclusive) when SPARK_HOME is not set.
///
/// To run locally:
///   brew install apache-spark
///   export SPARK_HOME=$(brew --prefix apache-spark)/libexec
///   dotnet test --filter "Category=SparkRowHydrator.Execution"
/// </summary>
[TestFixture]
[Category("SparkRowHydrator")]
[Category("SparkRowHydrator.Execution")]
public class SparkRowHydratorExecutionTests
{
  // SparkSession is stopped via .Stop() rather than IDisposable; NUnit1032 suppressed.
#pragma warning disable NUnit1032
  private SparkSession? _spark;
#pragma warning restore NUnit1032
  private SparkFrameProvider? _provider;

  [OneTimeSetUp]
  public void StartSpark()
  {
    Assume.That(
      SparkAssemblySetup.IsAvailable,
      Is.True,
      SparkAssemblySetup.UnavailableReason ?? "Spark JVM backend unavailable."
    );

    try
    {
      _spark = SparkSession
        .Builder()
        .AppName("flowthru-hydrator-tests")
        .Master("local[*]")
        .GetOrCreate();

      _provider = new SparkFrameProvider();
    }
    catch (Exception ex)
    {
      Assert.Inconclusive(
        $"Spark JVM backend failed to start — skipping hydrator execution tests. ({ex.Message})"
      );
    }
  }

  [OneTimeTearDown]
  public void StopSpark()
  {
    _spark?.Stop();
  }

  // Convenience: build a DataFrame with PersonSchema-compatible columns
  private DataFrame PersonDataFrame(params (string name, int age, bool isActive)[] rows)
  {
    var schema = new StructType(
      [
        new StructField("Name", new StringType()),
        new StructField("Age", new IntegerType()),
        new StructField("IsActive", new BooleanType()),
      ]
    );

    var genericRows = rows.Select(r => new GenericRow([r.name, r.age, r.isActive]));
    return _spark!.CreateDataFrame(genericRows, schema);
  }

  // ===================================================================
  //  ValidateSchema(DataFrame) — outer overload with live JVM
  // ===================================================================

  [Test]
  public void ValidateSchema_DataFrame_CompatibleSchema_ReturnsEmpty()
  {
    var hydrator = new SparkRowHydrator<PersonSchema>(_provider!);
    var df = PersonDataFrame(("Alice", 30, true));

    var errors = hydrator.ValidateSchema(df);

    Assert.That(errors, Is.Empty);
  }

  [Test]
  public void ValidateSchema_DataFrame_MissingColumn_ReturnsError()
  {
    var hydrator = new SparkRowHydrator<PersonSchema>(_provider!);

    // DataFrame has only Name and Age — IsActive is missing
    var schema = new StructType(
      [new StructField("Name", new StringType()), new StructField("Age", new IntegerType())]
    );
    var df = _spark!.CreateDataFrame([new GenericRow(["Alice", 30])], schema);

    var errors = hydrator.ValidateSchema(df);

    Assert.That(errors, Has.Count.EqualTo(1));
    Assert.That(errors[0].ColumnName, Is.EqualTo("IsActive"));
  }

  // ===================================================================
  //  Collect — full round-trip
  // ===================================================================

  [Test]
  public void Collect_ReturnsAllRows()
  {
    var hydrator = new SparkRowHydrator<PersonSchema>(_provider!);
    var df = PersonDataFrame(("Alice", 30, true), ("Bob", 25, false), ("Carol", 40, true));
    var frame = _provider!.CreateFromNative<PersonSchema>(df);

    var rows = hydrator.Collect(frame).ToList();

    Assert.That(rows, Has.Count.EqualTo(3));
  }

  [Test]
  public void Collect_HydratesPropertyValues()
  {
    var hydrator = new SparkRowHydrator<PersonSchema>(_provider!);
    var df = PersonDataFrame(("Alice", 30, true));
    var frame = _provider!.CreateFromNative<PersonSchema>(df);

    var rows = hydrator.Collect(frame).ToList();
    var alice = rows.Single();

    Assert.That(alice.Name, Is.EqualTo("Alice"));
    Assert.That(alice.Age, Is.EqualTo(30));
    Assert.That(alice.IsActive, Is.True);
  }

  [Test]
  public void Collect_AfterWhere_FiltersRows()
  {
    var hydrator = new SparkRowHydrator<PersonSchema>(_provider!);
    var df = PersonDataFrame(("Alice", 30, true), ("Bob", 17, false), ("Carol", 40, true));
    var frame =
      (TypedFrame<PersonSchema>)
        _provider!.CreateFromNative<PersonSchema>(df).Where(x => x.Age > 18);

    var rows = hydrator.Collect(frame).ToList();

    Assert.That(rows, Has.Count.EqualTo(2));
    Assert.That(rows.Select(r => r.Name), Is.EquivalentTo(new[] { "Alice", "Carol" }));
  }

  [Test]
  public void Collect_SchemaMismatch_ThrowsBeforeCollect()
  {
    var hydrator = new SparkRowHydrator<PersonSchema>(_provider!);

    // DataFrame has Age as StringType — incompatible with int property
    var schema = new StructType(
      [
        new StructField("Name", new StringType()),
        new StructField("Age", new StringType()), // wrong type
        new StructField("IsActive", new BooleanType()),
      ]
    );
    var df = _spark!.CreateDataFrame([new GenericRow(["Alice", "thirty", true])], schema);
    var frame = _provider!.CreateFromNative<PersonSchema>(df);

    Assert.That(
      () => hydrator.Collect(frame).ToList(),
      Throws.TypeOf<InvalidOperationException>().With.Message.Contains("Age")
    );
  }

  // ===================================================================
  //  [SerializedLabel] round-trip
  // ===================================================================

  [Test]
  public void Collect_HonoursSerializedLabel_InColumnLookup()
  {
    var hydrator = new SparkRowHydrator<HydratorLabeledSchema>(_provider!);

    // Columns use the serialized names (snake_case), not C# property names
    var schema = new StructType(
      [
        new StructField("full_name", new StringType()),
        new StructField("employee_id", new IntegerType()),
        new StructField("Department", new StringType()),
      ]
    );
    var df = _spark!.CreateDataFrame([new GenericRow(["Alice Smith", 42, "Engineering"])], schema);
    var frame = _provider!.CreateFromNative<HydratorLabeledSchema>(df);

    var rows = hydrator.Collect(frame).ToList();
    var row = rows.Single();

    Assert.That(row.FullName, Is.EqualTo("Alice Smith"));
    Assert.That(row.EmployeeId, Is.EqualTo(42));
    Assert.That(row.Department, Is.EqualTo("Engineering"));
  }
}
