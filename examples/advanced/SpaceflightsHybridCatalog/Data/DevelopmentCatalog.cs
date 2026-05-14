using Flowthru.Data.Catalog;
using Microsoft.Extensions.Configuration;
using SpaceflightsHybridCatalog.Data._02_Intermediate.Schemas;
using SpaceflightsHybridCatalog.Data._03_Primary.Schemas;
using SpaceflightsHybridCatalog.Data._05_ModelInput.Schemas;
using SpaceflightsHybridCatalog.Data._06_Models.Schemas;
using SpaceflightsHybridCatalog.Data._07_ModelOutput.Schemas;

namespace SpaceflightsHybridCatalog.Data;

/// <summary>
/// Development-mode catalog: every intermediate item is materialised to disk
/// as Parquet / JSON / in-memory. Suited to local iteration where artifacts
/// should be inspectable on the filesystem.
/// </summary>
public sealed class DevelopmentCatalog : Catalog
{
  public DevelopmentCatalog(string basePath, IConfiguration configuration)
    : base(basePath, configuration) { }

  public override IItem<IEnumerable<PreprocessedCompanySchema>> PreprocessedCompanies =>
    CreateItem(() => Item.Of<IEnumerable<PreprocessedCompanySchema>>("PreprocessedCompanies")
      .Parquet()
      .AtPath($"{_basePath}/_02_Intermediate/Datasets/preprocessed_companies.parquet")
      .Build());

  public override IItem<IEnumerable<PreprocessedShuttleSchema>> PreprocessedShuttles =>
    CreateItem(() => Item.Of<IEnumerable<PreprocessedShuttleSchema>>("PreprocessedShuttles")
      .Parquet()
      .AtPath($"{_basePath}/_02_Intermediate/Datasets/preprocessed_shuttles.parquet")
      .Build());

  public override IItem<IEnumerable<ModelInputTableSchema>> ModelInputTable =>
    CreateItem(() => Item.Of<IEnumerable<ModelInputTableSchema>>("ModelInputTable")
      .Parquet()
      .AtPath($"{_basePath}/_03_Primary/Datasets/model_input_table.parquet")
      .Build());

  public override IItem<IEnumerable<TrainingData>> TrainSplit =>
    CreateItem(() => Item.Of<IEnumerable<TrainingData>>("XTrain").Memory().Build());

  public override IItem<IEnumerable<TestData>> TestSplit =>
    CreateItem(() => Item.Of<IEnumerable<TestData>>("XTest").Memory().Build());

  public override IItem<LinearRegressionModel> Regressor =>
    CreateItem(() => Item.Of<LinearRegressionModel>("Regressor")
      .Json()
      .AtPath($"{_basePath}/_06_Models/Datasets/regressor.json")
      .Build());

  public override IItem<ModelMetrics> ModelMetrics =>
    CreateItem(() => Item.Of<ModelMetrics>("ModelMetrics")
      .Json()
      .AtPath($"{_basePath}/_07_ModelOutput/Datasets/model_metrics.json")
      .Build());

  public override IItem<IEnumerable<ModelPredictions>> ModelPredictions =>
    CreateItem(() => Item.Of<IEnumerable<ModelPredictions>>("ModelPredictions")
      .Json()
      .AtPath($"{_basePath}/_07_ModelOutput/Datasets/model_predictions.json")
      .Build());
}
