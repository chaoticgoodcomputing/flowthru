using Flowthru.Core.Data;
using KedroSpaceflightsSpark.Data._03_Primary.Schemas;

namespace KedroSpaceflightsSpark.Data;

public partial class Catalog
{
  /// <summary>
  /// Unified model input table. Persisted to Parquet at this layer so the DataScience
  /// flow can consume it as a materialized IEnumerable without requiring Spark.
  /// The TypedFrame produced by CreateModelInputTableStep materializes implicitly
  /// when the Parquet serializer enumerates it.
  /// </summary>
  public IItem<IEnumerable<ModelInputTableSchema>> ModelInputTable =>
    CreateItem(
      () =>
        ItemFactory.Enumerable.Parquet<ModelInputTableSchema>(
          label: "ModelInputTable",
          filePath: $"{_basePath}/_03_Primary/Datasets/model_input_table.parquet"
        )
    );
}
