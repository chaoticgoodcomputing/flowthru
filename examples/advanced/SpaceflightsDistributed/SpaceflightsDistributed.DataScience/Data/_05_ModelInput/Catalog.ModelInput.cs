using Flowthru.Core.Data;
using SpaceflightsDistributed.DataScience.Data._05_ModelInput.Schemas;

namespace SpaceflightsDistributed.DataScience.Data;

public partial class DataScienceCatalog
{
    /// <summary>Training split stored in memory — transient between pipeline runs.</summary>
    public IItem<IEnumerable<TrainingData>> TrainSplit =>
      CreateItem(() => ItemFactory.Enumerable.Memory<TrainingData>(label: "XTrain"));

    /// <summary>Test split stored in memory — transient between pipeline runs.</summary>
    public IItem<IEnumerable<TestData>> TestSplit =>
      CreateItem(() => ItemFactory.Enumerable.Memory<TestData>(label: "XTest"));
}
