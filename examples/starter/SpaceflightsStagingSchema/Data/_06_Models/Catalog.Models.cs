using Flowthru.Core.Data;
using Flowthru.Extensions.EFCore.Data;
using SpaceflightsStagingSchema.Data._06_Models.Schemas;

namespace SpaceflightsStagingSchema.Data;

public partial class ProductionCatalog
{
  /// <summary>Trained linear regression model for price prediction.</summary>
  public IItem<LinearRegressionModel> Regressor =>
    CreateItem(
      () =>
        EFCoreItemFactory.Single.EFCore<LinearRegressionModel, ProductionDbContext>(
          label: "Regressor",
          contextFactory: _contextFactory
        )
    );
}
