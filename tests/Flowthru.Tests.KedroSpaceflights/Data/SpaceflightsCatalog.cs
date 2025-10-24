using Flowthru.Data;
using Flowthru.Data.Implementations;
using Flowthru.Data.Validation;
using Flowthru.Tests.KedroSpaceflights.Data.Schemas.Models;
using Flowthru.Tests.KedroSpaceflights.Data.Schemas.Processed;
using Flowthru.Tests.KedroSpaceflights.Data.Schemas.Raw;
using Flowthru.Tests.KedroSpaceflights.Data.Schemas.Reference;
using Flowthru.Tests.KedroSpaceflights.Pipelines.DataScience.Nodes;
using Plotly.NET;

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
/// <item>06_Reports: Reports and metrics</item>
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
  // MARK: RAW DATA (01_Raw)
  // ===========================================================

  /// <summary>
  /// Raw company data from CSV file.
  /// Contains company ratings and information.
  /// </summary>
  /// <remarks>
  /// This is a critical Layer 0 input from an external source, configured for deep inspection
  /// to ensure data quality before pipeline execution.
  /// </remarks>
  public ICatalogDataset<CompanyRawSchema> Companies => GetOrCreateDataset(()
    => new CsvCatalogDataset<CompanyRawSchema>(
      key: "RawCompanies",
      filePath: $"{_basePath}/01_Raw/companies.csv")
    .WithInspectionLevel(InspectionLevel.Deep));

  /// <summary>
  /// Raw review data from CSV file.
  /// Contains customer reviews with scores.
  /// </summary>
  /// <remarks>
  /// This is a critical Layer 0 input from an external source, configured for deep inspection
  /// to ensure data quality before pipeline execution.
  /// </remarks>
  public ICatalogDataset<ReviewRawSchema> Reviews => GetOrCreateDataset(()
    => new CsvCatalogDataset<ReviewRawSchema>(
      key: "RawReviews",
      filePath: $"{_basePath}/01_Raw/reviews.csv")
    .WithInspectionLevel(InspectionLevel.Deep));

  /// <summary>
  /// Raw shuttle data from Excel file (read-only).
  /// Contains shuttle specifications and pricing.
  /// </summary>
  /// <remarks>
  /// This dataset is read-only because Excel files cannot be written to by the ExcelDataReader library.
  /// It can only be used as a pipeline input, not as an output.
  /// This is a critical Layer 0 input from an external source, configured for deep inspection
  /// to ensure data quality before pipeline execution.
  /// </remarks>
  public IReadableCatalogDataset<ShuttleRawSchema> Shuttles => GetOrCreateReadOnlyDataset(()
    => new ExcelCatalogDataset<ShuttleRawSchema>(
      key: "RawShuttles",
      filePath: $"{_basePath}/01_Raw/shuttles.xlsx",
      sheetName: "Sheet1")
    .WithInspectionLevel(InspectionLevel.Deep));

  // ===========================================================
  // MARK: CLEANED DATA
  // ===========================================================

  /// <summary>
  /// Preprocessed company data in Parquet format.
  /// Cleaned and validated company records.
  /// </summary>
  public ICatalogDataset<CompanySchema> CleanedCompanies => GetOrCreateDataset(()
    => new ParquetCatalogDataset<CompanySchema>(
      key: "CleanedCompanies",
      filePath: $"{_basePath}/02_Cleaned/cleaned_companies.parquet"));

  /// <summary>
  /// Preprocessed shuttle data in Parquet format.
  /// Cleaned and validated shuttle records.
  /// </summary>
  public ICatalogDataset<ShuttleSchema> CleanedShuttles => GetOrCreateDataset(()
    => new ParquetCatalogDataset<ShuttleSchema>(
      key: "CleanedShuttles",
      filePath: $"{_basePath}/02_Cleaned/cleaned_shuttles.parquet"));

  /// <summary>
  /// Preprocessed review data in Parquet format.
  /// Cleaned and validated review records with parsed numeric scores.
  /// </summary>
  public ICatalogDataset<ReviewSchema> CleanedReviews => GetOrCreateDataset(()
    => new ParquetCatalogDataset<ReviewSchema>(
      key: "CleanedReviews",
      filePath: $"{_basePath}/02_Cleaned/cleaned_reviews.parquet"));

  /// <summary>
  /// Preprocessed companies exported as CSV (for debugging).
  /// </summary>
  public ICatalogDataset<CompanySchema> CleanedCompaniesCsv => GetOrCreateDataset(()
    => new CsvCatalogDataset<CompanySchema>(
      key: "CleanedCompaniesCsv",
      filePath: $"{_basePath}/02_Cleaned/cleaned_companies.csv"));

  /// <summary>
  /// Preprocessed shuttles exported as CSV (for debugging).
  /// </summary>
  public ICatalogDataset<ShuttleSchema> CleanedShuttlesCsv => GetOrCreateDataset(()
    => new CsvCatalogDataset<ShuttleSchema>(
      key: "CleanedShuttlesCsv",
      filePath: $"{_basePath}/02_Cleaned/cleaned_shuttles.csv"));


  // ===========================================================
  // MARK: TRAINING DATA
  // ===========================================================

  /// <summary>
  /// Model input table in Parquet format.
  /// Joined dataset ready for ML training.
  /// </summary>
  public ICatalogDataset<ModelInputSchema> ModelInputTable => GetOrCreateDataset(()
    => new ParquetCatalogDataset<ModelInputSchema>(
      key: "ModelInputTable",
      filePath: $"{_basePath}/03_TrainingData/model_input_table.parquet"));

  /// <summary>
  /// Model input table exported as minified JSON (compact, production-ready format).
  /// </summary>
  public ICatalogDataset<ModelInputSchema> ModelInputTableJsonMinified => GetOrCreateDataset(()
    => new JsonCatalogDataset<ModelInputSchema>(
      key: "ModelInputTableJsonMinified",
      filePath: $"{_basePath}/03_TrainingData/model_input_table.min.json",
      minified: true));

  /// <summary>
  /// Model input table exported as CSV (for debugging).
  /// </summary>
  public ICatalogDataset<ModelInputSchema> ModelInputTableCsv => GetOrCreateDataset(()
    => new CsvCatalogDataset<ModelInputSchema>(
      key: "ModelInputTableCsv",
      filePath: $"{_basePath}/03_TrainingData/model_input_table.csv"));

  // ===========================================================
  // MARK: REFERENCE DATA
  // ===========================================================

  /// <summary>
  /// Reference model input table from Kedro pipeline (for validation).
  /// Used to compare Flowthru implementation against original Kedro output.
  /// </summary>
  public ICatalogDataset<KedroModelInputSchema> KedroModelInputTable => GetOrCreateDataset(()
    => new CsvCatalogDataset<KedroModelInputSchema>(
      key: "KedroModelInputTable",
      filePath: $"{_basePath}/99_Reference/kedro_model_input_table.csv"));

  // ===========================================================
  // MARK: TEST-TRAIN SPLIT
  // ===========================================================

  /// <summary>
  /// Training features (X_train).
  /// Feature vectors for model training.
  /// </summary>
  public ICatalogDataset<FeatureRow> XTrain => GetOrCreateDataset(()
    => new MemoryCatalogDataset<FeatureRow>(
      key: "XTrain"));

  /// <summary>
  /// Testing features (X_test).
  /// Feature vectors for model evaluation.
  /// </summary>
  public ICatalogDataset<FeatureRow> XTest => GetOrCreateDataset(()
    => new MemoryCatalogDataset<FeatureRow>(
      key: "XTest"));

  /// <summary>
  /// Training targets (y_train).
  /// Target prices for model training.
  /// </summary>
  public ICatalogDataset<decimal> YTrain => GetOrCreateDataset(()
    => new MemoryCatalogDataset<decimal>(
      key: "YTrain"));

  /// <summary>
  /// Testing targets (y_test).
  /// Target prices for model evaluation.
  /// </summary>
  public ICatalogDataset<decimal> YTest => GetOrCreateDataset(()
    => new MemoryCatalogDataset<decimal>(
      key: "YTest"));

  // ===========================================================
  // MARK: MODELS
  // ===========================================================

  /// <summary>
  /// Trained ordinary least squares linear regression model.
  /// Contains intercept and coefficients for price prediction.
  /// Stored as a singleton object (pipeline produces single model).
  /// </summary>
  public ICatalogObject<LinearRegressionModel> Regressor => GetOrCreateObject(()
    => new MemoryCatalogObject<LinearRegressionModel>(
      key: "Regressor"));

  // ===========================================================
  // MARK: REPORTING (06_Reports)
  // ===========================================================

  /// <summary>
  /// Model evaluation metrics.
  /// Contains R², MAE, RMSE, etc.
  /// Stored as a singleton object (pipeline produces single metrics object).
  /// </summary>
  public ICatalogDataset<ModelMetrics> ModelMetrics => GetOrCreateDataset(()
    => new CsvCatalogDataset<ModelMetrics>(
      key: "ModelMetrics",
      filePath: $"{_basePath}/06_Reports/model_metrics.csv"));

  /// <summary>
  /// Cross-validation results with R² distribution analysis.
  /// Contains metrics for each fold, mean, std dev, and comparison to Kedro.
  /// Stored as JSON to preserve nested List&lt;FoldMetric&gt; structure.
  /// </summary>
  public ICatalogObject<CrossValidationResults> CrossValidationResults => GetOrCreateObject(()
    => new JsonCatalogObject<CrossValidationResults>(
      key: "CrossValidationResults",
      filePath: $"{_basePath}/06_Reports/cross_validation_results.json"));

  /// <summary>
  /// Cross-validation summary report in Markdown format.
  /// Human-readable report summarizing model performance and validation results.
  /// </summary>
  public ICatalogObject<string> CrossValidationReport => GetOrCreateObject(()
    => new FileCatalogObject(
      key: "CrossValidationReport",
      filePath: $"{_basePath}/06_Reports/cross_validation_report.md"));

  /// <summary>
  /// Shuttle passenger capacity bar chart (in-memory GenericChart).
  /// Intermediate chart object stored in memory for downstream export to multiple formats.
  /// </summary>
  public ICatalogObject<GenericChart> ShuttlePassengerCapacityChart => GetOrCreateObject(()
    => new MemoryCatalogObject<GenericChart>(
      key: "ShuttlePassengerCapacityChart"));

  /// <summary>
  /// Shuttle passenger capacity visualization (Plotly JSON).
  /// Bar chart showing average passenger capacity grouped by shuttle type.
  /// Stored as Plotly JSON specification, compatible with plotly.js rendering.
  /// </summary>
  /// <remarks>
  /// Output format matches Kedro's plotly.JSONDataset. The JSON contains a complete Plotly
  /// figure specification with data traces and layout configuration. Can be rendered in browsers
  /// using plotly.js or converted to static images using Plotly.NET.ImageExport.
  /// </remarks>
  public ICatalogObject<string> ShuttlePassengerCapacityPlot => GetOrCreateObject(()
    => new FileCatalogObject(
      key: "ShuttlePassengerCapacityPlot",
      filePath: $"{_basePath}/06_Reports/shuttle_passenger_capacity_plot.json"));

  /// <summary>
  /// Confusion matrix heatmap (in-memory GenericChart).
  /// Intermediate chart object stored in memory for downstream export to multiple formats.
  /// </summary>
  public ICatalogObject<GenericChart> ConfusionMatrixChart => GetOrCreateObject(()
    => new MemoryCatalogObject<GenericChart>(
      key: "ConfusionMatrixChart"));

  /// <summary>
  /// Confusion matrix heatmap visualization (Plotly JSON).
  /// Shows model prediction accuracy with actual vs predicted classification matrix.
  /// Stored as Plotly JSON specification for interactive visualization.
  /// </summary>
  /// <remarks>
  /// Matches Kedro's matplotlib.MatplotlibWriter output but using Plotly for interactivity.
  /// The heatmap displays a 2x2 confusion matrix with color-coded cells showing classification
  /// performance. JSON format allows browser-based rendering and potential conversion to PNG.
  /// </remarks>
  public ICatalogObject<string> ConfusionMatrixPlot => GetOrCreateObject(()
    => new FileCatalogObject(
      key: "ConfusionMatrixPlot",
      filePath: $"{_basePath}/06_Reports/confusion_matrix_plot.json"));

}

