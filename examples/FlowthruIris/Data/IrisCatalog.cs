using Flowthru.Data;
using Flowthru.Data.Implementations;
using FlowthruIris.Data.Schemas;
using Microsoft.ML;
using Microsoft.ML.Data;

namespace FlowthruIris.Data;

/// <summary>
/// Strongly-typed catalog for the Iris analysis project.
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
/// <para><strong>Data Layering Convention:</strong></para>
/// <list type="bullet">
/// <item>01_Raw: Raw input data from external sources (CSV files)</item>
/// <item>02_Models: Trained ML models with evaluation metrics</item>
/// <item>03_Reports: Generated visualizations (PNG images)</item>
/// <item>04_Reports: Generated text reports (Markdown files)</item>
/// </list>
///
/// <para><strong>Note on Intermediate Data:</strong></para>
/// <para>
/// Processed Iris data (IrisSchema) is NOT persisted to disk - it exists only in memory
/// during pipeline execution. This reduces I/O overhead for small datasets and demonstrates
/// that not all pipeline data needs to be saved.
/// </para>
/// </summary>
public class IrisCatalog : DataCatalogBase
{
  private readonly string _basePath;

  /// <summary>
  /// Initializes a new instance of IrisCatalog with the specified base path.
  /// </summary>
  /// <param name="basePath">Base path for dataset files (default: "Datasets")</param>
  public IrisCatalog(string basePath = "Datasets")
  {
    _basePath = basePath;
    InitializeCatalogProperties();
  }

  // ========================================
  // 01_Raw: External CSV Input
  // ========================================

  /// <summary>
  /// Raw Iris data from CSV file.
  /// Contains string values that need parsing and validation.
  /// </summary>
  public ICatalogEntry<IEnumerable<IrisRawSchema>> RawIris =>
    GetOrCreateEntry(
      () =>
        new CsvCatalogEntry<IEnumerable<IrisRawSchema>>(
          key: "raw_iris",
          filePath: Path.Combine(_basePath, "01_Raw/iris.csv")
        )
    );

  // ========================================
  // 02_Models: ML.NET Models + Metrics
  // ========================================

  /// <summary>
  /// SDCA Maximum Entropy trained model.
  /// Uses ML.NET native serialization to persist ITransformer as .zip file.
  ///
  /// <para><strong>Multi-Output Pattern:</strong></para>
  /// <para>
  /// Paired with SdcaMetrics for complete training results. The TrainSdcaModelNode
  /// produces both outputs using Flowthru's tuple support:
  /// </para>
  /// <code>
  /// pipeline.AddNode&lt;TrainSdcaModelNode&gt;(
  ///     input: processedData,
  ///     output: (catalog.SdcaModel, catalog.SdcaMetrics),
  ///     label: "TrainSdca"
  /// );
  /// </code>
  /// </summary>
  public ICatalogEntry<ITransformer> SdcaModel =>
    GetOrCreateEntry(
      () =>
        new MLNetModelCatalogEntry(
          key: "sdca_model",
          modelPath: "Datasets/08_Models/sdca_model.zip"
        )
    );

  /// <summary>
  /// SDCA model evaluation metrics.
  /// Stored as JSON for easy inspection and reporting.
  /// </summary>
  public ICatalogEntry<MulticlassClassificationMetrics> SdcaMetrics =>
    GetOrCreateEntry(
      () =>
        new JsonCatalogEntry<MulticlassClassificationMetrics>(
          key: "sdca_metrics",
          filePath: "Datasets/08_Models/sdca_metrics.json"
        )
    );

  /// <summary>
  /// One-vs-All Perceptron trained model.
  /// Uses ML.NET native serialization to persist ITransformer as .zip file.
  /// </summary>
  public ICatalogEntry<ITransformer> OvaPerceptronModel =>
    GetOrCreateEntry(
      () =>
        new MLNetModelCatalogEntry(
          key: "ova_perceptron_model",
          modelPath: "Datasets/08_Models/ova_perceptron_model.zip"
        )
    );

  /// <summary>
  /// One-vs-All Perceptron model evaluation metrics.
  /// Stored as JSON for easy inspection and reporting.
  /// </summary>
  public ICatalogEntry<MulticlassClassificationMetrics> OvaPerceptronMetrics =>
    GetOrCreateEntry(
      () =>
        new JsonCatalogEntry<MulticlassClassificationMetrics>(
          key: "ova_perceptron_metrics",
          filePath: "Datasets/08_Models/ova_perceptron_metrics.json"
        )
    );

  /// <summary>
  /// Advanced classification model (OneVersusAll with Averaged Perceptron).
  /// Ensemble model - combines multiple binary classifiers for multi-class prediction.
  /// Tuple contains: (trained model, evaluation metrics)
  /// NOTE: Stored in memory only for this example. For production, use a custom
  /// catalog entry with ML.NET's model serialization.
  /// </summary>
  public ICatalogEntry<(ITransformer Model, MulticlassClassificationMetrics Metrics)> OvaModel =>
    GetOrCreateEntry(
      () =>
        new Flowthru.Data.Implementations.MemoryCatalogEntry<(
          ITransformer,
          MulticlassClassificationMetrics
        )>(key: "ova_model")
    );

  // ========================================
  // 03_Reports: Visualizations (PNG)
  // ========================================

  /// <summary>
  /// Scatter plot visualization of processed Iris data.
  /// Shows species separation by petal dimensions.
  /// Stored as PNG image for inclusion in reports.
  /// </summary>
  public ICatalogEntry<byte[]> DataScatterPng =>
    GetOrCreateEntry(
      () =>
        new BinaryFileCatalogEntry(
          key: "data_scatter_png",
          filePath: Path.Combine(_basePath, "03_Reports/data_scatter.png"),
          expectedFileType: BinaryFileType.Png
        )
    );

  /// <summary>
  /// Confusion matrix heatmap for SDCA model.
  /// Shows classification accuracy across all species.
  /// Stored as PNG image.
  /// </summary>
  public ICatalogEntry<byte[]> SdcaConfusionMatrixPng =>
    GetOrCreateEntry(
      () =>
        new BinaryFileCatalogEntry(
          key: "sdca_confusion_matrix_png",
          filePath: Path.Combine(_basePath, "03_Reports/sdca_confusion_matrix.png"),
          expectedFileType: BinaryFileType.Png
        )
    );

  /// <summary>
  /// Confusion matrix heatmap for OVA model.
  /// Shows classification accuracy across all species.
  /// Stored as PNG image.
  /// </summary>
  public ICatalogEntry<byte[]> OvaConfusionMatrixPng =>
    GetOrCreateEntry(
      () =>
        new BinaryFileCatalogEntry(
          key: "ova_confusion_matrix_png",
          filePath: Path.Combine(_basePath, "03_Reports/ova_confusion_matrix.png"),
          expectedFileType: BinaryFileType.Png
        )
    );

  // ========================================
  // 04_Reports: Text Reports (Markdown)
  // ========================================

  /// <summary>
  /// Performance report for SDCA model (Markdown format).
  /// Includes accuracy metrics, per-class performance, and model characteristics.
  /// </summary>
  public ICatalogEntry<string> SdcaReportMd =>
    GetOrCreateEntry(
      () =>
        new TextFileCatalogEntry(
          key: "sdca_report_md",
          filePath: Path.Combine(_basePath, "04_Reports/sdca_model_report.md")
        )
    );

  /// <summary>
  /// Performance report for OVA model (Markdown format).
  /// Includes accuracy metrics, per-class performance, and model comparison.
  /// </summary>
  public ICatalogEntry<string> OvaReportMd =>
    GetOrCreateEntry(
      () =>
        new TextFileCatalogEntry(
          key: "ova_report_md",
          filePath: Path.Combine(_basePath, "04_Reports/ova_model_report.md")
        )
    );
}
