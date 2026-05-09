using Flowthru.Data.Catalog;
using KedroSpaceflightsFUnit.Data._03_Primary.Schemas;

namespace KedroSpaceflightsFUnit.Data;

/// <summary>
/// Primary data layer: Domain model data.
/// </summary>
public partial class Catalog
{
  public IItem<IEnumerable<ModelInputTableSchema>> ModelInputTable =>
    CreateItem(() => Item.Of<IEnumerable<ModelInputTableSchema>>("ModelInputTable")
      .Parquet()
      .AtPath($"{_basePath}/_03_Primary/Datasets/model_input_table.parquet")
      .Build());
}
