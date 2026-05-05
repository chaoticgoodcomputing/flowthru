using Flowthru.Core.Data;
using Flowthru.Extensions.EFCore.Bulk;
using Flowthru.Extensions.EFCore.Data;
using SpaceflightsStagingSchema.Data._07_ModelOutput.Schemas;

namespace SpaceflightsStagingSchema.Data;

public partial class ProductionCatalog
{
  /// <summary>Evaluation metrics for the trained model. Single row.</summary>
  public IItem<ModelMetrics> ModelMetrics =>
    CreateItem(
      () =>
        EFCoreItemFactory.Single.EFCore<ModelMetrics, ProductionDbContext>(
          label: "ModelMetrics",
          contextFactory: _contextFactory
        )
    );

  /// <summary>
  /// Per-row model predictions on the test set. Bulk-inserted via
  /// <c>BulkSave.Insert</c>; consumed by Reporting as a deferred
  /// <see cref="DbQuery{T}"/> so confusion-matrix binning can run server-side
  /// if the step body chooses.
  /// </summary>
  public IItem<IEnumerable<ModelPredictions>> ModelPredictions =>
    CreateItem(
      () =>
        EFCoreItemFactory.Query.EFCore<ModelPredictions, ProductionDbContext>(
          label: "ModelPredictions",
          contextFactory: _contextFactory,
          saveFunc: BulkSave.Insert<ModelPredictions, ProductionDbContext>(),
          scope: DbScope.Explicit(StagingCatalog.SharedScope)
        )
    );
}
