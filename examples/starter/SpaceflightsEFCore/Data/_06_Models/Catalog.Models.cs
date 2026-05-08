using Flowthru.Data.Catalog;
using Flowthru.Data.Storage.EFCore;
using SpaceflightsEFCore.Data._06_Models.Schemas;

namespace SpaceflightsEFCore.Data;

/// <summary>
/// Models data layer: Serialized trained models.
/// Contains persisted model artifacts for reproducibility and deployment.
/// </summary>
public partial class Catalog
{
  /// <summary>
  /// Trained linear regression model for price prediction.
  /// </summary>
  public IItem<LinearRegressionModel> Regressor =>
    CreateItem(
      () =>
        ItemFactory.Singleton.EFCore<LinearRegressionModel, SpaceflightsDbContext>(
          label: "Regressor",
          contextFactory: _contextFactory
        )
    );
}
