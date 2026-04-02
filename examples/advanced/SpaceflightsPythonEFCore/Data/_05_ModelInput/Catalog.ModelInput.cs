using Flowthru.Data;
using SpaceflightsPythonEFCore.Data._05_ModelInput.Schemas;

namespace SpaceflightsPythonEFCore.Data;

/// <summary>
/// Model input data layer: Train/test splits.
/// Written and consumed by the Python DataScience pipeline (transient, memory-only).
/// </summary>
public partial class Catalog
{
  public IItem<IEnumerable<XValues>> XTest =>
    CreateItem(() => Items.Enumerable.Memory<XValues>(label: "XTest"));

  public IItem<IEnumerable<XValues>> XTrain =>
    CreateItem(() => Items.Enumerable.Memory<XValues>(label: "XTrain"));

  public IItem<IEnumerable<YValues>> YTest =>
    CreateItem(() => Items.Enumerable.Memory<YValues>(label: "YTest"));

  public IItem<IEnumerable<YValues>> YTrain =>
    CreateItem(() => Items.Enumerable.Memory<YValues>(label: "YTrain"));
}
