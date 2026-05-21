using Flowthru.Data.Catalog;
using IrisPython.Data._05_ModelInput.Schemas;

namespace IrisPython.Data;

/// <summary>
/// Model input data layer: Joined feature tables ("master tables").
/// </summary>
public partial class Catalog
{
  public IItem<IEnumerable<FeatureVectorSchema>> TrainX =>
    CreateItem(() => Item.Of<IEnumerable<FeatureVectorSchema>>("TrainX")
      .Parquet()
      .AtPath($"{_basePath}/_05_ModelInput/Datasets/train_x.parquet")
      .Build());

  public IItem<IEnumerable<TargetLabelSchema>> TrainY =>
    CreateItem(() => Item.Of<IEnumerable<TargetLabelSchema>>("TrainY")
      .Parquet()
      .AtPath($"{_basePath}/_05_ModelInput/Datasets/train_y.parquet")
      .Build());

  public IItem<IEnumerable<FeatureVectorSchema>> TestX =>
    CreateItem(() => Item.Of<IEnumerable<FeatureVectorSchema>>("TestX")
      .Parquet()
      .AtPath($"{_basePath}/_05_ModelInput/Datasets/test_x.parquet")
      .Build());

  public IItem<IEnumerable<TargetLabelSchema>> TestY =>
    CreateItem(() => Item.Of<IEnumerable<TargetLabelSchema>>("TestY")
      .Parquet()
      .AtPath($"{_basePath}/_05_ModelInput/Datasets/test_y.parquet")
      .Build());
}
