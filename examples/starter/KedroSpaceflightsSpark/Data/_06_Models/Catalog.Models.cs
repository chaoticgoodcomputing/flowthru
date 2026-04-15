using Flowthru.Core.Data;
using KedroSpaceflightsSpark.Data._06_Models.Schemas;

namespace KedroSpaceflightsSpark.Data;

public partial class Catalog
{
    public IItem<LinearRegressionModel> Regressor =>
      CreateItem(
        () =>
          ItemFactory.Single.Json<LinearRegressionModel>(
            label: "Regressor",
            filePath: $"{_basePath}/_06_Models/Datasets/regressor.json"
          )
      );
}
