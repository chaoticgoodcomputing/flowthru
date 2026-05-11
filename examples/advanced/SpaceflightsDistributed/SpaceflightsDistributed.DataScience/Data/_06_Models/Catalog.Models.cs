using Flowthru.Data.Catalog;
using SpaceflightsDistributed.DataScience.Data._06_Models.Schemas;

namespace SpaceflightsDistributed.DataScience.Data;

public partial class DataScienceCatalog
{
  public IItem<LinearRegressionModel> Regressor =>
    CreateItem(() => Item.Of<LinearRegressionModel>("Regressor")
      .Json()
      .AtPath($"{_basePath}/_06_Models/Datasets/regressor.json")
      .Build());
}
