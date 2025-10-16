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
/// </summary>
public class SpaceflightsCatalog
{
  // ═══════════════════════════════════════════════════════════
  // RAW DATA (01_Raw)
  // ═══════════════════════════════════════════════════════════

  /// <summary>
  /// Raw company data from CSV file.
  /// Contains company ratings and information.
  /// </summary>
  public ICatalogEntry<IEnumerable<CompanyRawSchema>> Companies { get; }

  /// <summary>
  /// Raw review data from CSV file.
  /// Contains customer reviews with scores.
  /// </summary>
  public ICatalogEntry<IEnumerable<ReviewRawSchema>> Reviews { get; }

  /// <summary>
  /// Raw shuttle data from Excel file.
  /// Contains shuttle specifications and pricing.
  /// </summary>
  public ICatalogEntry<IEnumerable<ShuttleRawSchema>> Shuttles { get; }

  // ═══════════════════════════════════════════════════════════
  // INTERMEDIATE DATA (02_Intermediate)
  // ═══════════════════════════════════════════════════════════

  /// <summary>
  /// Preprocessed company data in Parquet format.
  /// Cleaned and validated company records.
  /// </summary>
  public ICatalogEntry<IEnumerable<CompanySchema>> PreprocessedCompanies { get; }

  /// <summary>
  /// Preprocessed shuttle data in Parquet format.
  /// Cleaned and validated shuttle records.
  /// </summary>
  public ICatalogEntry<IEnumerable<ShuttleSchema>> PreprocessedShuttles { get; }

  // ═══════════════════════════════════════════════════════════
  // PRIMARY DATA (03_Primary)
  // ═══════════════════════════════════════════════════════════

  /// <summary>
  /// Model input table in Parquet format.
  /// Joined dataset ready for ML training.
  /// </summary>
  public ICatalogEntry<IEnumerable<ModelInputSchema>> ModelInputTable { get; }

  // ═══════════════════════════════════════════════════════════
  // DIAGNOSTIC CSV EXPORTS (for debugging)
  // ═══════════════════════════════════════════════════════════

  /// <summary>
  /// Preprocessed companies exported as CSV (for debugging).
  /// </summary>
  public ICatalogEntry<IEnumerable<CompanySchema>> PreprocessedCompaniesCsv { get; }

  /// <summary>
  /// Preprocessed shuttles exported as CSV (for debugging).
  /// </summary>
  public ICatalogEntry<IEnumerable<ShuttleSchema>> PreprocessedShuttlesCsv { get; }

  /// <summary>
  /// Model input table exported as CSV (for debugging).
  /// </summary>
  public ICatalogEntry<IEnumerable<ModelInputSchema>> ModelInputTableCsv { get; }

  // ═══════════════════════════════════════════════════════════
  // REFERENCE DATA (09_Reference - for validation)
  // ═══════════════════════════════════════════════════════════

  /// <summary>
  /// Reference model input table from Kedro pipeline (for validation).
  /// Used to compare Flowthru implementation against original Kedro output.
  /// </summary>
  public ICatalogEntry<IEnumerable<KedroModelInputSchema>> KedroModelInputTable { get; }

  // ═══════════════════════════════════════════════════════════
  // MODEL DATA (In-Memory Split Results)
  // ═══════════════════════════════════════════════════════════

  /// <summary>
  /// Training features (X_train).
  /// Feature vectors for model training.
  /// </summary>
  public ICatalogEntry<IEnumerable<FeatureRow>> XTrain { get; }

  /// <summary>
  /// Testing features (X_test).
  /// Feature vectors for model evaluation.
  /// </summary>
  public ICatalogEntry<IEnumerable<FeatureRow>> XTest { get; }

  /// <summary>
  /// Training targets (y_train).
  /// Target prices for model training.
  /// </summary>
  public ICatalogEntry<IEnumerable<decimal>> YTrain { get; }

  /// <summary>
  /// Testing targets (y_test).
  /// Target prices for model evaluation.
  /// </summary>
  public ICatalogEntry<IEnumerable<decimal>> YTest { get; }

  // ═══════════════════════════════════════════════════════════
  // MODELS (06_Models)
  // ═══════════════════════════════════════════════════════════

  /// <summary>
  /// Trained ordinary least squares linear regression model.
  /// Contains intercept and coefficients for price prediction.
  /// Stored as a singleton collection (pipeline produces single model).
  /// </summary>
  public ICatalogEntry<IEnumerable<LinearRegressionModel>> Regressor { get; }

  // ═══════════════════════════════════════════════════════════
  // REPORTING (08_Reporting)
  // ═══════════════════════════════════════════════════════════

  /// <summary>
  /// Model evaluation metrics.
  /// Contains R², MAE, RMSE, etc.
  /// Stored as a singleton collection (pipeline produces single metrics object).
  /// </summary>
  public ICatalogEntry<IEnumerable<ModelMetrics>> ModelMetrics { get; }

  /// <summary>
  /// Cross-validation results with R² distribution analysis.
  /// Contains metrics for each fold, mean, std dev, and comparison to Kedro.
  /// </summary>
  public ICatalogEntry<IEnumerable<CrossValidationResults>> CrossValidationResults { get; }

  /// <summary>
  /// Private constructor - use Build() factory method.
  /// </summary>
  private SpaceflightsCatalog(
    ICatalogEntry<IEnumerable<CompanyRawSchema>> companies,
    ICatalogEntry<IEnumerable<ReviewRawSchema>> reviews,
    ICatalogEntry<IEnumerable<ShuttleRawSchema>> shuttles,
    ICatalogEntry<IEnumerable<CompanySchema>> preprocessedCompanies,
    ICatalogEntry<IEnumerable<ShuttleSchema>> preprocessedShuttles,
    ICatalogEntry<IEnumerable<ModelInputSchema>> modelInputTable,
    ICatalogEntry<IEnumerable<CompanySchema>> preprocessedCompaniesCsv,
    ICatalogEntry<IEnumerable<ShuttleSchema>> preprocessedShuttlesCsv,
    ICatalogEntry<IEnumerable<ModelInputSchema>> modelInputTableCsv,
    ICatalogEntry<IEnumerable<KedroModelInputSchema>> kedroModelInputTable,
    ICatalogEntry<IEnumerable<FeatureRow>> xTrain,
    ICatalogEntry<IEnumerable<FeatureRow>> xTest,
    ICatalogEntry<IEnumerable<decimal>> yTrain,
    ICatalogEntry<IEnumerable<decimal>> yTest,
    ICatalogEntry<IEnumerable<LinearRegressionModel>> regressor,
    ICatalogEntry<IEnumerable<ModelMetrics>> modelMetrics,
    ICatalogEntry<IEnumerable<CrossValidationResults>> crossValidationResults)
  {
    Companies = companies;
    Reviews = reviews;
    Shuttles = shuttles;
    PreprocessedCompanies = preprocessedCompanies;
    PreprocessedShuttles = preprocessedShuttles;
    ModelInputTable = modelInputTable;
    PreprocessedCompaniesCsv = preprocessedCompaniesCsv;
    PreprocessedShuttlesCsv = preprocessedShuttlesCsv;
    ModelInputTableCsv = modelInputTableCsv;
    KedroModelInputTable = kedroModelInputTable;
    XTrain = xTrain;
    XTest = xTest;
    YTrain = yTrain;
    YTest = yTest;
    Regressor = regressor;
    ModelMetrics = modelMetrics;
    CrossValidationResults = crossValidationResults;
  }

  /// <summary>
  /// Builds and returns a configured SpaceflightsCatalog instance.
  /// </summary>
  /// <param name="basePath">Base path for dataset files</param>
  /// <returns>Fully configured typed catalog</returns>
  public static SpaceflightsCatalog Build(string basePath = "Data/Datasets")
  {
    return new SpaceflightsCatalog(
      companies: new CsvCatalogEntry<CompanyRawSchema>(
        "companies",
        $"{basePath}/01_Raw/companies.csv"),

      reviews: new CsvCatalogEntry<ReviewRawSchema>(
        "reviews",
        $"{basePath}/01_Raw/reviews.csv"),

      shuttles: new ExcelCatalogEntry<ShuttleRawSchema>(
        "shuttles",
        $"{basePath}/01_Raw/shuttles.xlsx",
        "Sheet1"),

      preprocessedCompanies: new ParquetCatalogEntry<CompanySchema>(
        "preprocessed_companies",
        $"{basePath}/02_Intermediate/preprocessed_companies.parquet"),

      preprocessedShuttles: new ParquetCatalogEntry<ShuttleSchema>(
        "preprocessed_shuttles",
        $"{basePath}/02_Intermediate/preprocessed_shuttles.parquet"),

      modelInputTable: new ParquetCatalogEntry<ModelInputSchema>(
        "model_input_table",
        $"{basePath}/03_Primary/model_input_table.parquet"),

      preprocessedCompaniesCsv: new CsvCatalogEntry<CompanySchema>(
        "preprocessed_companies_csv",
        $"{basePath}/02_Intermediate/preprocessed_companies.csv"),

      preprocessedShuttlesCsv: new CsvCatalogEntry<ShuttleSchema>(
        "preprocessed_shuttles_csv",
        $"{basePath}/02_Intermediate/preprocessed_shuttles.csv"),

      modelInputTableCsv: new CsvCatalogEntry<ModelInputSchema>(
        "model_input_table_csv",
        $"{basePath}/03_Primary/model_input_table.csv"),

      kedroModelInputTable: new CsvCatalogEntry<KedroModelInputSchema>(
        "kedro_model_input_table",
        $"{basePath}/09_Reference/kedro_model_input_table.csv"),

      xTrain: new MemoryCatalogEntry<IEnumerable<FeatureRow>>("x_train"),
      xTest: new MemoryCatalogEntry<IEnumerable<FeatureRow>>("x_test"),
      yTrain: new MemoryCatalogEntry<IEnumerable<decimal>>("y_train"),
      yTest: new MemoryCatalogEntry<IEnumerable<decimal>>("y_test"),

      regressor: new MemoryCatalogEntry<IEnumerable<LinearRegressionModel>>("regressor"),

      modelMetrics: new CsvCatalogEntry<ModelMetrics>(
        "model_metrics",
        $"{basePath}/07_Model_Output/model_metrics.csv"),

      crossValidationResults: new CsvCatalogEntry<CrossValidationResults>(
        "cross_validation_results",
        $"{basePath}/08_Reporting/cross_validation_results.csv")
    );
  }
}
