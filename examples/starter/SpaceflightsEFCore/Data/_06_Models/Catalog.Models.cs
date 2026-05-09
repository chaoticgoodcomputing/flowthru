using Flowthru.Data.Catalog;
using SpaceflightsEFCore.Data._06_Models.Schemas;

namespace SpaceflightsEFCore.Data;

/// <summary>
/// Models data layer: Serialized trained models.
/// </summary>
public partial class Catalog
{
  public IItem<LinearRegressionModel> Regressor =>
    CreateItem(() => Item.Of<LinearRegressionModel>("Regressor")
      .EFCoreEntity<LinearRegressionModel, SpaceflightsDbContext>()
      .WithContextFactory(_contextFactory)
      .Build());
}
