using Flowthru.Data;
using Flowthru.Data.Implementations;
using Flowthru.Spaceflights.Data.Schemas.Raw;
using Flowthru.Spaceflights.Data.Schemas.Processed;
using Flowthru.Spaceflights.Data.Schemas.Models;
using Flowthru.Spaceflights.Data.Schemas.Reference;
using Flowthru.Spaceflights.Pipelines.DataScience.Nodes;

namespace Flowthru.Spaceflights.Data;

/// <summary>
/// Strongly-typed catalog for the Spaceflights project.
/// 
/// <para><strong>Compile-Time Type Safety:</strong></para>
/// <para>
/// Each catalog entry is a strongly-typed property (ICatalogEntry&lt;T&gt;), ensuring:
/// - Pipelines cannot reference non-existent entries (compile error)
/// - Nodes cannot receive wrong input types (compile error)
/// - IntelliSense shows all available entries with their types
/// - Refactoring tools work seamlessly (rename, find references)
/// </para>
/// 
/// <para><strong>Data Layering Convention (Kedro):</strong></para>
/// <list type="bullet">
/// <item>01_Raw: Raw input data from external sources</item>
/// <item>02_Intermediate: Preprocessed/cleaned data</item>
/// <item>03_Primary: Primary model inputs</item>
/// <item>06_Models: Trained ML models</item>
/// <item>08_Reporting: Reports and metrics</item>
/// </list>
/// 
/// <para><strong>Zero-Ceremony Construction with Reflection-Based Caching:</strong></para>
/// <para>
/// Inherits from DataCatalogBase which provides automatic instance caching via reflection.
/// Catalog entries are defined ONCE via expression-bodied properties, with GetOrCreateEntry
/// ensuring object identity for DAG dependency resolution.
/// </para>
/// <para>
/// Usage: <c>var catalog = new SpaceflightsCatalog("Data/Datasets");</c>
/// </para>
/// </summary>
public class SpaceflightsCatalog : DataCatalogBase
{
  private readonly string _basePath;

  /// <summary>
  /// Initializes a new instance of SpaceflightsCatalog with the specified base path.
  /// </summary>
  /// <param name="basePath">Base path for dataset files (default: "Data/Datasets")</param>
  public SpaceflightsCatalog(string basePath = "Data/Datasets")
  {
    _basePath = basePath;

    // Eagerly initialize all catalog entries to populate cache
    // This ensures object identity for DAG dependency resolution
    InitializeCatalogProperties();
  }
  // ═══════════════════════════════════════════════════════════
  // RAW DATA (01_Raw)
  // ═══════════════════════════════════════════════════════════

  /// <summary>
  /// Raw company data from CSV file.
  /// Contains company ratings and information.
  /// </summary>
  public ICatalogEntry<IEnumerable<CompanyRawSchema>> Companies =>
    GetOrCreateEntry(() => new CsvCatalogEntry<CompanyRawSchema>("companies", $"{_basePath}/01_Raw/companies.csv"));

  /// <summary>
  /// Raw review data from CSV file.
  /// Contains customer reviews with scores.
  /// </summary>
  public ICatalogEntry<IEnumerable<ReviewRawSchema>> Reviews =>
    GetOrCreateEntry(() => new CsvCatalogEntry<ReviewRawSchema>("reviews", $"{_basePath}/01_Raw/reviews.csv"));

  /// <summary>
  /// Raw shuttle data from Excel file.
  /// Contains shuttle specifications and pricing.
  /// </summary>
  public ICatalogEntry<IEnumerable<ShuttleRawSchema>> Shuttles =>
    GetOrCreateEntry(() => new ExcelCatalogEntry<ShuttleRawSchema>("shuttles", $"{_basePath}/01_Raw/shuttles.xlsx", "Sheet1"));

  // ═══════════════════════════════════════════════════════════
  // INTERMEDIATE DATA (02_Intermediate)
  // ═══════════════════════════════════════════════════════════

  /// <summary>
  /// Preprocessed company data in Parquet format.
  /// Cleaned and validated company records.
  /// </summary>
  public ICatalogEntry<IEnumerable<CompanySchema>> PreprocessedCompanies =>
    GetOrCreateEntry(() => new ParquetCatalogEntry<CompanySchema>("preprocessed_companies", $"{_basePath}/02_Intermediate/preprocessed_companies.parquet"));

  /// <summary>
  /// Preprocessed shuttle data in Parquet format.
  /// Cleaned and validated shuttle records.
  /// </summary>
  public ICatalogEntry<IEnumerable<ShuttleSchema>> PreprocessedShuttles =>
    GetOrCreateEntry(() => new ParquetCatalogEntry<ShuttleSchema>("preprocessed_shuttles", $"{_basePath}/02_Intermediate/preprocessed_shuttles.parquet"));

  // ═══════════════════════════════════════════════════════════
  // PRIMARY DATA (03_Primary)
  // ═══════════════════════════════════════════════════════════

  /// <summary>
  /// Model input table in Parquet format.
  /// Joined dataset ready for ML training.
  /// </summary>
  public ICatalogEntry<IEnumerable<ModelInputSchema>> ModelInputTable =>
    GetOrCreateEntry(() => new ParquetCatalogEntry<ModelInputSchema>("model_input_table", $"{_basePath}/03_Primary/model_input_table.parquet"));

  // ═══════════════════════════════════════════════════════════
  // DIAGNOSTIC CSV EXPORTS (for debugging)
  // ═══════════════════════════════════════════════════════════

  /// <summary>
  /// Preprocessed companies exported as CSV (for debugging).
  /// </summary>
  public ICatalogEntry<IEnumerable<CompanySchema>> PreprocessedCompaniesCsv =>
    GetOrCreateEntry(() => new CsvCatalogEntry<CompanySchema>("preprocessed_companies_csv", $"{_basePath}/02_Intermediate/preprocessed_companies.csv"));

  /// <summary>
  /// Preprocessed shuttles exported as CSV (for debugging).
  /// </summary>
  public ICatalogEntry<IEnumerable<ShuttleSchema>> PreprocessedShuttlesCsv =>
    GetOrCreateEntry(() => new CsvCatalogEntry<ShuttleSchema>("preprocessed_shuttles_csv", $"{_basePath}/02_Intermediate/preprocessed_shuttles.csv"));

  /// <summary>
  /// Model input table exported as CSV (for debugging).
  /// </summary>
  public ICatalogEntry<IEnumerable<ModelInputSchema>> ModelInputTableCsv =>
    GetOrCreateEntry(() => new CsvCatalogEntry<ModelInputSchema>("model_input_table_csv", $"{_basePath}/03_Primary/model_input_table.csv"));

  // ═══════════════════════════════════════════════════════════
  // REFERENCE DATA (09_Reference - for validation)
  // ═══════════════════════════════════════════════════════════

  /// <summary>
  /// Reference model input table from Kedro pipeline (for validation).
  /// Used to compare Flowthru implementation against original Kedro output.
  /// </summary>
  public ICatalogEntry<IEnumerable<KedroModelInputSchema>> KedroModelInputTable =>
    GetOrCreateEntry(() => new CsvCatalogEntry<KedroModelInputSchema>("kedro_model_input_table", $"{_basePath}/09_Reference/kedro_model_input_table.csv"));

  // ═══════════════════════════════════════════════════════════
  // MODEL DATA (In-Memory Split Results)
  // ═══════════════════════════════════════════════════════════

  /// <summary>
  /// Training features (X_train).
  /// Feature vectors for model training.
  /// </summary>
  public ICatalogEntry<IEnumerable<FeatureRow>> XTrain =>
    GetOrCreateEntry(() => new MemoryCatalogEntry<IEnumerable<FeatureRow>>("x_train"));

  /// <summary>
  /// Testing features (X_test).
  /// Feature vectors for model evaluation.
  /// </summary>
  public ICatalogEntry<IEnumerable<FeatureRow>> XTest =>
    GetOrCreateEntry(() => new MemoryCatalogEntry<IEnumerable<FeatureRow>>("x_test"));

  /// <summary>
  /// Training targets (y_train).
  /// Target prices for model training.
  /// </summary>
  public ICatalogEntry<IEnumerable<decimal>> YTrain =>
    GetOrCreateEntry(() => new MemoryCatalogEntry<IEnumerable<decimal>>("y_train"));

  /// <summary>
  /// Testing targets (y_test).
  /// Target prices for model evaluation.
  /// </summary>
  public ICatalogEntry<IEnumerable<decimal>> YTest =>
    GetOrCreateEntry(() => new MemoryCatalogEntry<IEnumerable<decimal>>("y_test"));

  // ═══════════════════════════════════════════════════════════
  // MODELS (06_Models)
  // ═══════════════════════════════════════════════════════════

  /// <summary>
  /// Trained ordinary least squares linear regression model.
  /// Contains intercept and coefficients for price prediction.
  /// Stored as a singleton collection (pipeline produces single model).
  /// </summary>
  public ICatalogEntry<IEnumerable<LinearRegressionModel>> Regressor =>
    GetOrCreateEntry(() => new MemoryCatalogEntry<IEnumerable<LinearRegressionModel>>("regressor"));

  // ═══════════════════════════════════════════════════════════
  // REPORTING (08_Reporting)
  // ═══════════════════════════════════════════════════════════

  /// <summary>
  /// Model evaluation metrics.
  /// Contains R², MAE, RMSE, etc.
  /// Stored as a singleton collection (pipeline produces single metrics object).
  /// </summary>
  public ICatalogEntry<IEnumerable<ModelMetrics>> ModelMetrics =>
    GetOrCreateEntry(() => new CsvCatalogEntry<ModelMetrics>("model_metrics", $"{_basePath}/07_Model_Output/model_metrics.csv"));

  /// <summary>
  /// Cross-validation results with R² distribution analysis.
  /// Contains metrics for each fold, mean, std dev, and comparison to Kedro.
  /// </summary>
  public ICatalogEntry<IEnumerable<CrossValidationResults>> CrossValidationResults =>
    GetOrCreateEntry(() => new CsvCatalogEntry<CrossValidationResults>("cross_validation_results", $"{_basePath}/08_Reporting/cross_validation_results.csv"));
}
