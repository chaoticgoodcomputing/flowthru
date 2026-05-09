using Flowthru.Data.Catalog;
using SpaceflightsPythonEFCore.Data._02_Intermediate.Schemas;

namespace SpaceflightsPythonEFCore.Data;

/// <summary>
/// Intermediate data layer: Preprocessed, strongly-typed shuttle and company data.
/// Written by C# DataProcessing nodes and stored in SQLite via EFCore.
/// </summary>
public partial class Catalog
{
  public IItem<IEnumerable<PreprocessedCompanySchema>> PreprocessedCompanies =>
    CreateItem(() => Item.Of<IEnumerable<PreprocessedCompanySchema>>("PreprocessedCompanies")
      .EFCoreTable<PreprocessedCompanySchema, SpaceflightsDbContext>()
      .WithContextFactory(_contextFactory)
      .Build());

  public IItem<IEnumerable<PreprocessedShuttleSchema>> PreprocessedShuttles =>
    CreateItem(() => Item.Of<IEnumerable<PreprocessedShuttleSchema>>("PreprocessedShuttles")
      .EFCoreTable<PreprocessedShuttleSchema, SpaceflightsDbContext>()
      .WithContextFactory(_contextFactory)
      .Build());
}
