using Flowthru.Core.Data;
using KedroSpaceflightsGQL.Data._05_ModelInput.Schemas;

namespace KedroSpaceflightsGQL.Data;

/// <summary>
/// Model input data layer: Joined feature tables ("master tables").
/// Contains training and test splits ready for model consumption.
/// </summary>
public partial class Catalog
{
  /// <summary>
  /// Training dataset split from the model input table. Transient (memory only).
  /// </summary>
  public IItem<IEnumerable<TrainingData>> TrainSplit =>
    CreateItem(() => ItemFactory.Enumerable.Memory<TrainingData>(label: "XTrain"));

  /// <summary>
  /// Test dataset split from the model input table. Transient (memory only).
  /// </summary>
  public IItem<IEnumerable<TestData>> TestSplit =>
    CreateItem(() => ItemFactory.Enumerable.Memory<TestData>(label: "XTest"));
}
