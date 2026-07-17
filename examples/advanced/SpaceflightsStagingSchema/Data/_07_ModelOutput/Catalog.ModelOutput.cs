using Flowthru.Data.Catalog;
using Flowthru.Data.Storage.EFCore;
using Flowthru.Extensions.EFCore.Bulk;
using SpaceflightsStagingSchema.Data._07_ModelOutput.Schemas;

namespace SpaceflightsStagingSchema.Data;

public partial class ProductionCatalog
{
  /// <summary>Evaluation metrics for the trained model. Single row.</summary>
  public IItem<ModelMetrics> ModelMetrics =>
    CreateItem(() => Item.Of<ModelMetrics>("ModelMetrics")
      .EFCoreEntity<ModelMetrics, ProductionDbContext>()
      .WithContextFactory(_contextFactory)
      .Build());

  /// <summary>
  /// Per-row model predictions on the test set. Bulk-inserted via
  /// <c>BulkSave.Insert</c>; consumed by Reporting as a deferred query handle.
  /// </summary>
  #region docs:item-efcore-bulk
  public IItem<IEnumerable<ModelPredictions>> ModelPredictions =>
    CreateItem(() => Item.Of<IEnumerable<ModelPredictions>>("ModelPredictions")
      .EFCoreQuery<ModelPredictions, ProductionDbContext>()
      .WithContextFactory(_contextFactory)
      .WithSave(BulkSave.Insert<ModelPredictions, ProductionDbContext>())
      .WithScope(DbScope.Explicit(StagingCatalog.SharedScope))
      .Build());
  #endregion
}
