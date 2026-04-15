using Flowthru.Core.Data;
using KedroIrisFUnit.Data._06_Models.Schemas;

namespace KedroIrisFUnit.Data;

/// <summary>
/// Models data layer: Serialized trained models.
/// </summary>
public partial class Catalog
{
  /// <summary>
  /// Trained multi-class logistic regression model.
  /// Contains weight matrix for all three species classifiers.
  /// </summary>
  public IItem<ModelWeightsSchema> IrisModel =>
    CreateItem(
      () =>
        ItemFactory.Single.Json<ModelWeightsSchema>(
          label: "IrisModel",
          filePath: $"{_basePath}/_06_Models/Datasets/iris_model.json"
        )
    );
}
