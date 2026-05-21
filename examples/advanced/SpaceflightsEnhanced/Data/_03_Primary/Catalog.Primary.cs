using Flowthru.Data.Catalog;
using SpaceflightsEnhanced.Data._03_Primary.Schemas;

namespace SpaceflightsEnhanced.Data;

public partial class Catalog
{
  /// <summary>Model input table in Parquet format.</summary>
  public IItem<IEnumerable<ModelInputSchema>> ModelInputTable =>
    CreateItem(() => Item.Of<IEnumerable<ModelInputSchema>>("ModelInputTable")
      .Parquet()
      .AtPath($"{_basePath}/_03_Primary/Datasets/model_input_table.parquet")
      .Build());

  /// <summary>Model input table exported as minified JSON.</summary>
  public IItem<IEnumerable<ModelInputSchema>> ModelInputTableJsonMinified =>
    CreateItem(() => Item.Of<IEnumerable<ModelInputSchema>>("ModelInputTableJsonMinified")
      .Json()
      .AtPath($"{_basePath}/_03_Primary/Datasets/model_input_table.min.json")
      .Build());

  /// <summary>Model input table exported as CSV (for debugging).</summary>
  public IItem<IEnumerable<ModelInputSchema>> ModelInputTableCsv =>
    CreateItem(() => Item.Of<IEnumerable<ModelInputSchema>>("ModelInputTableCsv")
      .Csv()
      .AtPath($"{_basePath}/_03_Primary/Datasets/model_input_table.csv")
      .Build());

  /// <summary>Training features (X_train). Stored in memory.</summary>
  public IItem<IEnumerable<FeatureRow>> XTrain =>
    CreateItem(() => Item.Of<IEnumerable<FeatureRow>>("XTrain")
      .Memory()
      .Build());

  /// <summary>Testing features (X_test). Stored as Parquet for cross-pipeline usage.</summary>
  public IItem<IEnumerable<FeatureRow>> XTest =>
    CreateItem(() => Item.Of<IEnumerable<FeatureRow>>("XTest")
      .Parquet()
      .AtPath($"{_basePath}/_03_Primary/Datasets/x_test.parquet")
      .Build());

  /// <summary>Training targets (y_train). Stored in memory.</summary>
  public IItem<IEnumerable<TargetValue>> YTrain =>
    CreateItem(() => Item.Of<IEnumerable<TargetValue>>("YTrain")
      .Memory()
      .Build());

  /// <summary>Testing targets (y_test). Stored as Parquet for cross-pipeline usage.</summary>
  public IItem<IEnumerable<TargetValue>> YTest =>
    CreateItem(() => Item.Of<IEnumerable<TargetValue>>("YTest")
      .Parquet()
      .AtPath($"{_basePath}/_03_Primary/Datasets/y_test.parquet")
      .Build());
}
