using Flowthru.Core.Data;
using KedroSpaceflightsCustom.Data._03_Primary.Schemas;
using KedroSpaceflightsCustom.Flows.DataScience.Steps;

namespace KedroSpaceflightsCustom.Data;

public partial class Catalog
{
  /// <summary>
  /// Model input table in Parquet format.
  /// Joined dataset ready for ML training.
  /// </summary>
  public IItem<IEnumerable<ModelInputSchema>> ModelInputTable =>
    CreateItem(
      () =>
        ItemFactory.Enumerable.Parquet<ModelInputSchema>(
          label: "ModelInputTable",
          filePath: $"{_basePath}/_03_Primary/Datasets/model_input_table.parquet"
        )
    );

  /// <summary>
  /// Model input table exported as minified JSON (compact, production-ready format).
  /// </summary>
  public IItem<IEnumerable<ModelInputSchema>> ModelInputTableJsonMinified =>
    CreateItem(
      () =>
        ItemFactory.Enumerable.Json<ModelInputSchema>(
          label: "ModelInputTableJsonMinified",
          filePath: $"{_basePath}/_03_Primary/Datasets/model_input_table.min.json"
        )
    );

  /// <summary>
  /// Model input table exported as CSV (for debugging).
  /// </summary>
  public IItem<IEnumerable<ModelInputSchema>> ModelInputTableCsv =>
    CreateItem(
      () =>
        ItemFactory.Enumerable.Csv<ModelInputSchema>(
          label: "ModelInputTableCsv",
          filePath: $"{_basePath}/_03_Primary/Datasets/model_input_table.csv"
        )
    );

  /// <summary>
  /// Training features (X_train).
  /// Feature vectors for model training.
  /// Stored in memory as it's only used within the DataScience pipeline.
  /// </summary>
  public IItem<IEnumerable<FeatureRow>> XTrain =>
    CreateItem(() => ItemFactory.Enumerable.Memory<FeatureRow>(label: "XTrain"));

  /// <summary>
  /// Testing features (X_test).
  /// Feature vectors for model evaluation.
  /// Stored as Parquet to enable cross-pipeline usage (DataEvaluation depends on this).
  /// </summary>
  public IItem<IEnumerable<FeatureRow>> XTest =>
    CreateItem(
      () =>
        ItemFactory.Enumerable.Parquet<FeatureRow>(
          label: "XTest",
          filePath: $"{_basePath}/_03_Primary/Datasets/x_test.parquet"
        )
    );

  /// <summary>
  /// Training targets (y_train).
  /// Target prices for model training.
  /// Stored in memory as it's only used within the DataScience pipeline.
  /// </summary>
  public IItem<IEnumerable<TargetValue>> YTrain =>
    CreateItem(() => ItemFactory.Enumerable.Memory<TargetValue>(label: "YTrain"));

  /// <summary>
  /// Testing targets (y_test).
  /// Target prices for model evaluation.
  /// Stored as Parquet to enable cross-pipeline usage (DataEvaluation depends on this).
  /// </summary>
  public IItem<IEnumerable<TargetValue>> YTest =>
    CreateItem(
      () =>
        ItemFactory.Enumerable.Parquet<TargetValue>(
          label: "YTest",
          filePath: $"{_basePath}/_03_Primary/Datasets/y_test.parquet"
        )
    );
}
