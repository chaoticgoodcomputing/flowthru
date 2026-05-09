using Flowthru.Data.Catalog;
using SpaceflightsEFCore.Data._03_Primary.Schemas;

namespace SpaceflightsEFCore.Data;

/// <summary>
/// Primary data layer: Domain model data.
/// </summary>
public partial class Catalog
{
  public IItem<IEnumerable<ModelInputTableSchema>> ModelInputTable =>
    CreateItem(() => Item.Of<IEnumerable<ModelInputTableSchema>>("ModelInputTable")
      .EFCoreTable<ModelInputTableSchema, SpaceflightsDbContext>()
      .WithContextFactory(_contextFactory)
      .WithQuery(q => q.OrderBy(r => r.ShuttleId))
      .Build());
}
