using Flowthru.Data;
using RetailData.Data._01_Raw.Schemas;

namespace RetailData.Data;

public partial class Catalog
{
  /// <summary>
  /// Raw retail transaction data from CSV
  /// </summary>
  public ICatalogEntry<IEnumerable<RawRetailSchema>> RawRetailData =>
    GetOrCreateEntry(
      () =>
        CatalogEntries.Enumerable.Csv<RawRetailSchema>(
          label: "RawRetailData",
          filePath: $"{_basePath}/_01_Raw/Datasets/online-retail-dataset.csv"
        )
    );
}
