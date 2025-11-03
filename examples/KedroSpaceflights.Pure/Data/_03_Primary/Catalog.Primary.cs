using Flowthru.Data;
using KedroSpaceflights.Pure.Data._03_Primary.Schemas;

namespace KedroSpaceflights.Pure.Data;

public partial class Catalog
{
  public ICatalogEntry<IEnumerable<ModelInputTableSchema>> ModelInputTable =>
    GetOrCreateEntry(
      () =>
        CatalogEntries.Enumerable.Parquet<ModelInputTableSchema>(
          label: "ModelInputTable",
          filePath: $"{_basePath}/_03_Primary/Datasets/model_input_table.parquet"
        )
    );

  // Transient entries (memory only)
  public ICatalogEntry<IEnumerable<TrainingData>> XTrain =>
    GetOrCreateEntry(() => CatalogEntries.Enumerable.Memory<TrainingData>(label: "XTrain"));

  public ICatalogEntry<IEnumerable<TestData>> XTest =>
    GetOrCreateEntry(() => CatalogEntries.Enumerable.Memory<TestData>(label: "XTest"));
}
