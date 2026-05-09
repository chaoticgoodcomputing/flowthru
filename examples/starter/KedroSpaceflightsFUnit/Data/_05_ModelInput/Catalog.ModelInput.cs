using Flowthru.Data.Catalog;
using KedroSpaceflightsFUnit.Data._05_ModelInput.Schemas;

namespace KedroSpaceflightsFUnit.Data;

/// <summary>
/// Model input data layer: Joined feature tables ("master tables").
/// </summary>
public partial class Catalog
{
  public IItem<IEnumerable<TrainingData>> TrainSplit =>
    CreateItem(() => Item.Of<IEnumerable<TrainingData>>("XTrain").Memory().Build());

  public IItem<IEnumerable<TestData>> TestSplit =>
    CreateItem(() => Item.Of<IEnumerable<TestData>>("XTest").Memory().Build());
}
