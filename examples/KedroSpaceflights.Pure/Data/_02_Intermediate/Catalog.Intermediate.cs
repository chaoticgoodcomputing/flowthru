using Flowthru.Data;
using KedroSpaceflights.Pure.Data._02_Intermediate.Schemas;

namespace KedroSpaceflights.Pure.Data;

public partial class Catalog
{
  // Intermediate data entries
  public ICatalogEntry<IEnumerable<PreprocessedCompanySchema>> PreprocessedCompanies =>
    GetOrCreateEntry(
      () =>
        CatalogEntries.Enumerable.Parquet<PreprocessedCompanySchema>(
          label: "PreprocessedCompanies",
          filePath: $"{_basePath}/_02_Intermediate/Datasets/preprocessed_companies.parquet"
        )
    );

  public ICatalogEntry<IEnumerable<PreprocessedShuttleSchema>> PreprocessedShuttles =>
    GetOrCreateEntry(
      () =>
        CatalogEntries.Enumerable.Parquet<PreprocessedShuttleSchema>(
          label: "PreprocessedShuttles",
          filePath: $"{_basePath}/_02_Intermediate/Datasets/preprocessed_shuttles.parquet"
        )
    );
}
