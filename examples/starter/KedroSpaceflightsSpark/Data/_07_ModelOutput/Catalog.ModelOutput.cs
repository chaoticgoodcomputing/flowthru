using Flowthru.Core.Data;
using KedroSpaceflightsSpark.Data._07_ModelOutput.Schemas;

namespace KedroSpaceflightsSpark.Data;

public partial class Catalog
{
  public IItem<ModelMetrics> ModelMetrics =>
    CreateItem(
      () =>
        ItemFactory.Single.Json<ModelMetrics>(
          label: "ModelMetrics",
          filePath: $"{_basePath}/_07_ModelOutput/Datasets/model_metrics.json"
        )
    );

  public IItem<IEnumerable<ModelPredictions>> ModelPredictions =>
    CreateItem(
      () =>
        ItemFactory.Enumerable.Json<ModelPredictions>(
          label: "ModelPredictions",
          filePath: $"{_basePath}/_07_ModelOutput/Datasets/model_predictions.json"
        )
    );
}
