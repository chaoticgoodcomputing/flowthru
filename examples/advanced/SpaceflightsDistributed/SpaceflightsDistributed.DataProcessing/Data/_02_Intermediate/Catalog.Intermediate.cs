using Flowthru.Data.Catalog;
using SpaceflightsDistributed.DataProcessing.Data._02_Intermediate.Schemas;

namespace SpaceflightsDistributed.DataProcessing.Data;

public partial class DataProcessingCatalog
{
  public IItem<IEnumerable<PreprocessedCompanySchema>> PreprocessedCompanies =>
    CreateItem(() => Item.Of<IEnumerable<PreprocessedCompanySchema>>("PreprocessedCompanies")
      .Parquet()
      .AtPath($"{_basePath}/_02_Intermediate/Datasets/preprocessed_companies.parquet")
      .Build());

  public IItem<IEnumerable<PreprocessedShuttleSchema>> PreprocessedShuttles =>
    CreateItem(() => Item.Of<IEnumerable<PreprocessedShuttleSchema>>("PreprocessedShuttles")
      .Parquet()
      .AtPath($"{_basePath}/_02_Intermediate/Datasets/preprocessed_shuttles.parquet")
      .Build());
}
