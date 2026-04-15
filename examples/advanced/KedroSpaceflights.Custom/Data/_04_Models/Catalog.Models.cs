using Flowthru.Core.Data;
using KedroSpaceflights.Custom.Data._04_Models.Schemas;

namespace KedroSpaceflights.Custom.Data;

public partial class Catalog
{
  /// <summary>
  /// Trained ordinary least squares linear regression model.
  /// Contains intercept and coefficients for price prediction.
  /// Stored as JSON to enable cross-pipeline usage (DataEvaluation depends on this).
  /// </summary>
  public IItem<LinearRegressionModel> Regressor =>
    CreateItem(
      () =>
        ItemFactory.Single.Json<LinearRegressionModel>(
          label: "Regressor",
          filePath: $"{_basePath}/_04_Models/Datasets/regressor.json"
        )
    );
}
