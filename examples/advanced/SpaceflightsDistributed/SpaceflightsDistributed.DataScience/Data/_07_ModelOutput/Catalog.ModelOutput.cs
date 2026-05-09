using Flowthru.Data.Catalog;
using SpaceflightsDistributed.DataScience.Data._07_ModelOutput.Schemas;

namespace SpaceflightsDistributed.DataScience.Data;

public partial class DataScienceCatalog
{
  public IItem<ModelMetrics> ModelMetrics =>
    CreateItem(() => Item.Of<ModelMetrics>("ModelMetrics")
      .Json()
      .AtPath($"{_basePath}/_07_ModelOutput/Datasets/model_metrics.json")
      .Build());

  public IItem<IEnumerable<ModelPredictions>> ModelPredictions =>
    CreateItem(() => Item.Of<IEnumerable<ModelPredictions>>("ModelPredictions")
      .Json()
      .AtPath($"{_basePath}/_07_ModelOutput/Datasets/model_predictions.json")
      .Build());
}
