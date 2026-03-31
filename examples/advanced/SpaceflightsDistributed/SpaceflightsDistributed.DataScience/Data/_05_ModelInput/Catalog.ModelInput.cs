using Flowthru.Data;
using SpaceflightsDistributed.DataScience.Data._05_ModelInput.Schemas;

namespace SpaceflightsDistributed.DataScience.Data;

public partial class DataScienceCatalog
{
  /// <summary>Training split stored in memory — transient between pipeline runs.</summary>
  public ICatalogEntry<IEnumerable<TrainingData>> TrainSplit =>
    GetOrCreateEntry(() => CatalogEntries.Enumerable.Memory<TrainingData>(label: "XTrain"));

  /// <summary>Test split stored in memory — transient between pipeline runs.</summary>
  public ICatalogEntry<IEnumerable<TestData>> TestSplit =>
    GetOrCreateEntry(() => CatalogEntries.Enumerable.Memory<TestData>(label: "XTest"));
}
