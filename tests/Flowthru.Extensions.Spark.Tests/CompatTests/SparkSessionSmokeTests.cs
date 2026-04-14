using Flowthru.Spark.Sql;

namespace Flowthru.Extensions.Spark.Tests.CompatTests;

/// <summary>
/// Smoke tests validating Microsoft.Spark compatibility with the current target framework.
/// Layer 1 tests verify assembly loading (no JVM required).
/// Layer 2 tests verify JVM bridge functionality (requires Java, Spark, and Microsoft.Spark.Worker).
/// </summary>
[TestFixture]
[Category("SparkSession")]
public class SparkSessionSmokeTests
{
  // =================================================================
  //  Layer 1 — Assembly Loading (no JVM required)
  //
  //  Builder's constructor immediately calls into the JVM bridge,
  //  so Layer 1 can only verify that types resolve via reflection —
  //  not instantiate them.
  // =================================================================

  [Test]
  [Category("SparkSession.AssemblyLoading")]
  public void SparkSession_TypeLoads_OnCurrentFramework()
  {
    // If Microsoft.Spark types can't resolve on this TFM, this will throw
    // TypeLoadException or FileLoadException before reaching the assert.
    var sessionType = typeof(SparkSession);

    Assert.That(sessionType, Is.Not.Null);
    Assert.That(sessionType.AssemblyQualifiedName, Does.Contain("Flowthru.Spark"));
  }

  [Test]
  [Category("SparkSession.AssemblyLoading")]
  public void Builder_TypeLoads_OnCurrentFramework()
  {
    // Builder is a top-level class in Microsoft.Spark.Sql, not nested.
    var builderType = typeof(Builder);

    Assert.That(builderType, Is.Not.Null);
    Assert.That(builderType.AssemblyQualifiedName, Does.Contain("Flowthru.Spark"));
  }

  [Test]
  [Category("SparkSession.AssemblyLoading")]
  public void DataFrame_TypeLoads_OnCurrentFramework()
  {
    var dfType = typeof(DataFrame);

    Assert.That(dfType, Is.Not.Null);
    Assert.That(dfType.AssemblyQualifiedName, Does.Contain("Flowthru.Spark"));
  }

  [Test]
  [Category("SparkSession.AssemblyLoading")]
  public void Builder_HasExpectedFluentApiMethods()
  {
    // Verify the API surface we depend on exists at the reflection level.
    var builderType = typeof(Builder);

    Assert.That(builderType.GetMethod("Master", [typeof(string)]), Is.Not.Null);
    Assert.That(builderType.GetMethod("AppName", [typeof(string)]), Is.Not.Null);
    Assert.That(builderType.GetMethod("GetOrCreate"), Is.Not.Null);
  }

  // =================================================================
  //  Layer 2 — JVM Bridge (requires Java + Spark + Worker)
  //
  //  These tests are ignored by default. Remove the [Ignore] attribute
  //  when running with a full Spark environment configured:
  //    - Java 1.8+
  //    - SPARK_HOME set to a Spark distribution
  //    - DOTNET_WORKER_DIR set to extracted Microsoft.Spark.Worker
  // =================================================================

  [Test]
  [Ignore("Requires JVM + Spark + Microsoft.Spark.Worker runtime environment")]
  [Category("SparkSession.JvmBridge")]
  public void SparkSession_GetOrCreate_EstablishesJvmBridge()
  {
    SparkSession? spark = null;
    try
    {
      spark = SparkSession
        .Builder()
        .AppName("net10-jvm-bridge-test")
        .Master("local[*]")
        .GetOrCreate();

      Assert.That(spark, Is.Not.Null);
    }
    finally
    {
      spark?.Stop();
    }
  }

  [Test]
  [Ignore("Requires JVM + Spark + Microsoft.Spark.Worker runtime environment")]
  [Category("SparkSession.JvmBridge")]
  public void SparkSession_Range_ExecutesTrivialDataFrameOperation()
  {
    SparkSession? spark = null;
    try
    {
      spark = SparkSession
        .Builder()
        .AppName("net10-dataframe-test")
        .Master("local[*]")
        .GetOrCreate();

      var df = spark.Range(10);
      var count = df.Count();

      Assert.That(count, Is.EqualTo(10));
    }
    finally
    {
      spark?.Stop();
    }
  }
}
