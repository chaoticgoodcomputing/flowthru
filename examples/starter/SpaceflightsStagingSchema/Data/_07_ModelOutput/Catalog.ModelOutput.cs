using Flowthru.Core.Data;
using Flowthru.Extensions.EFCore.Data;
using SpaceflightsStagingSchema.Data._07_ModelOutput.Schemas;

namespace SpaceflightsStagingSchema.Data;

public partial class ProductionCatalog
{
  /// <summary>Evaluation metrics for the trained model.</summary>
  public IItem<ModelMetrics> ModelMetrics =>
    CreateItem(
      () =>
        EFCoreItemFactory.Single.EFCore<ModelMetrics, ProductionDbContext>(
          label: "ModelMetrics",
          contextFactory: _contextFactory
        )
    );

  /// <summary>Per-row model predictions on the test set.</summary>
  public IItem<IEnumerable<ModelPredictions>> ModelPredictions =>
    CreateItem(
      () =>
        EFCoreItemFactory.Enumerable.EFCore<ModelPredictions, ProductionDbContext>(
          label: "ModelPredictions",
          contextFactory: _contextFactory
        )
    );
}
