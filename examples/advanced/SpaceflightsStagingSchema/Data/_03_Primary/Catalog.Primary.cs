using Flowthru.Data.Catalog;
using SpaceflightsStagingSchema.Data._03_Primary.Schemas;

namespace SpaceflightsStagingSchema.Data;

public partial class ProductionCatalog
{
  /// <summary>
  /// Model input table — a deferred query view over
  /// <see cref="Companies"/>, <see cref="Shuttles"/>, and <see cref="Reviews"/>.
  /// </summary>
  public IItem<IEnumerable<ModelInputTableSchema>> ModelInputTable =>
    CreateItem(() => Item.Of<IEnumerable<ModelInputTableSchema>>("ProductionModelInputTableView")
      .Memory()
      .Build());
}
