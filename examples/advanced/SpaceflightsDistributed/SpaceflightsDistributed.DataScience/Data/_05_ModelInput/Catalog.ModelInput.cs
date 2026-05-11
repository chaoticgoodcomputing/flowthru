using Flowthru.Data.Catalog;
using SpaceflightsDistributed.DataScience.Data._05_ModelInput.Schemas;

namespace SpaceflightsDistributed.DataScience.Data;

public partial class DataScienceCatalog
{
  /// <summary>Training split stored in memory — transient between pipeline runs.</summary>
  public IItem<IEnumerable<TrainingData>> TrainSplit =>
    CreateItem(() => Item.Of<IEnumerable<TrainingData>>("XTrain").Memory().Build());

  /// <summary>Test split stored in memory — transient between pipeline runs.</summary>
  public IItem<IEnumerable<TestData>> TestSplit =>
    CreateItem(() => Item.Of<IEnumerable<TestData>>("XTest").Memory().Build());
}
