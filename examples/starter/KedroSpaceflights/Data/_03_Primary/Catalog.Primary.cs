using Flowthru.Data;
using KedroSpaceflights.Data._03_Primary.Schemas;

namespace KedroSpaceflights.Data;

/// <summary>
/// Primary data layer: Domain model data.
/// Contains datasets structured according to the problem being solved, not the source system structure.
/// </summary>
public partial class Catalog
{
  /// <summary>
  /// Unified model input table combining shuttle, company, and review data.
  /// </summary>
  public ICatalogEntry<IEnumerable<ModelInputTableSchema>> ModelInputTable =>
    GetOrCreateEntry(
      () =>
        CatalogEntries.Enumerable.Parquet<ModelInputTableSchema>(
          label: "ModelInputTable",
          filePath: $"{_basePath}/_03_Primary/Datasets/model_input_table.parquet"
        )
    );
}
