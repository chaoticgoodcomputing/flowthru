using Flowthru.Data;
using SpaceflightsDistributed.DataScience.Data._07_ModelOutput.Schemas;

namespace SpaceflightsDistributed.DataScience.Data;

public partial class DataScienceCatalog
{
  public ICatalogEntry<ModelMetrics> ModelMetrics =>
    GetOrCreateEntry(
      () =>
        CatalogEntries.Single.Json<ModelMetrics>(
          label: "ModelMetrics",
          filePath: $"{_basePath}/_07_ModelOutput/Datasets/model_metrics.json"
        )
    );

  public ICatalogEntry<IEnumerable<ModelPredictions>> ModelPredictions =>
    GetOrCreateEntry(
      () =>
        CatalogEntries.Enumerable.Json<ModelPredictions>(
          label: "ModelPredictions",
          filePath: $"{_basePath}/_07_ModelOutput/Datasets/model_predictions.json"
        )
    );
}
