using Flowthru.Data.Catalog;
using Spaceflights.Data._02_Intermediate.Schemas;

namespace Spaceflights.Data;

public partial class Catalog
{
  /// <summary>Preprocessed company data with validated and strongly-typed fields.</summary>
  public IItem<IEnumerable<PreprocessedCompanySchema>> PreprocessedCompanies =>
    CreateItem(() => Item.Of<IEnumerable<PreprocessedCompanySchema>>("PreprocessedCompanies")
      .Parquet()
      .AtPath($"{_basePath}/_02_Intermediate/Datasets/preprocessed_companies.parquet")
      .Build());

  /// <summary>Preprocessed shuttle data with validated and strongly-typed fields.</summary>
  #region docs:item-parquet
  public IItem<IEnumerable<PreprocessedShuttleSchema>> PreprocessedShuttles =>
    CreateItem(() => Item.Of<IEnumerable<PreprocessedShuttleSchema>>("PreprocessedShuttles")
      .Parquet()
      .AtPath($"{_basePath}/_02_Intermediate/Datasets/preprocessed_shuttles.parquet")
      .Build());
  #endregion
}
