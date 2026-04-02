using Flowthru.Data;
using SpaceflightsDistributed.DataScience.Data._07_ModelOutput.Schemas;

namespace SpaceflightsDistributed.DataScience.Data;

public partial class DataScienceCatalog
{
  public IItem<ModelMetrics> ModelMetrics =>
    CreateItem(
      () =>
        Items.Single.Json<ModelMetrics>(
          label: "ModelMetrics",
          filePath: $"{_basePath}/_07_ModelOutput/Datasets/model_metrics.json"
        )
    );

  public IItem<IEnumerable<ModelPredictions>> ModelPredictions =>
    CreateItem(
      () =>
        Items.Enumerable.Json<ModelPredictions>(
          label: "ModelPredictions",
          filePath: $"{_basePath}/_07_ModelOutput/Datasets/model_predictions.json"
        )
    );
}
