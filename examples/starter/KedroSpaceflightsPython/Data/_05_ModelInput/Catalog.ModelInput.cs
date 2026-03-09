using Flowthru.Data;
using KedroSpaceflightsPython.Data._05_ModelInput.Schemas;

namespace KedroSpaceflightsPython.Data;

/// <summary>
/// Model input data layer: Joined feature tables ("master tables").
/// Contains training and test splits ready for model consumption.
/// </summary>
public partial class Catalog
{
  /// <summary>
  /// Training features split from the model input table. Transient (memory only).
  /// </summary>
  public ICatalogEntry<IEnumerable<XValues>> XTest =>
    GetOrCreateEntry(() => CatalogEntries.Enumerable.Memory<XValues>(label: "XTest"));

  /// <summary>
  /// Test features split from the model input table. Transient (memory only).
  /// </summary>
  public ICatalogEntry<IEnumerable<XValues>> XTrain =>
    GetOrCreateEntry(() => CatalogEntries.Enumerable.Memory<XValues>(label: "XTrain"));

  /// <summary>
  /// Test targets split from the model input table. Transient (memory only).
  /// </summary>
  public ICatalogEntry<IEnumerable<YValues>> YTest =>
    GetOrCreateEntry(() => CatalogEntries.Enumerable.Memory<YValues>(label: "YTest"));

  /// <summary>
  /// Training targets split from the model input table. Transient (memory only).
  /// </summary>
  public ICatalogEntry<IEnumerable<YValues>> YTrain =>
    GetOrCreateEntry(() => CatalogEntries.Enumerable.Memory<YValues>(label: "YTrain"));
}
