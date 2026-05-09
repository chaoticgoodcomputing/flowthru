using Flowthru.Data.Catalog;
using SpaceflightsStagingSchema.Data._06_Models.Schemas;

namespace SpaceflightsStagingSchema.Data;

public partial class ProductionCatalog
{
  /// <summary>
  /// Trained linear regression model for price prediction. Single-row, default
  /// save (no bulk needed for one row).
  /// </summary>
  public IItem<LinearRegressionModel> Regressor =>
    CreateItem(() => Item.Of<LinearRegressionModel>("Regressor")
      .EFCoreEntity<LinearRegressionModel, ProductionDbContext>()
      .WithContextFactory(_contextFactory)
      .Build());
}
