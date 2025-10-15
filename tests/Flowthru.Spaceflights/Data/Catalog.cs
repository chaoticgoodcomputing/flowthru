using Flowthru.Data;
using Flowthru.Spaceflights.Data.Schemas.Raw;
using Flowthru.Spaceflights.Data.Schemas.Processed;
using Flowthru.Spaceflights.Data.Schemas.Models;
using Microsoft.ML;

namespace Flowthru.Spaceflights.Data;

/// <summary>
/// Configures the data catalog for the Spaceflights project.
/// Registers all datasets with their schemas, formats, and file paths.
/// </summary>
public static class CatalogConfiguration
{
  /// <summary>
  /// Builds and returns the configured data catalog.
  /// Follows Kedro's data layering convention:
  /// - 01_Raw: Raw input data
  /// - 02_Intermediate: Preprocessed data
  /// - 03_Primary: Primary model inputs
  /// - 06_Models: Trained models
  /// - 08_Reporting: Reports and metrics
  /// </summary>
  public static DataCatalog BuildCatalog(string basePath = "Data/Datasets")
  {
    return DataCatalogBuilder.BuildCatalog(catalog =>
    {
      // ═══════════════════════════════════════════════════════════
      // RAW DATA (01_Raw)
      // ═══════════════════════════════════════════════════════════

      catalog.Register("companies", new CsvDataset<CompanyRawSchema>
      {
        FilePath = $"{basePath}/01_Raw/companies.csv",
        LoadOptions = new CsvLoadOptions
        {
          HasHeaderRecord = true
        }
      });

      catalog.Register("reviews", new CsvDataset<ReviewRawSchema>
      {
        FilePath = $"{basePath}/01_Raw/reviews.csv",
        LoadOptions = new CsvLoadOptions
        {
          HasHeaderRecord = true
        }
      });

      catalog.Register("shuttles", new ExcelDataset<ShuttleRawSchema>
      {
        FilePath = $"{basePath}/01_Raw/shuttles.xlsx",
        LoadOptions = new ExcelLoadOptions
        {
          SheetName = "Sheet1"
        }
      });

      // ═══════════════════════════════════════════════════════════
      // INTERMEDIATE DATA (02_Intermediate)
      // ═══════════════════════════════════════════════════════════

      catalog.Register("preprocessed_companies", new ParquetDataset<CompanySchema>
      {
        FilePath = $"{basePath}/02_Intermediate/preprocessed_companies.parquet"
      });

      catalog.Register("preprocessed_shuttles", new ParquetDataset<ShuttleSchema>
      {
        FilePath = $"{basePath}/02_Intermediate/preprocessed_shuttles.parquet"
      });

      // ═══════════════════════════════════════════════════════════
      // PRIMARY DATA (03_Primary)
      // ═══════════════════════════════════════════════════════════

      catalog.Register("model_input_table", new ParquetDataset<ModelInputSchema>
      {
        FilePath = $"{basePath}/03_Primary/model_input_table.parquet"
      });

      // ═══════════════════════════════════════════════════════════
      // MODEL DATA (Split results - in memory)
      // ═══════════════════════════════════════════════════════════

      catalog.Register("train_test_split", new MemoryDataset<TrainTestSplit>());

      // ═══════════════════════════════════════════════════════════
      // MODELS (06_Models)
      // ═══════════════════════════════════════════════════════════

      catalog.Register("regressor", new MemoryDataset<ITransformer>());
      // In production, would use:
      // catalog.Register("regressor", new PickleDataset<ITransformer>
      // {
      //     FilePath = $"{basePath}/06_Models/regressor.zip",
      //     Versioned = true
      // });

      // ═══════════════════════════════════════════════════════════
      // REPORTING (08_Reporting)
      // ═══════════════════════════════════════════════════════════

      catalog.Register("model_metrics", new MemoryDataset<ModelMetrics>());
      // In production, could use:
      // catalog.Register("model_metrics", new JsonDataset<ModelMetrics>
      // {
      //     FilePath = $"{basePath}/08_Reporting/model_metrics.json"
      // });
    });
  }
}
