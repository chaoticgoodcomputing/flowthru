using Flowthru.Data;
using Flowthru.Extensions.EFCore.Data;
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
  public ICatalogEntry<LinearRegressionModel> Regressor =>
    GetOrCreateEntry(
      () =>
        EFCoreCatalogEntries.Single.EFCore<LinearRegressionModel>(
          label: "Regressor",
          context: _dbContext
        )
    );
}
