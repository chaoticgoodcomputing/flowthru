using Flowthru.Data.Catalog;
using Flowthru.Data.Storage.EFCore;
using Flowthru.Extensions.EFCore.Bulk;
using SpaceflightsStagingSchema.Data._05_ModelInput.Schemas;

namespace SpaceflightsStagingSchema.Data;

public partial class ProductionCatalog
{
  /// <summary>Training dataset with features and labels.</summary>
  public IItem<IEnumerable<TrainingData>> TrainSplit =>
    CreateItem(() => Item.Of<IEnumerable<TrainingData>>("XTrain")
      .EFCoreQuery<TrainingData, ProductionDbContext>()
      .WithContextFactory(_contextFactory)
      .WithSave(BulkSave.Insert<TrainingData, ProductionDbContext>())
      .WithScope(DbScope.Explicit(StagingCatalog.SharedScope))
      .Build());

  /// <summary>Test dataset with features and labels for model evaluation.</summary>
  public IItem<IEnumerable<TestData>> TestSplit =>
    CreateItem(() => Item.Of<IEnumerable<TestData>>("XTest")
      .EFCoreQuery<TestData, ProductionDbContext>()
      .WithContextFactory(_contextFactory)
      .WithSave(BulkSave.Insert<TestData, ProductionDbContext>())
      .WithScope(DbScope.Explicit(StagingCatalog.SharedScope))
      .Build());
}
