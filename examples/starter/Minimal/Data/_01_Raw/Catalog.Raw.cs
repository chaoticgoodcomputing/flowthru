using Flowthru.Data;
using Minimal.Data._01_Raw.Schemas;

namespace Minimal.Data;

public partial class Catalog
{
  /// <summary>
  /// Raw name data imported from CSV file.
  /// </summary>
  public ICatalogEntry<IEnumerable<NameSchema>> Names =>
    GetOrCreateEntry(
      () =>
        CatalogEntries.Enumerable.Csv<NameSchema>(
          label: "Names",
          filePath: $"{_basePath}/Data/_01_Raw/Datasets/names.csv"
        )
    );
}
