using Flowthru.Data;
using Flowthru.Extensions.EFCore.Data;
using SpaceflightsPythonEFCore.Data._03_Primary.Schemas;

namespace SpaceflightsPythonEFCore.Data;

/// <summary>
/// Primary data layer: Joined model input table.
/// Written by the C# DataProcessing pipeline and read by the Python DataScience pipeline.
/// This is the EFCore → Python handoff point.
/// </summary>
public partial class Catalog
{
  public ICatalogEntry<IEnumerable<ModelInputTableSchema>> ModelInputTable =>
    GetOrCreateEntry(
      () =>
        EFCoreCatalogEntries.Enumerable.EFCore<ModelInputTableSchema, SpaceflightsDbContext>(
          label: "ModelInputTable",
          contextFactory: _contextFactory,
          queryCustomizer: q => q.OrderBy(r => r.ShuttleId)
        )
    );
}
