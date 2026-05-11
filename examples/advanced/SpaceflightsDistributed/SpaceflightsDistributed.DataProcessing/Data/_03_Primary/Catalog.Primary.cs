using Flowthru.Data.Catalog;
using SpaceflightsDistributed.DataProcessing.Data._03_Primary.Schemas;

namespace SpaceflightsDistributed.DataProcessing.Data;

public partial class DataProcessingCatalog
{
  public IItem<IEnumerable<ModelInputTableSchema>> ModelInputTable =>
    CreateItem(() => Item.Of<IEnumerable<ModelInputTableSchema>>("ModelInputTable")
      .Parquet()
      .AtPath($"{_basePath}/_03_Primary/Datasets/model_input_table.parquet")
      .Build());
}
