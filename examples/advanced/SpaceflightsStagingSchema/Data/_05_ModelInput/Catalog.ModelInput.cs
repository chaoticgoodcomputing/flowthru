using Flowthru.Core.Data;
using Flowthru.Extensions.EFCore.Bulk;
using Flowthru.Extensions.EFCore.Data;
using SpaceflightsStagingSchema.Data._05_ModelInput.Schemas;

namespace SpaceflightsStagingSchema.Data;

public partial class ProductionCatalog
{
  /// <summary>Training dataset with features and labels.</summary>
  /// <remarks>
  /// Written by <c>SplitDataStep</c> from a regular IEnumerable; uses
  /// <c>BulkSave.Insert</c> for the bulk insert path. Read shape is the
  /// deferred <see cref="DbQuery{T}"/> so downstream consumers can compose
  /// SQL operations.
  /// </remarks>
  public IItem<IEnumerable<TrainingData>> TrainSplit =>
    CreateItem(
      () =>
        EFCoreItemFactory.Query.EFCore<TrainingData, ProductionDbContext>(
          label: "XTrain",
          contextFactory: _contextFactory,
          saveFunc: BulkSave.Insert<TrainingData, ProductionDbContext>(),
          scope: DbScope.Explicit(StagingCatalog.SharedScope)
        )
    );

  /// <summary>Test dataset with features and labels for model evaluation.</summary>
  public IItem<IEnumerable<TestData>> TestSplit =>
    CreateItem(
      () =>
        EFCoreItemFactory.Query.EFCore<TestData, ProductionDbContext>(
          label: "XTest",
          contextFactory: _contextFactory,
          saveFunc: BulkSave.Insert<TestData, ProductionDbContext>(),
          scope: DbScope.Explicit(StagingCatalog.SharedScope)
        )
    );
}
