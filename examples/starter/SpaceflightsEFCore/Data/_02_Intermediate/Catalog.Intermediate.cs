using Flowthru.Core.Data;
using Flowthru.Core.Data.Validation;
using Flowthru.Extensions.EFCore.Data;
using SpaceflightsEFCore.Data._02_Intermediate.Schemas;

namespace SpaceflightsEFCore.Data;

public partial class Catalog
{
  /// <summary>
  /// Preprocessed company data with validated and strongly-typed fields.
  /// Backed by a deferred <see cref="Flowthru.Extensions.EFCore.Data.DbQuery{T}"/> handle:
  /// no rows are fetched until a step iterates the value.
  /// </summary>
  public IItem<IEnumerable<PreprocessedCompanySchema>> PreprocessedCompanies =>
    CreateItem(
      () =>
        EFCoreItemFactory
          .Query.EFCore<PreprocessedCompanySchema, SpaceflightsDbContext>(
            label: "PreprocessedCompanies",
            contextFactory: _contextFactory
          )
          .WithInspectionLevel(InspectionLevel.Shallow)
    );

  /// <summary>
  /// Preprocessed shuttle data with validated and strongly-typed fields.
  /// Backed by a deferred <see cref="Flowthru.Extensions.EFCore.Data.DbQuery{T}"/> handle:
  /// no rows are fetched until a step iterates the value.
  /// </summary>
  public IItem<IEnumerable<PreprocessedShuttleSchema>> PreprocessedShuttles =>
    CreateItem(
      () =>
        EFCoreItemFactory
          .Query.EFCore<PreprocessedShuttleSchema, SpaceflightsDbContext>(
            label: "PreprocessedShuttles",
            contextFactory: _contextFactory
          )
          .WithInspectionLevel(InspectionLevel.Shallow)
    );
}
