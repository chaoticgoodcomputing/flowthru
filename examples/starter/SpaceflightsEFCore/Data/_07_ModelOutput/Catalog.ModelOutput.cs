using Flowthru.Data.Catalog;
using SpaceflightsEFCore.Data._07_ModelOutput.Schemas;

namespace SpaceflightsEFCore.Data;

/// <summary>
/// Model output data layer: Model predictions and scores.
/// </summary>
public partial class Catalog
{
  public IItem<ModelMetrics> ModelMetrics =>
    CreateItem(() => Item.Of<ModelMetrics>("ModelMetrics")
      .EFCoreEntity<ModelMetrics, SpaceflightsDbContext>()
      .WithContextFactory(_contextFactory)
      .Build());

  public IItem<IEnumerable<ModelPredictions>> ModelPredictions =>
    CreateItem(() => Item.Of<IEnumerable<ModelPredictions>>("ModelPredictions")
      .EFCoreTable<ModelPredictions, SpaceflightsDbContext>()
      .WithContextFactory(_contextFactory)
      .Build());
}
