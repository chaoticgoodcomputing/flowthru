using Flowthru.Data.Catalog;
using IrisFUnit.Data._05_ModelInput.Schemas;

namespace IrisFUnit.Data;

/// <summary>
/// Model input data layer: Joined feature tables ("master tables").
/// </summary>
public partial class Catalog
{
  public IItem<IEnumerable<FeatureVectorSchema>> TrainX =>
    CreateItem(() => Item.Of<IEnumerable<FeatureVectorSchema>>("TrainX")
      .Json()
      .AtPath($"{_basePath}/_05_ModelInput/Datasets/train_x.json")
      .Build());

  public IItem<IEnumerable<TargetLabelSchema>> TrainY =>
    CreateItem(() => Item.Of<IEnumerable<TargetLabelSchema>>("TrainY")
      .Json()
      .AtPath($"{_basePath}/_05_ModelInput/Datasets/train_y.json")
      .Build());

  public IItem<IEnumerable<FeatureVectorSchema>> TestX =>
    CreateItem(() => Item.Of<IEnumerable<FeatureVectorSchema>>("TestX")
      .Json()
      .AtPath($"{_basePath}/_05_ModelInput/Datasets/test_x.json")
      .Build());

  public IItem<IEnumerable<TargetLabelSchema>> TestY =>
    CreateItem(() => Item.Of<IEnumerable<TargetLabelSchema>>("TestY")
      .Json()
      .AtPath($"{_basePath}/_05_ModelInput/Datasets/test_y.json")
      .Build());
}
