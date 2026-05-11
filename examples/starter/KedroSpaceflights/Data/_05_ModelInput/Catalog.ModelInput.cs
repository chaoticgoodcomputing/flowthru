using Flowthru.Data.Catalog;
using KedroSpaceflights.Data._05_ModelInput.Schemas;

namespace KedroSpaceflights.Data;

/// <summary>
/// Model input data layer: Joined feature tables ("master tables").
/// Contains training and test splits ready for model consumption.
/// </summary>
public partial class Catalog
{
  /// <summary>Training dataset split from the model input table. Transient (memory only).</summary>
  public IItem<IEnumerable<TrainingData>> TrainSplit =>
    CreateItem(() => Item.Of<IEnumerable<TrainingData>>("XTrain")
      .Memory()
      .Build());

  /// <summary>Test dataset split from the model input table. Transient (memory only).</summary>
  public IItem<IEnumerable<TestData>> TestSplit =>
    CreateItem(() => Item.Of<IEnumerable<TestData>>("XTest")
      .Memory()
      .Build());
}
