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
  public IItem<IEnumerable<XValues>> XTest =>
    CreateItem(() => Items.Enumerable.Memory<XValues>(label: "XTest"));

  /// <summary>
  /// Test features split from the model input table. Transient (memory only).
  /// </summary>
  public IItem<IEnumerable<XValues>> XTrain =>
    CreateItem(() => Items.Enumerable.Memory<XValues>(label: "XTrain"));

  /// <summary>
  /// Test targets split from the model input table. Transient (memory only).
  /// </summary>
  public IItem<IEnumerable<YValues>> YTest =>
    CreateItem(() => Items.Enumerable.Memory<YValues>(label: "YTest"));

  /// <summary>
  /// Training targets split from the model input table. Transient (memory only).
  /// </summary>
  public IItem<IEnumerable<YValues>> YTrain =>
    CreateItem(() => Items.Enumerable.Memory<YValues>(label: "YTrain"));
}
