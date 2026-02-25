using Flowthru.Data;
using KedroSpaceflights.Custom.Data._01_Raw.Schemas;
using KedroSpaceflights.Custom.Data._03_Primary.Schemas;

namespace KedroSpaceflights.Custom.Data;

public partial class Catalog
{
  /// <summary>
  /// Raw company data from CSV file.
  /// Contains company ratings and information.
  /// </summary>
  /// <remarks>
  /// This is a critical Layer 0 input from an external source, configured for deep inspection
  /// to ensure data quality before pipeline execution.
  /// </remarks>
  public ICatalogEntry<IEnumerable<CompanyRawSchema>> Companies =>
    GetOrCreateEntry(
      () =>
        CatalogEntries.Enumerable.Csv<CompanyRawSchema>(
          label: "RawCompanies",
          filePath: $"{_basePath}/_01_Raw/Datasets/companies.csv"
        )
    );

  /// <summary>
  /// Raw review data from CSV file.
  /// Contains customer reviews with scores.
  /// </summary>
  /// <remarks>
  /// This is a critical Layer 0 input from an external source, configured for deep inspection
  /// to ensure data quality before pipeline execution.
  /// </remarks>
  public ICatalogEntry<IEnumerable<ReviewRawSchema>> Reviews =>
    GetOrCreateEntry(
      () =>
        CatalogEntries.Enumerable.Csv<ReviewRawSchema>(
          label: "RawReviews",
          filePath: $"{_basePath}/_01_Raw/Datasets/reviews.csv"
        )
    );

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
  public ICatalogEntry<IEnumerable<ShuttleRawSchema>> Shuttles =>
    GetOrCreateEntry(
      () =>
        CatalogEntries.Enumerable.Excel<ShuttleRawSchema>(
          label: "RawShuttles",
          filePath: $"{_basePath}/_01_Raw/Datasets/shuttles.xlsx",
          sheetName: "Sheet1"
        )
    );

  /// <summary>
  /// Reference model input table from Kedro pipeline (for validation).
  /// Used to compare Flowthru implementation against original Kedro output.
  /// </summary>
  /// <remarks>
  /// This is external reference data from the original Kedro implementation,
  /// used for validation purposes in the DataDiagnostics pipeline.
  /// Uses the same schema as our model input table (ModelInputSchema).
  /// </remarks>
  public ICatalogEntry<IEnumerable<ModelInputSchema>> KedroModelInputTable =>
    GetOrCreateEntry(
      () =>
        CatalogEntries.Enumerable.Csv<ModelInputSchema>(
          label: "KedroModelInputTable",
          filePath: $"{_basePath}/_01_Raw/Datasets/kedro_model_input_table.csv"
        )
    );
}
