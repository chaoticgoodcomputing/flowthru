using Flowthru.Data;
using KedroSpaceflightsPython.Data._02_Intermediate.Schemas;

namespace KedroSpaceflightsPython.Data;

public partial class Catalog
{
  /// <summary>
  /// Preprocessed company data with validated and strongly-typed fields.
  /// </summary>
  public IItem<IEnumerable<PreprocessedCompanySchema>> PreprocessedCompanies =>
    CreateItem(
      () =>
        ItemFactory.Enumerable.Parquet<PreprocessedCompanySchema>(
          label: "PreprocessedCompanies",
          filePath: $"{_basePath}/_02_Intermediate/Datasets/preprocessed_companies.parquet"
        )
    );

  /// <summary>
  /// Preprocessed shuttle data with validated and strongly-typed fields.
  /// </summary>
  public IItem<IEnumerable<PreprocessedShuttleSchema>> PreprocessedShuttles =>
    CreateItem(
      () =>
        ItemFactory.Enumerable.Parquet<PreprocessedShuttleSchema>(
          label: "PreprocessedShuttles",
          filePath: $"{_basePath}/_02_Intermediate/Datasets/preprocessed_shuttles.parquet"
        )
    );
}
