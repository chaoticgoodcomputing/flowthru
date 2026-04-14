using Flowthru.Core.Data;
using Flowthru.Extensions.Spark;
using Flowthru.Extensions.Spark.Runtime;
using Flowthru.Spark.Sql;

namespace KedroSpaceflightsSpark.Data;

public partial class Catalog : CatalogAbstract
{
  private readonly string _basePath;

  /// <summary>
  /// The Spark frame provider used by preprocessing steps to wrap native DataFrames
  /// into typed frames.
  /// </summary>
  public SparkFrameProvider Provider { get; }

  /// <summary>
  /// The active Spark session. Created once after the JVM backend is initialized.
  /// </summary>
  public SparkSession Session { get; }

  public Catalog(string basePath, SparkFrameProvider provider, SparkRuntime runtime)
  {
    _basePath = basePath;
    Provider = provider;
    runtime.Initialize();
    Session = SparkSession.Builder().GetOrCreate();
    InitializeCatalogProperties();
  }
}
