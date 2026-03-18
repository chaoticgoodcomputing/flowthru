using Flowthru.Data;
using Flowthru.Data.Validation;
using Flowthru.Extensions.EFCore.Data;
using SpaceflightsPythonEFCore.Data._02_Intermediate.Schemas;

namespace SpaceflightsPythonEFCore.Data;

/// <summary>
/// Intermediate data layer: Preprocessed, strongly-typed shuttle and company data.
/// Written by C# DataProcessing nodes and stored in SQLite via EFCore.
/// </summary>
public partial class Catalog
{
  public ICatalogEntry<IEnumerable<PreprocessedCompanySchema>> PreprocessedCompanies =>
    GetOrCreateEntry(
      () =>
        EFCoreCatalogEntries
          .Enumerable.EFCore<PreprocessedCompanySchema, SpaceflightsDbContext>(
            label: "PreprocessedCompanies",
            contextFactory: _contextFactory
          )
          .WithInspectionLevel(InspectionLevel.Shallow)
    );

  public ICatalogEntry<IEnumerable<PreprocessedShuttleSchema>> PreprocessedShuttles =>
    GetOrCreateEntry(
      () =>
        EFCoreCatalogEntries
          .Enumerable.EFCore<PreprocessedShuttleSchema, SpaceflightsDbContext>(
            label: "PreprocessedShuttles",
            contextFactory: _contextFactory
          )
          .WithInspectionLevel(InspectionLevel.Shallow)
    );
}
