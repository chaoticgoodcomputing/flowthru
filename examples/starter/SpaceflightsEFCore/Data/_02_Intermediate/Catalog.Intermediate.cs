using Flowthru.Data;
using Flowthru.Data.Validation;
using Flowthru.Extensions.EFCore.Data;
using SpaceflightsEFCore.Data._02_Intermediate.Schemas;

namespace SpaceflightsEFCore.Data;

public partial class Catalog
{
  /// <summary>
  /// Preprocessed company data with validated and strongly-typed fields.
  /// </summary>
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

  /// <summary>
  /// Preprocessed shuttle data with validated and strongly-typed fields.
  /// </summary>
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
