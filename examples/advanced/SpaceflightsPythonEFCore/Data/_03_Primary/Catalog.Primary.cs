using Flowthru.Data.Catalog;
using SpaceflightsPythonEFCore.Data._03_Primary.Schemas;

namespace SpaceflightsPythonEFCore.Data;

/// <summary>
/// Primary data layer: Joined model input table.
/// Written by the C# DataProcessing pipeline and read by the Python DataScience pipeline.
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
