using Flowthru.Data;
using Minimal.Data._02_Intermediate.Schemas;

namespace Minimal.Data;

public partial class Catalog
{
  /// <summary>
  /// Greetings with "Hello" prefix.
  /// </summary>
  public ICatalogEntry<IEnumerable<GreetingSchema>> HelloGreetings =>
    GetOrCreateEntry(
      () =>
        CatalogEntries.Enumerable.Csv<GreetingSchema>(
          label: "HelloGreetings",
          filePath: $"{_basePath}/Data/_02_Intermediate/Datasets/hello_greetings.csv"
        )
    );
}
