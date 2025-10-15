using Flowthru.Data;
using Flowthru.Data.Implementations;
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

      catalog.Register("companies",
        new CsvCatalogEntry<CompanyRawSchema>("companies", $"{basePath}/01_Raw/companies.csv"));

      catalog.Register("reviews",
        new CsvCatalogEntry<ReviewRawSchema>("reviews", $"{basePath}/01_Raw/reviews.csv"));

      catalog.Register("shuttles",
        new ExcelCatalogEntry<ShuttleRawSchema>("shuttles", $"{basePath}/01_Raw/shuttles.xlsx", "Sheet1"));

      // ═══════════════════════════════════════════════════════════
      // INTERMEDIATE DATA (02_Intermediate)
      // ═══════════════════════════════════════════════════════════

      catalog.Register("preprocessed_companies",
        new ParquetCatalogEntry<CompanySchema>("preprocessed_companies",
          $"{basePath}/02_Intermediate/preprocessed_companies.parquet"));

      catalog.Register("preprocessed_shuttles",
        new ParquetCatalogEntry<ShuttleSchema>("preprocessed_shuttles",
          $"{basePath}/02_Intermediate/preprocessed_shuttles.parquet"));

      // ═══════════════════════════════════════════════════════════
      // PRIMARY DATA (03_Primary)
      // ═══════════════════════════════════════════════════════════

      catalog.Register("model_input_table",
        new ParquetCatalogEntry<ModelInputSchema>("model_input_table",
          $"{basePath}/03_Primary/model_input_table.parquet"));

      // ═══════════════════════════════════════════════════════════
      // MODEL DATA (Split results - in memory)
      // Multi-output from SplitDataNode mapped via CatalogMap<SplitDataOutputs>
      // ═══════════════════════════════════════════════════════════

      catalog.Register("x_train", new MemoryCatalogEntry<IEnumerable<FeatureRow>>("x_train"));
      catalog.Register("x_test", new MemoryCatalogEntry<IEnumerable<FeatureRow>>("x_test"));
      catalog.Register("y_train", new MemoryCatalogEntry<IEnumerable<decimal>>("y_train"));
      catalog.Register("y_test", new MemoryCatalogEntry<IEnumerable<decimal>>("y_test"));

      // ═══════════════════════════════════════════════════════════
      // MODELS (06_Models)
      // ═══════════════════════════════════════════════════════════

      catalog.Register("regressor", new MemoryCatalogEntry<ITransformer>("regressor"));
      // In production, would use file-based storage

      // ═══════════════════════════════════════════════════════════
      // REPORTING (08_Reporting)
      // ═══════════════════════════════════════════════════════════

      catalog.Register("model_metrics", new MemoryCatalogEntry<ModelMetrics>("model_metrics"));
      // In production, could use CSV or JSON storage
    });
  }
}
