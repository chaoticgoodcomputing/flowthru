using Flowthru.Data;
using SpaceflightsDistributed.DataProcessing.Data._02_Intermediate.Schemas;

namespace SpaceflightsDistributed.DataProcessing.Data;

public partial class DataProcessingCatalog
{
  public IItem<IEnumerable<PreprocessedCompanySchema>> PreprocessedCompanies =>
    CreateItem(
      () =>
        ItemFactory.Enumerable.Parquet<PreprocessedCompanySchema>(
          label: "PreprocessedCompanies",
          filePath: $"{_basePath}/_02_Intermediate/Datasets/preprocessed_companies.parquet"
        )
    );

  public IItem<IEnumerable<PreprocessedShuttleSchema>> PreprocessedShuttles =>
    CreateItem(
      () =>
        ItemFactory.Enumerable.Parquet<PreprocessedShuttleSchema>(
          label: "PreprocessedShuttles",
          filePath: $"{_basePath}/_02_Intermediate/Datasets/preprocessed_shuttles.parquet"
        )
    );
}
