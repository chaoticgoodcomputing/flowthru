using Flowthru.Data;
using Flowthru.Data.Validation;
using Flowthru.Extensions.EFCore.Data;
using KedroSpaceflights.Data._02_Intermediate.Schemas;

namespace KedroSpaceflights.Data;

public partial class Catalog
{
  /// <summary>
  /// Preprocessed company data with validated and strongly-typed fields.
  /// </summary>
  public ICatalogEntry<IEnumerable<PreprocessedCompanySchema>> PreprocessedCompanies =>
    GetOrCreateEntry(
      () =>
        ((CatalogEntry<IEnumerable<PreprocessedCompanySchema>>)
          EFCoreCatalogEntries.Enumerable.EFCore<PreprocessedCompanySchema>(
            label: "PreprocessedCompanies",
            context: _dbContext
          )).WithInspectionLevel(InspectionLevel.Shallow)
    );

  /// <summary>
  /// Preprocessed shuttle data with validated and strongly-typed fields.
  /// </summary>
  public ICatalogEntry<IEnumerable<PreprocessedShuttleSchema>> PreprocessedShuttles =>
    GetOrCreateEntry(
      () =>
        ((CatalogEntry<IEnumerable<PreprocessedShuttleSchema>>)
          EFCoreCatalogEntries.Enumerable.EFCore<PreprocessedShuttleSchema>(
            label: "PreprocessedShuttles",
            context: _dbContext
          )).WithInspectionLevel(InspectionLevel.Shallow)
    );
}
