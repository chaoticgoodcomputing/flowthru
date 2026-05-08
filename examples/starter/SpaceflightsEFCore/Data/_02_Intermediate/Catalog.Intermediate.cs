using Flowthru.Data.Catalog;
using Flowthru.Data.Storage;
using Flowthru.Data.Storage.EFCore;
using SpaceflightsEFCore.Data._02_Intermediate.Schemas;

namespace SpaceflightsEFCore.Data;

public partial class Catalog
{
  /// <summary>
  /// Preprocessed company data with validated and strongly-typed fields.
  /// Backed by a deferred <see cref="DbQuery{T}"/> handle: no rows are
  /// fetched until a step iterates the value.
  /// </summary>
  public IItem<IEnumerable<PreprocessedCompanySchema>> PreprocessedCompanies =>
    CreateItem(() =>
      ItemFactory.Enumerable.EFCoreQuery<PreprocessedCompanySchema, SpaceflightsDbContext>(
        label: "PreprocessedCompanies",
        contextFactory: _contextFactory
      ).WithMaxInspectionLevel(InspectionLevel.Shallow)
    );

  /// <summary>
  /// Preprocessed shuttle data with validated and strongly-typed fields.
  /// Backed by a deferred <see cref="DbQuery{T}"/> handle: no rows are
  /// fetched until a step iterates the value.
  /// </summary>
  public IItem<IEnumerable<PreprocessedShuttleSchema>> PreprocessedShuttles =>
    CreateItem(() =>
      ItemFactory.Enumerable.EFCoreQuery<PreprocessedShuttleSchema, SpaceflightsDbContext>(
        label: "PreprocessedShuttles",
        contextFactory: _contextFactory
      ).WithMaxInspectionLevel(InspectionLevel.Shallow)
    );
}
