using Flowthru.Data;
using RetailData.Data._04_Metadata.Schemas;

namespace RetailData.Data;

public partial class Catalog
{
  /// <summary>
  /// Metadata about the processed dataset
  /// </summary>
  public ICatalogEntry<DatasetMetadata> DatasetMetadata =>
    GetOrCreateEntry(
      () =>
        CatalogEntries.Single.Json<DatasetMetadata>(
          label: "DatasetMetadata",
          filePath: $"{_basePath}/_04_Metadata/Datasets/dataset_metadata.json"
        )
    );
}
