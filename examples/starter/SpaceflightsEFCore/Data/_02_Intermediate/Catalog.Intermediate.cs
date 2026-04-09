using Flowthru.Core.Data;
using Flowthru.Core.Data.Validation;
using Flowthru.Extensions.EFCore.Data;
using SpaceflightsEFCore.Data._02_Intermediate.Schemas;

namespace SpaceflightsEFCore.Data;

public partial class Catalog
{
  /// <summary>
  /// Preprocessed company data with validated and strongly-typed fields.
  /// </summary>
  public IItem<IEnumerable<PreprocessedCompanySchema>> PreprocessedCompanies =>
    CreateItem(
      () =>
        EFCoreItemFactory
          .Enumerable.EFCore<PreprocessedCompanySchema, SpaceflightsDbContext>(
            label: "PreprocessedCompanies",
            contextFactory: _contextFactory
          )
          .WithInspectionLevel(InspectionLevel.Shallow)
    );

  /// <summary>
  /// Preprocessed shuttle data with validated and strongly-typed fields.
  /// </summary>
  public IItem<IEnumerable<PreprocessedShuttleSchema>> PreprocessedShuttles =>
    CreateItem(
      () =>
        EFCoreItemFactory
          .Enumerable.EFCore<PreprocessedShuttleSchema, SpaceflightsDbContext>(
            label: "PreprocessedShuttles",
            contextFactory: _contextFactory
          )
          .WithInspectionLevel(InspectionLevel.Shallow)
    );
}
