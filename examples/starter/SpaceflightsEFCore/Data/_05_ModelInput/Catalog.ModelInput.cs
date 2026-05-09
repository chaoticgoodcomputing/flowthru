using Flowthru.Data.Catalog;
using SpaceflightsEFCore.Data._05_ModelInput.Schemas;

namespace SpaceflightsEFCore.Data;

/// <summary>
/// Model input data layer: Joined feature tables ("master tables").
/// </summary>
public partial class Catalog
{
  public IItem<IEnumerable<TrainingData>> TrainSplit =>
    CreateItem(() => Item.Of<IEnumerable<TrainingData>>("XTrain")
      .EFCoreTable<TrainingData, SpaceflightsDbContext>()
      .WithContextFactory(_contextFactory)
      .Build());

  public IItem<IEnumerable<TestData>> TestSplit =>
    CreateItem(() => Item.Of<IEnumerable<TestData>>("XTest")
      .EFCoreTable<TestData, SpaceflightsDbContext>()
      .WithContextFactory(_contextFactory)
      .Build());
}
