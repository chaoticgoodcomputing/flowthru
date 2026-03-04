using Flowthru.Data;
using Flowthru.Extensions.EFCore.Data;
using KedroSpaceflights.Data._03_Primary.Schemas;

namespace KedroSpaceflights.Data;

/// <summary>
/// Primary data layer: Domain model data.
/// Contains datasets structured according to the problem being solved, not the source system structure.
/// </summary>
public partial class Catalog
{
  /// <summary>
  /// Master table of features and labels prepared for model training.
  /// Combines preprocessed company and shuttle data into a single feature matrix.
  /// </summary>
  public ICatalogEntry<IEnumerable<ModelInputTableSchema>> ModelInputTable =>
    GetOrCreateEntry(
      () =>
        EFCoreCatalogEntries.Enumerable.EFCore<ModelInputTableSchema>(
          label: "ModelInputTable",
          context: _dbContext
        )
    );
}
