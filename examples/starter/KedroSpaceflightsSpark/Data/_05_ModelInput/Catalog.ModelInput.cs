using Flowthru.Core.Data;
using KedroSpaceflightsSpark.Data._05_ModelInput.Schemas;

namespace KedroSpaceflightsSpark.Data;

public partial class Catalog
{
  public IItem<IEnumerable<TrainingData>> TrainSplit =>
    CreateItem(() => ItemFactory.Enumerable.Memory<TrainingData>(label: "XTrain"));

  public IItem<IEnumerable<TestData>> TestSplit =>
    CreateItem(() => ItemFactory.Enumerable.Memory<TestData>(label: "XTest"));
}
