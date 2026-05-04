using Flowthru.Core.Data;
using Flowthru.Extensions.EFCore.Data;
using SpaceflightsStagingSchema.Data._05_ModelInput.Schemas;

namespace SpaceflightsStagingSchema.Data;

public partial class ProductionCatalog
{
  /// <summary>Training dataset with features and labels.</summary>
  public IItem<IEnumerable<TrainingData>> TrainSplit =>
    CreateItem(
      () =>
        EFCoreItemFactory.Enumerable.EFCore<TrainingData, ProductionDbContext>(
          label: "XTrain",
          contextFactory: _contextFactory
        )
    );

  /// <summary>Test dataset with features and labels for model evaluation.</summary>
  public IItem<IEnumerable<TestData>> TestSplit =>
    CreateItem(
      () =>
        EFCoreItemFactory.Enumerable.EFCore<TestData, ProductionDbContext>(
          label: "XTest",
          contextFactory: _contextFactory
        )
    );
}
