using Flowthru.Data;
using SpaceflightsDistributed.DataScience.Data._06_Models.Schemas;

namespace SpaceflightsDistributed.DataScience.Data;

public partial class DataScienceCatalog
{
  public IItem<LinearRegressionModel> Regressor =>
    CreateItem(
      () =>
        Items.Single.Json<LinearRegressionModel>(
          label: "Regressor",
          filePath: $"{_basePath}/_06_Models/Datasets/regressor.json"
        )
    );
}
