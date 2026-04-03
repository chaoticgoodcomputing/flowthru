using Flowthru.Data;
using SpaceflightsDistributed.DataProcessing.Data._03_Primary.Schemas;

namespace SpaceflightsDistributed.DataProcessing.Data;

public partial class DataProcessingCatalog
{
  public IItem<IEnumerable<ModelInputTableSchema>> ModelInputTable =>
    CreateItem(
      () =>
        ItemFactory.Enumerable.Parquet<ModelInputTableSchema>(
          label: "ModelInputTable",
          filePath: $"{_basePath}/_03_Primary/Datasets/model_input_table.parquet"
        )
    );
}
