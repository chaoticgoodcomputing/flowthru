using Flowthru.Data.Catalog;
using SpaceflightsEFCore.Data._02_Intermediate.Schemas;

namespace SpaceflightsEFCore.Data;

public partial class Catalog
{
  public IItem<IEnumerable<PreprocessedCompanySchema>> PreprocessedCompanies =>
    CreateItem(() => Item.Of<IEnumerable<PreprocessedCompanySchema>>("PreprocessedCompanies")
      .EFCoreQuery<PreprocessedCompanySchema, SpaceflightsDbContext>()
      .WithContextFactory(_contextFactory)
      .Build());

  public IItem<IEnumerable<PreprocessedShuttleSchema>> PreprocessedShuttles =>
    CreateItem(() => Item.Of<IEnumerable<PreprocessedShuttleSchema>>("PreprocessedShuttles")
      .EFCoreQuery<PreprocessedShuttleSchema, SpaceflightsDbContext>()
      .WithContextFactory(_contextFactory)
      .Build());
}
