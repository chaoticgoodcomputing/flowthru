using Flowthru.Data;
using SpaceflightsPythonEFCore.Data._05_ModelInput.Schemas;

namespace SpaceflightsPythonEFCore.Data;

/// <summary>
/// Model input data layer: Train/test splits.
/// Written and consumed by the Python DataScience pipeline (transient, memory-only).
/// </summary>
public partial class Catalog
{
  public ICatalogEntry<IEnumerable<XValues>> XTest =>
    GetOrCreateEntry(() => CatalogEntries.Enumerable.Memory<XValues>(label: "XTest"));

  public ICatalogEntry<IEnumerable<XValues>> XTrain =>
    GetOrCreateEntry(() => CatalogEntries.Enumerable.Memory<XValues>(label: "XTrain"));

  public ICatalogEntry<IEnumerable<YValues>> YTest =>
    GetOrCreateEntry(() => CatalogEntries.Enumerable.Memory<YValues>(label: "YTest"));

  public ICatalogEntry<IEnumerable<YValues>> YTrain =>
    GetOrCreateEntry(() => CatalogEntries.Enumerable.Memory<YValues>(label: "YTrain"));
}
