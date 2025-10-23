using Flowthru.Data;
using Flowthru.Data.Implementations;
using Flowthru.Tests.KedroSpaceflights.Data.Schemas.Models;
using Flowthru.Tests.KedroSpaceflights.Data.Schemas.Processed;
using Flowthru.Tests.KedroSpaceflights.Data.Schemas.Raw;
using Flowthru.Tests.KedroSpaceflights.Data.Schemas.Reference;
using Flowthru.Tests.KedroSpaceflights.Pipelines.DataScience.Nodes;

namespace Flowthru.Tests.KedroSpaceflights.Data;

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
/// <item>03_TrainingData: Primary model inputs</item>
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
public class SpaceflightsCatalog : DataCatalogBase {
  private readonly string _basePath;

  /// <summary>
  /// Initializes a new instance of SpaceflightsCatalog with the specified base path.
  /// </summary>
  /// <param name="basePath">Base path for dataset files</param>
  public SpaceflightsCatalog(string basePath) {
    _basePath = basePath;

    // Eagerly initialize all catalog entries to populate cache
    // This ensures object identity for DAG dependency resolution
    InitializeCatalogProperties();
  }
  // ===========================================================
  // RAW DATA (01_Raw)
  // ===========================================================

  /// <summary>
  /// Raw company data from CSV file.
  /// Contains company ratings and information.
  /// </summary>
  public ICatalogDataset<CompanyRawSchema> Companies =>
    GetOrCreateDataset(() => new CsvCatalogDataset<CompanyRawSchema>("RawCompanies", $"{_basePath}/01_Raw/companies.csv"));

  /// <summary>
  /// Raw review data from CSV file.
  /// Contains customer reviews with scores.
  /// </summary>
  public ICatalogDataset<ReviewRawSchema> Reviews =>
    GetOrCreateDataset(() => new CsvCatalogDataset<ReviewRawSchema>("RawReviews", $"{_basePath}/01_Raw/reviews.csv"));

  /// <summary>
  /// Raw shuttle data from Excel file (read-only).
  /// Contains shuttle specifications and pricing.
  /// </summary>
  /// <remarks>
  /// This dataset is read-only because Excel files cannot be written to by the ExcelDataReader library.
  /// It can only be used as a pipeline input, not as an output.
  /// </remarks>
  public IReadableCatalogDataset<ShuttleRawSchema> Shuttles =>
    GetOrCreateReadOnlyDataset(() => new ExcelCatalogDataset<ShuttleRawSchema>("RawShuttles", $"{_basePath}/01_Raw/shuttles.xlsx", "Sheet1"));

  // ===========================================================
  // INTERMEDIATE DATA (02_Intermediate)
  // ===========================================================

  /// <summary>
  /// Preprocessed company data in Parquet format.
  /// Cleaned and validated company records.
  /// </summary>
  public ICatalogDataset<CompanySchema> CleanedCompanies =>
    GetOrCreateDataset(() => new ParquetCatalogDataset<CompanySchema>("CleanedCompanies", $"{_basePath}/02_Cleaned/cleaned_companies.parquet"));

  /// <summary>
  /// Preprocessed shuttle data in Parquet format.
  /// Cleaned and validated shuttle records.
  /// </summary>
  public ICatalogDataset<ShuttleSchema> CleanedShuttles =>
    GetOrCreateDataset(() => new ParquetCatalogDataset<ShuttleSchema>("CleanedShuttles", $"{_basePath}/02_Cleaned/cleaned_shuttles.parquet"));

  /// <summary>
  /// Preprocessed review data in Parquet format.
  /// Cleaned and validated review records with parsed numeric scores.
  /// </summary>
  public ICatalogDataset<ReviewSchema> CleanedReviews =>
    GetOrCreateDataset(() => new ParquetCatalogDataset<ReviewSchema>("CleanedReviews", $"{_basePath}/02_Cleaned/cleaned_reviews.parquet"));

  // ===========================================================
  // PRIMARY DATA (03_TrainingData)
  // ===========================================================

  /// <summary>
  /// Model input table in Parquet format.
  /// Joined dataset ready for ML training.
  /// </summary>
  public ICatalogDataset<ModelInputSchema> ModelInputTable =>
    GetOrCreateDataset(() => new ParquetCatalogDataset<ModelInputSchema>("ModelInputTable", $"{_basePath}/03_TrainingData/model_input_table.parquet"));

  // ===========================================================
  // DIAGNOSTIC CSV EXPORTS (for debugging)
  // ===========================================================

  /// <summary>
  /// Preprocessed companies exported as CSV (for debugging).
  /// </summary>
  public ICatalogDataset<CompanySchema> CleanedCompaniesCsv =>
    GetOrCreateDataset(() => new CsvCatalogDataset<CompanySchema>("CleanedCompaniesCsv", $"{_basePath}/02_Cleaned/cleaned_companies.csv"));

  /// <summary>
  /// Preprocessed shuttles exported as CSV (for debugging).
  /// </summary>
  public ICatalogDataset<ShuttleSchema> CleanedShuttlesCsv =>
    GetOrCreateDataset(() => new CsvCatalogDataset<ShuttleSchema>("CleanedShuttlesCsv", $"{_basePath}/02_Cleaned/cleaned_shuttles.csv"));

  /// <summary>
  /// Model input table exported as CSV (for debugging).
  /// </summary>
  public ICatalogDataset<ModelInputSchema> ModelInputTableCsv =>
    GetOrCreateDataset(() => new CsvCatalogDataset<ModelInputSchema>("ModelInputTableCsv", $"{_basePath}/03_TrainingData/model_input_table.csv"));

  // ===========================================================
  // REFERENCE DATA (09_Reference - for validation)
  // ===========================================================

  /// <summary>
  /// Reference model input table from Kedro pipeline (for validation).
  /// Used to compare Flowthru implementation against original Kedro output.
  /// </summary>
  public ICatalogDataset<KedroModelInputSchema> KedroModelInputTable =>
    GetOrCreateDataset(() => new CsvCatalogDataset<KedroModelInputSchema>("KedroModelInputTable", $"{_basePath}/99_Reference/kedro_model_input_table.csv"));

  // ===========================================================
  // MODEL DATA (In-Memory Split Results)
  // ===========================================================

  /// <summary>
  /// Training features (X_train).
  /// Feature vectors for model training.
  /// </summary>
  public ICatalogDataset<FeatureRow> XTrain =>
    GetOrCreateDataset(() => new MemoryCatalogDataset<FeatureRow>("XTrain"));

  /// <summary>
  /// Testing features (X_test).
  /// Feature vectors for model evaluation.
  /// </summary>
  public ICatalogDataset<FeatureRow> XTest =>
    GetOrCreateDataset(() => new MemoryCatalogDataset<FeatureRow>("XTest"));

  /// <summary>
  /// Training targets (y_train).
  /// Target prices for model training.
  /// </summary>
  public ICatalogDataset<decimal> YTrain =>
    GetOrCreateDataset(() => new MemoryCatalogDataset<decimal>("YTrain"));

  /// <summary>
  /// Testing targets (y_test).
  /// Target prices for model evaluation.
  /// </summary>
  public ICatalogDataset<decimal> YTest =>
    GetOrCreateDataset(() => new MemoryCatalogDataset<decimal>("YTest"));

  // ===========================================================
  // MODELS (06_Models)
  // ===========================================================

  /// <summary>
  /// Trained ordinary least squares linear regression model.
  /// Contains intercept and coefficients for price prediction.
  /// Stored as a singleton object (pipeline produces single model).
  /// </summary>
  public ICatalogObject<LinearRegressionModel> Regressor =>
    GetOrCreateObject(() => new MemoryCatalogObject<LinearRegressionModel>("Regressor"));

  // ===========================================================
  // REPORTING (08_Reporting)
  // ===========================================================

  /// <summary>
  /// Model evaluation metrics.
  /// Contains R², MAE, RMSE, etc.
  /// Stored as a singleton object (pipeline produces single metrics object).
  /// </summary>
  public ICatalogDataset<ModelMetrics> ModelMetrics =>
    GetOrCreateDataset(() => new CsvCatalogDataset<ModelMetrics>("ModelMetrics", $"{_basePath}/05_ModelOutput/model_metrics.csv"));

  /// <summary>
  /// Cross-validation results with R² distribution analysis.
  /// Contains metrics for each fold, mean, std dev, and comparison to Kedro.
  /// Stored as JSON to preserve nested List&lt;FoldMetric&gt; structure.
  /// </summary>
  public ICatalogObject<CrossValidationResults> CrossValidationResults =>
    GetOrCreateObject(() => new JsonCatalogObject<CrossValidationResults>("CrossValidationResults", $"{_basePath}/05_ModelOutput/cross_validation_results.json"));

  /// <summary>
  /// Model input table exported as minified JSON (compact, production-ready format).
  /// Same data as ModelInputTableJson but without pretty-printing for smaller file size.
  /// </summary>
  public ICatalogDataset<ModelInputSchema> ModelInputTableJsonMinified =>
    GetOrCreateDataset(() => new JsonCatalogDataset<ModelInputSchema>("ModelInputTableJsonMinified", $"{_basePath}/03_TrainingData/model_input_table.min.json", minified: true));
}
